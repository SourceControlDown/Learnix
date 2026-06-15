# Learnix — ADR: Інфраструктура

> Формат: що вирішили → чому → які альтернативи відкинули.
> Оновлюється після кожного чату, де приймались архітектурні рішення.

Суміжні файли: [DECISIONS_ARCHITECTURE.md](DECISIONS_ARCHITECTURE.md) · [DECISIONS_AUTH.md](DECISIONS_AUTH.md) · [DECISIONS_DOMAIN.md](DECISIONS_DOMAIN.md)

## Конвенція статусів

ADR не видаляються. Якщо рішення переглянуто — старий ADR помічається `Superseded by ADR-XXX`, новий — `Supersedes ADR-YYY`. Це зберігає історію мислення і показує як архітектура еволюціонувала.

---

## ADR-001: PostgreSQL + MongoDB (polyglot persistence)

**Рішення:** Основні реляційні дані в PostgreSQL, неструктуровані — в MongoDB.

**Чому:**
- Більшість даних (Users, Courses, Enrollments, Payments) — строго реляційні, потребують транзакцій і FK constraints
- Chat sessions мають змінну кількість повідомлень, не потребують joins → MongoDB nature fit
- Reviews: гнучка схема, можливість додавати поля без міграцій

**Що в MongoDB:**
- `chat_sessions` — історія AI чату
- `course_reviews` — відгуки з рейтингами

**Альтернативи:**
- Все в PostgreSQL (JSONB для чатів) — можливо, але ускладнює query patterns для document-like даних
- Все в MongoDB — втрата referential integrity для критичних даних (платежі, enrollments)

---

## ADR-002: MassTransit + Azure Service Bus для async processing

**Рішення:** Асинхронні операції (email, PDF generation, achievements) виконуються через MassTransit consumers, підключені до Azure Service Bus.

**Чому:**
- Відв'язує тривалі операції від HTTP request lifecycle
- Retry з backoff — якщо email provider тимчасово недоступний
- Масштабування consumers незалежно від API

**Що йде через bus:**
- Emails (verification, enrollment confirmation, instructor approval)
- Certificate PDF generation
- Achievement checking
- Enrollment activation після оплати

**Що залишається in-process (MediatR):**
- Cache invalidation
- Progress updates
- Permission checks

**Альтернативи:**
- Background tasks (Hangfire / `IHostedService`) — простіше, але немає guaranteed delivery
- RabbitMQ — self-hosted альтернатива, але потребує менеджменту інфраструктури

---

## ADR-003: Specification Pattern для queries

**Рішення:** Усі запити до репозиторіїв використовують Specification<T> для інкапсуляції criteria, includes, ordering, paging.

**Чому:**
- Логіка фільтрації/сортування живе в Application layer, а не в Infrastructure
- Специфікації легко тестувати ізольовано
- Уникаємо дублювання query logic між handlers

**Конвенції:**
- `AsNoTracking = true` за замовчуванням
- Для Commands, що змінюють сутності: явно `AsNoTracking = false`
- Розташування: `Application/{Feature}/Specifications/`

---

## ADR-004: Redis distributed cache — ICacheable<TValue> + MediatR pipeline behavior

**Рішення:** Queries, що реалізують `ICacheable<TValue>`, автоматично кешуються в Redis через `CachingBehavior<TRequest, TValue>`. Commands, що змінюють кешовані дані, явно інвалідують відповідні ключі після `SaveChangesAsync`.

---

### Реалізація

**Інтерфейс:**
```csharp
public interface ICacheable<TValue>
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
}
```

**Pipeline behavior** реалізує `IPipelineBehavior<TRequest, Result<TValue>>`, де `TValue` — другий generic-параметр. MediatR закриває тип автоматично: для `GetAllCategoriesQuery : ICacheable<IReadOnlyList<CategoryListItemDto>>` MediatR виводить `TValue = IReadOnlyList<CategoryListItemDto>`. Рефлексії немає — `response.Value` і `Result.Ok(value)` типізовані на рівні компілятора.

**Серіалізація:** кешується тільки `Value` з `Result<T>`, не весь Result-wrapper. `FluentResults.Result<T>` не підтримує JSON roundtrip (private setter на `Value`). `System.Text.Json` серіалізує payload напряму, десеріалізує назад, і behavior обгортає в `Result.Ok(value)`.

**Інвалідація:** у command handlers після `SaveChangesAsync` викликається `IDistributedCache.RemoveAsync(key)`. `IDistributedCache` — це офіційна Microsoft abstraction (не інфраструктурна деталь), тому живе в Application layer поруч з handlers.

---

### Які queries кешуються і чому

| Query | Ключ | TTL | Чому |
|---|---|---|---|
| `GetAllCategoriesQuery` | `categories:all` | 24 год | Список категорій змінюється лише через адмін-дії. Читається при кожному відкритті каталогу та фільтрів. Найдовший TTL — найнижчий churn. |
| `GetFeaturedCoursesQuery` | `courses:featured` | 30 хв | Вибірка популярних курсів — expensive JOIN з сортуванням по enrollments/rating. Запит однаковий для всіх користувачів (public, без per-user контексту). |
| `GetCourseByIdQuery` | `course:{id}` | 10 хв | Сторінка деталей курсу читається масово студентами перед записом. Включає `AverageRating` та `ReviewsCount` — змінюються кожним відгуком. Явна інвалідація при зміні курсу та відгуків. |
| `GetPublicCoursesQuery` | `courses:public:{всі 8 параметрів}` | 5 хв | Каталог — найнавантаженіший endpoint (пошук + фільтри + сортування + пагінація). Унікальний ключ на кожну комбінацію параметрів — неможливо інвалідувати за патерном через `IDistributedCache` без підключення `IConnectionMultiplexer` напряму. Короткий TTL компенсує відсутність явної інвалідації. |

**Що навмисно НЕ кешується:**
- Per-user queries (`GetMyProfile`, `GetMyEnrollments`, `GetMyAchievements`) — у кожного користувача свій стан, частих мутацій багато, ключ включав би userId → мала ймовірність cache hit для конкретного запиту.
- Admin queries — низький трафік, не впливає на продуктивність.
- Real-time дані (chat, SignalR notifications) — завжди актуальні.

---

### Інвалідація — де і чому

**Явна інвалідація `course:{id}` + `courses:featured`** після кожної мутації курсу:
- `PublishCourse`, `UnpublishCourse` — статус курсу змінюється, він з'являється або зникає з каталогу
- `ArchiveCourse`, `UnarchiveCourse` — аналогічно
- `UpdateCourseDetails` — змінюється title, price, cover, category — все є в закешованому DTO
- `DeleteCourse`, `AdminDeleteCourse`, `AdminRecoverCourse`, `AdminUnpublishCourse` — курс повністю змінює стан

**Явна інвалідація `course:{id}`** при мутаціях відгуків:
- `CreateReview`, `UpdateReview`, `DeleteReview` — всі три змінюють `AverageRating` та `ReviewsCount` на `Course` entity. `CourseDetailDto` включає ці поля — без інвалідації кешована сторінка показувала б застарілий рейтинг.

**Явна інвалідація `categories:all`** при будь-якій зміні категорій:
- `CreateCategory`, `UpdateCategory`, `DeleteCategory`, `SetCategoryImage`, `DeleteCategoryImage`

**`GetPublicCoursesQuery` — тільки TTL (5 хв):** оскільки ключ включає всі 8 filter-параметрів (search, skip, take, categoryId, instructorId, sortBy, isFree, minRating), різних комбінацій можуть бути сотні. Видалення за префіксом `courses:public:*` потребує `IConnectionMultiplexer.GetServer().Keys()` — дорога O(N) операція на Redis. Для каталогу 5-хвилинна затримка видимості після публікації курсу прийнятна.

---

### Чому Redis, а не IMemoryCache

`IDistributedCache` (Redis) — єдиний centralized store, `RemoveAsync(key)` є Redis `DEL` command. При горизонтальному масштабуванні (декілька API instances) інвалідація на одному instance автоматично поширюється на всі: наступний запит на будь-якому instance отримає cache miss і перечитає з БД.

`IMemoryCache` — per-process. Інвалідація на instance A не впливає на instances B і C, які продовжують роздавати stale дані до свого TTL. Неприйнятно для даних що явно інвалідуються (category list, course detail).

**Чому:**
- Популярні курси, каталог категорій — read-heavy, рідко змінюються
- Redis дає O(1) lookup і TTL з коробки
- Pipeline behavior — кешування прозоре для handler, без boilerplate в кожному query
- Distributed cache коректно працює при scale-out

**Альтернативи:**
- `IMemoryCache` — простіше, але stale data при multiple instances. Відхилено для публічних запитів.
- Lazy invalidation (тільки TTL для всього) — простіше, але `CourseDetailDto` з рейтингом показував би застарілий rating хвилинами після відгуку. Відхилено для `course:{id}`.
- Response caching middleware (`[ResponseCache]`) — HTTP-level cache, не контролює per-key invalidation. Відхилено.

**Наслідки:**
- `ICacheable<TValue>` в `Application/Common/Caching/`
- `CachingBehavior<TRequest, TValue>` в `Application/Common/Behaviors/`
- `CacheKeys` static class в `Application/Common/Constants/`
- Redis connection string: `ConnectionStrings:Redis` в `appsettings.json`
- Пакети: `Microsoft.Extensions.Caching.StackExchangeRedis` (Infrastructure), `Microsoft.Extensions.Caching.Abstractions` (Application)



## ADR-006: Offset-based пагінація через PaginatedResult<T> + PaginationRequest

**Рішення:** Offset-based пагінація (skip/take). Спільні класи
PaginatedResult<T> і PaginationRequest в Application.Common.Pagination.

**Чому:**
- Достатньо для LMS без мільйонів записів
- PaginationRequest з Math.Clamp(PageSize, 1, 100) захищає від зловживань
- Cursor-based — overkill для цього проєкту

**Деталі:**
- PageIndex — zero-based
- MaxPageSize = 100, DefaultPageSize = 20
- PaginatedResult містить TotalCount, TotalPages, HasNextPage, HasPreviousPage

---

## ADR-007: Audit fields через EF SaveChanges interceptor

**Рішення:** CreatedAt / UpdatedAt встановлюються автоматично через
EF SaveChanges interceptor. Properties мають private set —
interceptor встановлює через EF ChangeTracker (без рефлексії,
EF нативно підтримує private setters).

**Чому:**
- Жоден handler не забуде встановити дату
- Логіка в одному місці, не розмазана по всіх commands
- Private set — ніхто окрім interceptor не змінить значення випадково

---

## ADR-008: CacheKeys в Application layer, не Domain

**Рішення:** `CacheKeys` константи живуть в `Learnix.Application.Common.Constants.CacheKeys`, а не в `Learnix.Domain.Constants`.

**Чому:**
- Кешування — інфраструктурна турбота. Domain не повинен знати що десь є Redis
- Domain має залишатись максимально чистим від крос-cutting concerns

**Альтернативи:**
- Лишити в Domain — працює, але змішує рівні абстракцій

---

## ADR-009: DbContext сам реалізує IUnitOfWork

**Рішення:** `ApplicationDbContext` реалізує `IUnitOfWork`. Окремого класу `UnitOfWork` немає. DI: `services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>())` — резолв в той самий scope instance.

**Чому:**
- Окремий `UnitOfWork` клас просто делегував би `SaveChangesAsync` в DbContext — зайвий шар indirection
- Application шар все одно бачить тільки `IUnitOfWork`, не DbContext — абстракція зберігається
- Менше файлів, менше DI-реєстрацій, менше шансів облажатись з scopes

**Альтернативи:**
- Окремий `UnitOfWork` клас — канонічний підхід, але додає шар без функціональної цінності

---

## ADR-010: Outbox pattern (Schema & Background Worker)

**Рішення:** Патерн Outbox реалізовано для надійного виконання фонових операцій (confirm/delete blob, надсилання email, оцінка досягнень). Domain events публікуються in-process через `DomainEventsInterceptor` після `SaveChangesAsync`. Критичні background-операції записуються в `OutboxMessage` в тій самій транзакції бази даних.

**`OutboxMessage` entity:**
- `Id`, `Type` (наприклад, `DeleteBlob`, `UnlockAchievement`), `Payload` (JSONB)
- `OccurredAt`, `ProcessedAt?`, `AttemptCount`, `LastAttemptAt?`, `LastError?`, `NextRetryAt?`
- Записується domain event handler в тій самій EF транзакції що і зміни entity

**Outbox worker (background `IHostedService`):**
- Читає `WHERE ProcessedAt IS NULL AND (NextRetryAt IS NULL OR NextRetryAt <= NOW())`
- Викликає `IOutboxMessageDispatcher.DispatchAsync(message)` який маршрутизує до конкретного handler.
- Exponential backoff через `NextRetryAt` при помилках
- **Див. ADR-020:** Механізм диспатчу оптимізовано через PostgreSQL LISTEN/NOTIFY.

---


## ADR-012: Авто-міграції тільки в Development

**Рішення:** `Database.MigrateAsync()` викликається при старті API через extension `app.ApplyMigrationsAsync()` тільки коли `app.Environment.IsDevelopment()`. У staging/prod міграції застосовуються окремим контрольованим кроком (CI/CD або ручний `dotnet ef database update`).

**Чому:**
- Dev: розробник підняв `docker compose up -d` і `dotnet run` — БД готова без додаткових команд. Прискорення feedback loop.
- Prod: авто-міграції створюють race condition при scale-out (декілька instance стартують одночасно), помилка міграції = API не піднімається, руйнівні зміни схеми проходять без human review.

**Альтернативи:**
- Завжди авто-міграції — небезпечно в prod (див. вище).
- Ніколи авто-міграції, навіть в dev — кожен `git pull` з новою міграцією потребує ручного `dotnet ef database update`. Тертя у щоденній роботі.
- `Database.EnsureCreatedAsync()` — несумісно з міграціями, годиться тільки для тестових БД що створюються з нуля.

**Наслідки:**
- Phase D (Deploy): додати окремий CI step для застосування міграцій у staging/prod, або генерувати idempotent SQL script через `dotnet ef migrations script --idempotent` і застосовувати через міграційний tool (Flyway/власний).
- Розробник має бути готовий що при першому `dotnet run` після `git pull` міграції запустяться автоматично — побачить це в консолі через `LogInformation`.


---


## ADR-015: Background job scheduling — IHostedService vs Quartz.NET vs Hangfire

**Рішення:** Для фонових завдань використовуємо `BackgroundService` + `PeriodicTimer` (вбудовано в .NET). Quartz.NET та Hangfire не вводимо поки не виникне конкретна потреба в їхніх можливостях.

**Чому IHostedService достатньо зараз:**
- Всі поточні фонові завдання є idempotent і safe to run on every replica (reconciliation, cleanup, seeding). Паралельний запуск на декількох інстансах не призводить до некоректних результатів.
- Zero additional dependencies — `BackgroundService` є частиною `Microsoft.Extensions.Hosting`.
- Паттерн вже використовується в кодовій базі (RefreshTokenCleanup, OutboxProcessor тощо) — консистентність важливіша за передчасну гнучкість.

**Що вміють Quartz.NET і Hangfire (і чого не вміє IHostedService):**

| Можливість | IHostedService | Quartz.NET | Hangfire |
|---|---|---|---|
| Distributed lock (singleton execution across replicas) | ❌ | ✅ (DB/Redis) | ✅ (DB) |
| Cron-вирази для scheduling | ❌ | ✅ | ✅ |
| Management UI | ❌ | ✅ | ✅ (вбудований) |
| Job persistence (retry after crash) | ❌ | ✅ | ✅ |
| Fire-and-forget з web request | ❌ | ❌ | ✅ |
| Dependency | 0 | Quartz + Quartz.Extensions.Hosting | Hangfire.Core + storage |

**Quartz.NET** — enterprise scheduler, побудований на Unix cron-концепціях. Підходить коли потрібні складні розклади (cron expressions) і distributed locking. Конфігурація вербозна.

**Hangfire** — простіший у налаштуванні. Має вбудований dashboard для моніторингу та ручного перезапуску джобів. Популярний для fire-and-forget завдань з HTTP-запитів (наприклад, надіслати email після реєстрації).

**Ключовий concept — Distributed Lock:**
Якщо API запущений на 3 серверах одночасно (horizontal scaling), `IHostedService` запустить job на ВСІХ 3 серверах паралельно. Quartz.NET та Hangfire вирішують це через distributed lock у спільній БД або Redis: тільки ONE instance виконує job, інші чекають або пропускають тік. Це критично для завдань з side-effects (надсилання email, charge платежу) — дублювання неприпустиме.

**Коли переходити на Quartz.NET або Hangfire:**
- З'являється job, який MUST run exactly once across all replicas (наприклад, надсилання щомісячного дайджесту)
- Потрібен dashboard для моніторингу та ручного retrigger джобів
- Кількість background jobs зростає > ~5–6 і управління ними через `AddHostedService` стає громіздким
- Потрібні складні cron-розклади (перший понеділок місяця, кожен робочий день о 9:00 тощо)

**Наслідки поточного рішення:**
- `CategoryCoursesCountReconciliationService`, `RefreshTokenCleanupHostedService` та інші працюють на кожній репліці паралельно — це прийнятно бо всі вони idempotent.
- При введенні горизонтального масштабування (Phase Deploy) — аудит всіх `IHostedService` на предмет того чи безпечно їх запускати паралельно.
- Якщо MassTransit (ADR-002) буде впроваджено в Phase 6 — частина фонових завдань (email, achievements) перейде до MassTransit consumers. `IHostedService` залишиться для infrastructure-level задач (cleanup, seeding, reconciliation).

---




## ADR-020: Outbox latency — PostgreSQL LISTEN/NOTIFY замість polling-only

> Частково supersedes ADR-010 в частині «Outbox worker (background IHostedService)» — механізм диспатчу повідомлень змінено з чистого polling на push-first з polling fallback.

**Контекст і проблема:**

Початкова реалізація Outbox (ADR-010) використовувала чистий polling: `OutboxProcessorService` з `PeriodicTimer(10s)` щоразу робив SELECT по таблиці `OutboxMessages`. Це працювало для blob-операцій та emails, де затримка 10с була прийнятною.

Проблема стала критичною з появою ланцюгових подій у системі досягнень (ADR-ACHIEVEMENT-001, ADR-ACHIEVEMENT-007):

```
LessonCompleted → SaveChanges
    → DomainEventsInterceptor → outbox: EvaluateLessonCompleted
    → ⏳ до 10с (polling)
    → AchievementEvaluator → UserAchievement.Unlock() → SaveChanges
        → DomainEventsInterceptor → outbox: NotifyAchievementUnlocked
        → ⏳ ще до 10с (polling)
        → SignalR push → toast у браузері
```

Два цикли polling = **до 20 секунд** від завершення уроку до нотифікації про досягнення. Для UX — неприйнятно.

---

**Рішення:** `OutboxProcessorService` тепер прокидається одразу після INSERT в `OutboxMessages` через нативний PostgreSQL механізм `LISTEN/NOTIFY`. Polling з інтервалом 10с залишається як fallback.

**Як працює PostgreSQL LISTEN/NOTIFY:**

PostgreSQL має вбудований lightweight pub/sub механізм, окремий від реплікації та WAL. Він працює на рівні сесії (connection):

1. **NOTIFY** — будь-яка транзакція може виконати `pg_notify('channel_name', 'optional_payload')`. Повідомлення буферизується і відправляється **тільки після COMMIT** транзакції. Якщо транзакція відкочується — notification не відправляється. Це ключова гарантія: processor отримує сигнал лише про committed дані.

2. **LISTEN** — клієнт (NpgsqlConnection) реєструється на каналі. Після цього будь-який `NOTIFY` на цьому каналі з будь-якого з'єднання доставляється як подія до всіх LISTEN-підписників. PostgreSQL гарантує доставку до всіх активних підписників на момент COMMIT.

3. **Обмеження:** Якщо підписник відключений в момент NOTIFY — повідомлення втрачається. LISTEN/NOTIFY не має persistence (на відміну від message broker). Саме тому polling залишається як fallback: навіть якщо listener був відключений, processor підхопить повідомлення на наступному 10-секундному тіку.

**Архітектура реалізації (3 компоненти):**

**1. PostgreSQL trigger (database layer):**

```sql
CREATE FUNCTION notify_outbox_insert() RETURNS trigger AS $$
BEGIN
  PERFORM pg_notify('outbox_new', '');
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_outbox_notify
  AFTER INSERT ON "OutboxMessages"
  FOR EACH STATEMENT EXECUTE FUNCTION notify_outbox_insert();
```

`FOR EACH STATEMENT` (не `FOR EACH ROW`) — якщо один `SaveChanges` записує 5 outbox-повідомлень, trigger спрацьовує один раз. Payload порожній — потрібен лише факт «є нові повідомлення», конкретні ID не потрібні, бо processor робить свій SELECT з фільтром.

**2. `OutboxNotificationListener` (Infrastructure BackgroundService):**

Dedicated long-lived `NpgsqlConnection` (не з пулу!) слухає канал `outbox_new`:

```csharp
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync(ct);
await using var cmd = new NpgsqlCommand("LISTEN outbox_new", connection);
await cmd.ExecuteNonQueryAsync(ct);

while (!ct.IsCancellationRequested)
    await connection.WaitAsync(ct);  // blocks until notification arrives
```

Чому dedicated connection: PostgreSQL LISTEN state прив'язаний до конкретної сесії. Connection pooling (Npgsql `NpgsqlDataSource`) повертає з'єднання в пул після використання — LISTEN state втрачається. Тому listener відкриває окреме з'єднання, яке живе весь lifetime додатку.

При розриві з'єднання — автоматичний reconnect з exponential backoff (1с → 2с → 4с → ... → 30с cap). Під час reconnect polling fallback забезпечує доставку.

**3. `OutboxSignal` (in-process bridge):**

`SemaphoreSlim` singleton, що зв'язує listener і processor. Listener викликає `signal.Notify()` при отриманні PG notification. Processor чекає `signal.WaitAsync(10s, ct)` — повертається одразу при сигналі або через 10с (fallback).

Додатково: processor сам сигналить себе (`signal.Notify()`) якщо обробив **хоча б одне повідомлення** (`messages.Count > 0`). Це гарантує миттєву обробку каскадних подій (наприклад, коли під час обробки одного повідомлення генерується інше — `NotifyAchievementUnlocked`), не чекаючи на новий сигнал від бази чи 10с таймаут.

**Результат:**

| Сценарій | Polling-only | LISTEN/NOTIFY + fallback |
|---|---|---|
| Single-hop (email, blob) | до 10с | < 100ms |
| Achievement chain (2 hops) | до 20с | < 500ms |
| Idle load (немає повідомлень) | SELECT кожні 10с | SELECT кожні 10с |
| Нові залежності | — | 0 (Npgsql вже є) |

---

**Альтернативи що розглядались:**

1. **Зменшити polling interval до 1с** — найпростіше, але 1 SELECT/с на порожній таблиці = зайве навантаження. При scale-out (N instances) це N SELECT/с. Не масштабується.

2. **In-process SemaphoreSlim без PostgreSQL** — сигналити з `DomainEventsInterceptor` напряму (без PG trigger). Працює для single-instance, але при горизонтальному масштабуванні instance A записує outbox message, а instance B (де крутиться processor) не отримає сигнал. PG LISTEN/NOTIFY працює cross-connection і cross-process.

3. **Debezium CDC (Change Data Capture)** — Debezium підключається до PostgreSQL WAL і стрімить зміни в Kafka topic. Це production-grade рішення для мікросервісів. Відхилено: потребує Kafka + Debezium connector + Kafka consumers — disproportionate для моноліту. Правильний вибір при переході на мікросервісну архітектуру.

4. **Wolverine framework** — .NET application framework з вбудованим LISTEN/NOTIFY outbox. Відхилено: Wolverine замінює MediatR і має свій pipeline — це не drop-in рішення, а міграція всієї архітектури.

5. **CAP library** — lightweight event bus з вбудованим outbox. Відхилено: вводить власні абстракції (`ICapPublisher`, `ICapSubscribe`), власну outbox таблицю. Конфлікт з існуючою outbox реалізацією.

6. **Hybrid: optimistic dispatch + outbox as safety net** (NServiceBus підхід) — після COMMIT спробувати одразу відправити повідомлення in-process, outbox як fallback при crash. Відхилено для поточної архітектури: потребує зміни в Application layer (handler повинен знати про dispatch), що суперечить розділенню шарів.

---

**Наслідки:**

- Migration `AddOutboxNotifyTrigger` створює PL/pgSQL функцію і trigger.
- `OutboxNotificationListener` в `Infrastructure/Services/` — окремий `BackgroundService`.
- `OutboxSignal` в `Infrastructure/Outbox/` — singleton `SemaphoreSlim` wrapper.
- `OutboxProcessorService` змінено: `PeriodicTimer` → `outboxSignal.WaitAsync(10s)`.
- Один додатковий PostgreSQL connection (не з пулу) для LISTEN — мінімальний resource footprint.

**Scale-out safety (`FOR UPDATE SKIP LOCKED`):**

Outbox processor використовує `SELECT ... FOR UPDATE SKIP LOCKED` замість звичайного SELECT:

```sql
SELECT * FROM "OutboxMessages"
WHERE "ProcessedAt" IS NULL AND "NextRetryAt" <= {now}
ORDER BY "OccurredAt"
LIMIT {batch_size}
FOR UPDATE SKIP LOCKED
```

- `FOR UPDATE` — лочить вибрані рядки на рівні PostgreSQL транзакції. Інші транзакції не можуть їх SELECT FOR UPDATE до COMMIT.
- `SKIP LOCKED` — якщо рядок вже залочений іншим інстансом, пропустити його замість очікування (на відміну від `NOWAIT`, який кидає помилку).
- **Timestamp rounding buffer:** Змінна `{now}` розраховується як `DateTime.UtcNow.AddSeconds(1)`. Це обходить проблему мікросекундного округлення PostgreSQL (`timestamp` має точність 1us, а `.NET DateTime` — 100ns), яке могло призводити до того, що щойно вставлене повідомлення отримувало `NextRetryAt` на мікросекунду в майбутньому і пропускалося запитом.
- Результат: Instance A бере повідомлення 1–10, Instance B бере 11–20. Ніякого дублювання.
- Саме цей механізм використовують MassTransit, Wolverine, і NServiceBus для своїх outbox реалізацій.

Весь batch обгорнутий в explicit transaction (`BeginTransactionAsync` → `CommitAsync`), щоб лок тримався під час обробки повідомлень. `pg_notify` від нових outbox-повідомлень (створених під час обробки, наприклад `NotifyAchievementUnlocked`) буферизується PostgreSQL і доставляється тільки після COMMIT зовнішньої транзакції.

---

## ADR-021: Embedded Resources для Data Seeding

**Рішення:** Активи (зображення та відео), необхідні для сідингу бази даних (курси, уроки, аватари), зберігаються безпосередньо у збірці `Learnix.Infrastructure` як Embedded Resources, а не у файловій системі чи у вигляді Base64-рядків у коді. Під час завантаження у Blob Storage кожен згенерований об'єкт (курс чи відео-урок) отримує власну унікальну копію файлу (генерується унікальний `blobPath`).

**Чому:**
- **Усунення залежності від середовища:** Код сідера не залежить від файлової системи хоста чи поточного робочого каталогу (що є проблемою при запуску в Docker контейнерах або під час тестів). Файли гарантовано завжди присутні разом зі збіркою.
- **Розмір коду:** Попередній підхід використовував великі Base64-рядки прямо у C# коді, що сильно забруднювало код, ускладнювало читання та призводило до великих розмірів `.cs` файлів.
- **Ізоляція даних:** Під час сідингу кожен курс або урок отримує власний унікальний шлях у Blob Storage, куди стрімиться відповідний ресурс зі збірки. Це запобігає конфліктам (наприклад, випадковому видаленню спільного `placeholder.mp4` через адмін-панель).

**Альтернативи:**
- **Base64-константи в коді (старе рішення):** Забруднює C#-файли, складно підтримувати великі файли (відео або багато зображень). Відхилено.
- **Читання з файлової системи (`File.ReadAllBytes`):** Потребує правильного налаштування `Copy to Output Directory`, шляхи можуть ламатися при запуску з різних директорій.
- **Спільний Blob Storage об'єкт:** Завантаження одного `placeholder.mp4` в Blob Storage і посилання на нього з усіх уроків. Відхилено: у випадку, коли інструктор замінює або видаляє відео в одному уроці, система очищення (Outbox `DeleteBlob`) видалила б файл з Blob Storage, що призвело б до зламаних посилань (404) в інших уроках, які посилаються на той самий спільний файл.

**Наслідки:**
- Файли додані у папку `Learnix.Infrastructure/Assets/` та налаштовані як `<EmbeddedResource>` у `Learnix.Infrastructure.csproj`.
- `CourseSeederHostedService` та `StudentSeederHostedService` використовують `Assembly.GetExecutingAssembly().GetManifestResourceStream()` для доступу до файлів під час завантаження в Blob Storage.
- Кожна завантажена копія отримує `Guid.NewGuid()` у шляху (`blobPath`), що гарантує унікальність і безпечність видалення.

---

## ADR-022: PII Masking in Application Logs

**Context:**
Під час аудиту безпеки виявлено, що сервіс відправки листів (`SmtpEmailSender`) логував повні email-адреси користувачів на рівні `Information` (наприклад, `logger.LogInformation("Email sent to Oleh123@gmail.com")`). У production середовищі ці логи можуть передаватися в централізовані системи (ELK, Datadog), де доступ до них матиме широке коло розробників. Логування персональних даних (Personally Identifiable Information - PII) у відкритому вигляді створює ризики безпеки та порушує compliance (GDPR).

**Decision:**
Впровадити правило маскування PII у всіх логах додатку. 
Для email-адрес використовувати часткове приховування символів замість повного вирізання або хешування, оскільки часткове приховування (наприклад, `O***@gmail.com`) зберігає достатньо контексту для дебагінгу і пошуку проблем адміністраторами, але не розкриває повну адресу.
Будь-який сервіс, що логує чутливі дані (email, телефони, IP-адреси), зобов'язаний застосовувати функції маскування перед записом у `ILogger`.

**Consequences:**
- Безпека: Знижується ризик витоку PII через системи агрегації логів.
- Дебагінг: При повному маскуванні дебагінг ускладнюється, тому прийнято компромісний підхід із відображенням першої літери та домену.
- Додаткові зусилля: Розробники повинні бути свідомими щодо даних, які вони логують.

