# Learnix — Frontend Architecture Decision Records (Forms & Error Handling)

> Формат: що вирішили → чому → які альтернативи відкинули.
> Архітектурні рішення беку — в [DECISIONS.md](./DECISIONS.md).

---

## FADR-FORM-001: Форми — Zod schemas + FormValues окремо від DTO

**Рішення:**
- Zod schema — **single source of truth** для валідації і типу **форми** (`FormValues`)
- DTO — окремий TypeScript інтерфейс в `types/`, співпадає з бекенд-контрактом
- Трансформація `FormValues → DTO` — **явна**, в `onSubmit`

**Чому:**
- `FormValues` і `DTO` часто розходяться: поле `tagsInput` (string для зручного вводу) ≠ `tags: string[]` (формат бекенду). Якщо робити один тип — або UX страждає, або трансформація ховається в resolver
- Zod-inference для DTO = runtime-валідація на кожен response з бекенду (overkill)
- Явна трансформація легше дебажиться і рефакториться

**Наслідки:**
- Zod схеми не використовуються для типізації response-ів з бекенду
- `types/` — ручні інтерфейси для DTO, одна папка на домен (`course.types.ts`, `user.types.ts`)

---

## FADR-FORM-002: Error handling — три рівні, ProblemDetails mapping

**Рішення:** Три рівні обробки помилок: field-level у формах, toast для бізнес-помилок, Error Boundary для crash. Єдиний шлях мапінгу `ProblemDetails → UI`.

**Рівень 1 — field-level (у формах):**
Функція `isValidationError` парсить `ProblemDetails` з `errors` і перетворює їх на масив повідомлень для React Hook Form.

**Рівень 2 — toast для бізнес-помилок:**
Використовується через `toast.success()`, `toast.error()`, `toast.info()` з будь-якого місця коду. Глобальний handler для React Query (mutations) виводить toast для всіх `isValidationError === false`.

**Рівень 3 — Error Boundary для JS crashes:**
Компонент `ErrorBoundary` обгортає root або частини дерева, щоб ловити краші на клієнті.

**Мапінг статусів на UI:**

| HTTP | Що це | Де показуємо |
|---|---|---|
| 400 з `errors` | Validation з бекенду | Inline під полями форми |
| 400 без `errors` | Business error | Toast error |
| 401 | Token expired | Interceptor робить silent refresh — юзер не бачить |
| 403 | Немає прав | Toast + redirect на `/forbidden` |
| 404 | Not found | Toast або NotFoundPage (залежить від контексту) |
| 409 | Conflict | Toast error (наприклад "Already enrolled") |
| 500+ | Server error | Toast "Something went wrong" + опціонально Error Boundary |

**Чому:**
- Різні помилки потребують різної UX: validation у формі ≠ network error
- Глобальний handler в React Query знімає boilerplate з кожного handler'а
- ProblemDetails (RFC 7807) — стандарт на беку, прямо мапиться на UI
