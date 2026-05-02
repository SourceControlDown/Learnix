# Learnix — ADR: AI Chat

> Формат: що вирішили → чому → які альтернативи відкинули.
> Покриває Phase 8: B-44 (MongoDB), B-45 (AI providers + SSE), B-46 (session persistence), B-46.5 (cleanup).

---

## ADR-019: AI Chat Provider Abstraction (`IAiChatProvider`)

**Рішення:** Application layer визначає `IAiChatProvider` з одним методом що повертає `IAsyncEnumerable<ChatStreamEvent>`. Infrastructure містить `AnthropicChatProvider` і `GeminiChatProvider`. Вибір провайдера через `appsettings.json` → `AiChat:Provider = "Anthropic" | "Gemini"`. DI factory резолвить правильну реалізацію по рядку з конфігу.

Streaming events нормалізуються в спільну модель незалежно від провайдера:

```
TextDeltaEvent(Content)
ToolUseStartEvent(CallId, ToolName)
ToolUseEndEvent(CallId, ToolName, ArgumentsJson)
MessageEndEvent(FinishReason)
ProviderErrorEvent(Message, Code)
```

**Чому:**
- Swap провайдера без жодних змін в Application layer — достатньо змінити один рядок конфігу.
- Tool execution loop пишеться один раз в `ChatStreamOrchestrator`, не дублюється на провайдер.
- `IAsyncEnumerable` дозволяє стрімити события безпосередньо в SSE без буферизації всієї відповіді.

**Альтернативи:**
- Anthropic-only в v1 — простіше, але втрачаємо можливість переключення для cost optimization або fallback.
- Окремі handlers per provider — масивне дублювання tool execution loop.
- Повертати `Stream` замість `IAsyncEnumerable<ChatStreamEvent>` — дешевше за абстракцією, але тоді парсинг SSE лягав би на Application layer, що порушує Clean Architecture.

---

## ADR-020: MongoDB для зберігання AI чат-сесій

**Рішення:** AI чат-сесії зберігаються в MongoDB колекції `chat_sessions`. Один документ = одна сесія = список повідомлень.

Схема документа:
```json
{
  "_id": "ObjectId",
  "userId": "Guid",
  "isActive": true,
  "messages": [
    { "role": "user|assistant|tool_result", "content": "...", "sentAt": "DateTime",
      "toolCalls": [{ "callId": "...", "toolName": "...", "argumentsJson": "...", "resultJson": "..." }] }
  ],
  "createdAt": "DateTime",
  "updatedAt": "DateTime",
  "closedAt": "DateTime|null"
}
```

Індекс: `{ userId: 1, isActive: 1 }` — для швидкого знаходження активної сесії.

**Чому:**
- Документна структура природна для conversational data — повідомлення зберігаються як масив всередині документа, а не в окремій таблиці з FK.
- MongoDB `$push` дає атомарний append повідомлень без race condition при конкурентних запитах.
- Уникаємо схемної міграції при зміні структури повідомлень (нові поля типу `toolCalls` додаються без ALTER TABLE).

**Альтернативи:**
- PostgreSQL JSONB — теж підтримує документну структуру, але вже використовується для реляційних даних. Змішувати не варто без вагомої причини.
- Redis зі списками — надто ephemeral, не підходить для 30-денного зберігання.

---

## ADR-021: Single Active Session per User

**Рішення:** Кожен користувач має максимум одну активну сесію (`isActive: true`). При досягненні hard cap 50 повідомлень або при виклику `DELETE /api/ai-chat/session` — поточна сесія закривається (`isActive: false`, `closedAt` заповнюється), при наступному повідомленні створюється нова.

Закриті сесії **не показуються в UI** (GET повертає тільки активну), але зберігаються 30 днів для observability, потім видаляються `ChatSessionCleanupService`.

**Чому:**
- Спрощений UX — у студента завжди є "поточний чат", без навігації по історії сесій.
- Soft close замість hard delete — залишаємо дані для дебагу якщо AI дав неправильну відповідь.
- Hard cap 50 повідомлень захищає від надто довгого контексту і явно сигналізує про розрив (нова сесія = чистий старт).

**Альтернативи:**
- Multiple sessions з UI для переключення (Model B) — правильніше для функціонального продукту, але over-engineering для LMS де AI = допоміжний інструмент, а не основний.
- Нескінченна сесія без cap — ризик exponential росту документа і context overflow у провайдера.

---

## ADR-022: Sliding Window Context (20 повідомлень)

**Рішення:** До AI провайдера передаються лише останні 20 повідомлень з активної сесії. Старіші повідомлення залишаються в MongoDB, але не потрапляють у context window провайдера.

Реалізовано в `ChatStreamOrchestrator` перед кожним викликом провайдера: `conversation.TakeLast(contextWindowSize)`.

**Чому:**
- Cost control: менше токенів = менша вартість за запит. 20 повідомлень ≈ 10 пар user/assistant — достатньо для типового сценарію "порекомендуй курс".
- Латенція: коротший prompt = швидша перша відповідь.
- `ContextWindowSize` вирізається в `appsettings.json` (`AiChat:ContextWindowSize: 20`) для можливості тюнінгу без перекомпіляції.

**Альтернативи:**
- Передавати всі повідомлення — правильно для складних розмов, але дорого і повільно при великих сесіях.
- Summarization попередніх повідомлень — ефективний підхід для production AI assistants, але значно складніший (потребує окремого API call для summary generation).

---

## ADR-023: Tool Use для рекомендацій курсів

**Рішення:** AI провайдер має доступ до інструменту `search_courses(query, category?, maxResults?)`. Реалізовано через `IChatTool` інтерфейс в Application layer. `SearchCoursesTool` делегує до `IMediator.Send(SearchCoursesQuery)` — пошук по опублікованих курсах з фільтрацією по назві/описі.

Tool execution loop в `ChatStreamOrchestrator`:
1. Отримує `ToolUseEndEvent` від провайдера.
2. Знаходить відповідний `IChatTool` по імені.
3. Викликає `ExecuteAsync` — результат як JSON string.
4. Додає `tool_result` message в conversation.
5. Повторно викликає провайдера з оновленою historical context.
6. Loop до `MessageEndEvent` без tool calls (або max 5 turns для безпеки).

Фільтрація по `category` реалізована через lookup `ICategoryRepository` по slug, а не через JOIN — у `Course` немає навігаційного property `Category` (тільки `CategoryId` FK, відповідно до EF конфігурації проєкту).

**Чому:**
- Tool use — стандартний підхід для grounded recommendations замість hallucination.
- `IChatTool` реєструються як `IEnumerable<IChatTool>` — нові інструменти (search lessons, get enrollment status) додаються без зміни orchestrator.
- MediatR як точка входу для tool execution зберігає validation pipeline (FluentValidation) і logging behavior.

**Альтернативи:**
- RAG (Retrieval-Augmented Generation) — значно потужніше для семантичного пошуку, але потребує embedding model і vector store. Over-engineering для поточного обсягу курсів.
- System prompt з переліком всіх курсів — не масштабується при сотнях курсів.
- Пряме звернення з Infrastructure до БД у tool — порушує Clean Architecture, оминає validation pipeline.

---

## ADR-024: Rate Limiting AI Chat (20 запитів/годину per user)

**Рішення:** Окремий `RateLimiterPolicy` `"ai-chat"` для `POST /api/ai-chat/messages`. 20 запитів / 1 година, partition key = `userId` (з JWT `sub` claim). Реалізовано через ASP.NET Rate Limiter `FixedWindowLimiter` — той самий механізм що в `"auth-strict"` policy (ADR-038 в DECISIONS_AUTH.md).

При перевищенні — HTTP 429 + `ProblemDetails` з `Retry-After` header (уніфікований `OnRejected` handler).

**Чому:**
- Cost control: кожен запит до Anthropic/Gemini — платний. 20/год = ~1 запит кожні 3 хвилини, достатньо для навчального використання.
- Anti-abuse: без ліміту один користувач може вичерпати API quota для всього продукту.
- Per-user (не per-IP) partition — правильніший для authenticated endpoints де IP може бути shared (NAT, VPN).

**Альтернативи:**
- Sliding window замість fixed window — рівніший розподіл, але трохи складніше. Для 20/год різниця несуттєва.
- Token budget (ліміт по кількості токенів) — більш точний cost control, але потребує tracking токенів в БД.

---

## ADR-025: SSE замість WebSocket для AI стрімінгу

**Рішення:** `POST /api/ai-chat/messages` повертає `Content-Type: text/event-stream`. Контролер пише SSE events напряму в `Response.Body` без буферизації. Endpoint свідомо виключений з MediatR pipeline — `ChatStreamOrchestrator` викликається напряму, бо SSE потребує доступу до `HttpContext.Response`.

SSE формат:
```
event: text_delta
data: {"content":"Hello"}

event: tool_use_start
data: {"toolName":"search_courses","callId":"abc123"}

event: tool_use_end
data: {"callId":"abc123","resultsCount":5}

event: message_end
data: {"finishReason":"end_turn","sessionMessageCount":12}

event: error
data: {"message":"Provider unavailable","code":"PROVIDER_ERROR"}
```

Tool execution відбувається повністю server-side. Клієнт бачить `tool_use_start`/`tool_use_end` для UX (показати spinner "шукаю курси..."), але не виконує інструмент.

**Чому:**
- SSE простіший за WebSocket для one-way streaming (server → client). Не потребує окремого upgrade, proxy-friendly, вбудована підтримка в браузерах через `EventSource`.
- Виключення SSE endpoint з MediatR — виправданий виняток з правила: handler не може стрімити в `HttpContext.Response`. Задокументований тут як явне рішення, а не прогалина.

**Альтернативи:**
- WebSocket — підходить якщо потрібен bi-directional streaming (напр., для Student↔Instructor messaging). Для AI відповідей один напрямок достатній.
- Polling (client запитує кожні N секунд) — простіша серверна реалізація, але вища латенція першого токена і зайве навантаження.
- Long polling — компроміс між polling і SSE, але складніший і без переваг у порівнянні з SSE для streaming.

---

## ADR-026: Cleanup закритих AI чат-сесій (30-денна retention)

**Рішення:** `ChatSessionCleanupService` — `BackgroundService` на базі `PeriodicTimer` з інтервалом 24 години. Видаляє документи з `isActive: false` і `updatedAt < UtcNow - 30d` через `IChatSessionRepository.DeleteOlderThanAsync`. Запускається одразу при старті і далі кожні 24 год.

**Чому:**
- 30 днів — достатньо для post-mortem якщо AI дав помилкову відповідь і є скарга від студента.
- `PeriodicTimer` + `BackgroundService` — той самий паттерн що `RefreshTokenCleanupHostedService`. Консистентність в кодовій базі.
- `IServiceScopeFactory` для резолву scoped `IChatSessionRepository` — обов'язково для singleton-hosted services що звертаються до scoped залежностей.

**Альтернативи:**
- MongoDB TTL index (`expireAfterSeconds`) — автоматичний, не потребує коду. Відкинуто: TTL index на MongoDB працює тільки з одним полем з `Date` типом і видаляє документ по фіксованому часу після значення в полі. Для умовного видалення (`isActive: false AND updatedAt < threshold`) TTL не підходить — він видалив би всі документи через 30 днів, включаючи активні.
- Cron job в окремому процесі — зайва складність для однієї операції.
