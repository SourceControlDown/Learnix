# Learnix — ADR: Автентифікація та авторизація

> Формат: що вирішили → чому → які альтернативи відкинули.
> Оновлюється після кожного чату, де приймались архітектурні рішення.

Суміжні файли: [DECISIONS_ARCHITECTURE.md](DECISIONS_ARCHITECTURE.md) · [DECISIONS_DOMAIN.md](DECISIONS_DOMAIN.md) · [DECISIONS_INFRA.md](DECISIONS_INFRA.md)

## Конвенція статусів

ADR не видаляються. Якщо рішення переглянуто — старий ADR помічається `Superseded by ADR-XXX`, новий — `Supersedes ADR-YYY`. Це зберігає історію мислення і показує як архітектура еволюціонувала.

---

## ADR-001: JWT (short-lived) + Refresh Token (long-lived, HttpOnly cookie)

**Рішення:** Аутентифікація через пару токенів:
- **Access token (JWT):** 15 хвилин, передається в `Authorization: Bearer` header
- **Refresh token:** 7 днів, зберігається в HttpOnly + Secure + SameSite=Strict cookie

**Чому:**
- Короткий JWT мінімізує вікно компрометації — навіть якщо токен вкрадено, він живе 15 хв
- HttpOnly cookie захищає refresh token від XSS (JavaScript не має доступу)
- Rotation: кожен refresh видає нову пару (access + refresh), старий refresh інвалідується
- Хешування refresh token в БД (SHA-256) — навіть при витоку БД токени не compromised

**Альтернативи:**
- Session-based auth — простіше, але погано масштабується горизонтально без sticky sessions
- JWT only (довгоживучий, без refresh) — небезпечно, немає механізму відкликання
- OAuth2 + OpenID Connect (IdentityServer) — overkill для монолітного LMS

**Деталі:**
- Refresh token зберігається в таблиці `RefreshToken` (PostgreSQL), хешований
- При кожному refresh: старий токен ревокується, створюється новий
- Якщо використовується вже ревокований токен → ревокуються ВСІ токени юзера (захист від replay attack)
- Google OAuth: після успішного OAuth flow сервер сам генерує JWT + refresh token пару

---

## ADR-002: ASP.NET Identity — наслідувати IdentityUser, свій DbContext

**Рішення:** User entity наслідує IdentityUser<Guid>.
Використовуємо свій ApplicationDbContext, не IdentityDbContext.
Instructor-specific дані — НЕ в claims.

**Instructor-specific дані:** Якщо потрібні — nullable поля на User entity.
Окрема таблиця InstructorProfile — out of scope для v1.

**Чому:**
- Identity дає безкоштовно: password hashing, token generation,
  lockout, email confirmation, external logins
- Свій DbContext = контроль над схемою, без зайвих таблиць
- Claims — для auth metadata (роль, permissions), не для бізнес-даних

**Альтернативи:**
- Identity повністю з коробки (IdentityDbContext) — тягне зайві таблиці,
  менше контролю
- Без Identity взагалі — місяці на написання auth вручну без користі

---

## ADR-003: Чисті Identity ролі замість UserRole enum

**Рішення:** Видалено `UserRole` enum з Domain. Ролі (Student / Instructor / Admin) живуть тільки в Identity (`AspNetRoles` + `AspNetUserRoles`). `Domain.Constants.Roles` — статичний клас з string-константами для типобезпечного посилання.

**Чому:**
- Дублювання enum-поля на User + Identity ролей — два источника правди, неминучий розсинхрон
- `[Authorize(Roles = "Instructor")]` працює з Identity з коробки
- JWT claims автоматично заповнюються Identity з ролей
- Менше коду, менше шансів на помилку

**Альтернативи:**
- Тільки enum на User, без Identity ролей — втрачаємо `[Authorize(Roles=...)]` і вбудовану підтримку, треба писати свій authorization handler
- Гібрид: enum + Identity, синхронізація через domain method — попередня рекомендація, відкинуто, бо дублювання навіть для 3 значень не вартує. Якщо додасться 4-та роль — треба міняти в двох місцях

---

## ADR-004: IIdentityService як абстракція над UserManager

> **Status:** Superseded by ADR-006 (декомпозиція на три інтерфейси). Оригінальний принцип "Application не знає про UserManager" лишається в силі, але реалізований через три окремі інтерфейси замість одного `IIdentityService`. Цей ADR лишається для історичного контексту.

**Рішення:** Інтерфейс `IIdentityService` живе в Application, реалізація `IdentityService` в Infrastructure. Application handlers не знають про `UserManager<User>` — викликають тільки `IIdentityService`.

**Чому:**
- `UserManager<User>` залежить від `IUserStore` → EF Core → це Infrastructure concern
- Прямий виклик `UserManager` з handler у Application — порушення Clean Architecture (Application залежить від Infrastructure через MS.AspNetCore.Identity.EntityFrameworkCore)
- Інтерфейс дає чітку межу: Application говорить "зареєструй / підтверди email", Infrastructure знає як саме (через Identity або інакше)

**Альтернативи:**
- Прямий виклик `UserManager` з handler — простіше, але порушує dependency rule
- Загорнути Identity в окремий "Auth module" — overengineering для одного сервісу

**Наслідки:**
- Усі auth-related handlers викликають `IIdentityService`, не `UserManager` напряму
- Тестування handlers — мокаємо `IIdentityService`, не Identity інфраструктуру

---

## ADR-005: JWT секрет — placeholder в base + dev-секрет в Development + env var в production

**Рішення:** `appsettings.json` містить `Jwt.Secret = ""` (placeholder, валідація на старті падає якщо він порожній). `appsettings.Development.json` перевизначає його статичним рандомним рядком (>32 байт). У production значення передається через змінну оточення `JWT__Secret` (double underscore = nested config key в .NET configuration).

**Чому:**
- Розробник підняв `dotnet run` без зайвих кроків — БД мігрується автоматично (DECISIONS_INFRA.md ADR-012), JWT не падає на старті
- `appsettings.Development.json` ніколи не йде в production билд — низький ризик витоку
- Production-секрет ніколи не торкається диска чи git — тільки runtime env var (Azure Key Vault → App Service config → env var)
- Перевірка `string.IsNullOrWhiteSpace(jwtSettings.Secret)` в `AddInfrastructure` — fail fast, краще впасти на старті ніж випустити токени підписані порожнім ключем

**Альтернативи:**
- Завжди через env var (включно з dev) — кожен новий розробник має окремо налаштувати `.env` чи user-secrets перед першим запуском. Тертя в onboarding
- User Secrets (`dotnet user-secrets`) для dev — канонічний MS-підхід, але ховає секрет в окремому місці що ускладнює дебаг "звідки взялось значення"
- Hardcoded fallback у коді (`?? "default-dev-secret"`) — небезпечно, легко проґавити в production-збірці

**Наслідки:**
- `appsettings.json`: секція `Jwt` з порожнім `Secret`
- `appsettings.Development.json`: перевизначення `Jwt.Secret` рандомним рядком
- `.env.example`: рядок `JWT__Secret=<generate 64+ char secret>` з коментарем "production only"
- `AddInfrastructure`: явна перевірка наявності секрету з `InvalidOperationException` при порожньому

---

## ADR-006: Декомпозиція Identity сервісу на три ролі за принципом "одна причина змінитись"

> **Supersedes:** ADR-004 (один IIdentityService → три інтерфейси)

**Рішення:** Замість одного `IIdentityService` — три інтерфейси:
- `IUserRegistrationService` — реєстрація + email-підтвердження (CRUD-life-cycle юзера)
- `IUserAuthenticationService` — валідація креденшелів + вибірка info для побудови токена
- `ITokenService` — генерація JWT + refresh token + хешування (чиста функція без знання про БД чи Identity)

Усі три живуть у `Auth/Abstractions/` (DECISIONS_ARCHITECTURE.md ADR-009). Реалізації — в `Infrastructure/Identity/`.

**Чому:**
- **Different reasons to change.** Заміниш Identity provider (на Auth0/IdentityServer) — переписуєш `UserRegistration` + `UserAuthentication`, `TokenService` не знає. Поміняєш JWT на PASETO або змінити claims — переписуєш `TokenService`, решта не торкається. Single Responsibility у дії
- **Тестабельність.** Login handler у unit-тесті мокаєш три легких інтерфейси, а не один товстий
- **Читабельність handler.** `LoginCommandHandler` явно показує оркестрацію: validate → generate token pair → persist refresh → save. Кожен крок — окрема залежність

**Альтернативи:**
- Один `IIdentityService` з усіма методами — простіше менше файлів, але товстий контракт що змінюється з кожної причини
- `IUserAuthenticationService.LoginAsync` повертає одразу JWT — змішує валідацію credentials з token generation, два різних concern в одному методі

**Наслідки:**
- 3 окремих DI-реєстрації в `AddInfrastructure`
- `IdentityService.cs` (старий) видалено, на його місці `UserRegistrationService.cs` + `UserAuthenticationService.cs` + `JwtTokenService.cs`
- Старі handlers (Register, ConfirmEmail, ResendConfirmationEmail) оновлено — параметр конструктора з `IIdentityService` на `IUserRegistrationService`

---

## ADR-007: Refresh token rotation з replay-attack protection

**Рішення:** При кожному успішному `/api/auth/refresh` — старий refresh token revoked (не видаляється), новий створюється і повертається. Якщо приходить запит з токеном що **уже revoked** — це сигнал компрометації: всі активні токени юзера примусово revoked, юзер вилогінюється з усіх пристроїв, інцидент логується як warning з UserId.

Refresh tokens зберігаються в PostgreSQL у вигляді SHA-256 хешу (`TokenHash`, унікальний індекс). Plain-токен існує тільки в HttpOnly cookie клієнта. Витік БД ≠ компрометація сесій.

Refresh token передається через HttpOnly + Secure + SameSite=Strict cookie з `Path = "/api/auth"`. Контролер відповідає за читання/запис cookie; handlers оперують голим рядком — Application шар нічого не знає про HTTP.

**Чому:**
- Rotation робить вікно компрометації мінімальним — токен живе один запит
- Replay protection ловить сценарій "токен украли, обидва (атакувальник і юзер) ним користуються" — перший хто refresh-ить отримує новий, другий приходить зі старим (revoked) → всіх кидає в logout
- Хешування в БД — захист від витоку дампа БД
- Path-обмежений cookie не відправляється з кожним запитом до API, тільки до auth-endpoints — менше експозиції

**Альтернативи:**
- Refresh без rotation (один довгоживучий токен) — простіше, але втрачаємо replay-detection
- Refresh tokens у Redis — швидше, але втрачаємо durability (перезапуск Redis = всі юзери вилогінено)
- JWT як refresh теж — symmetric з access, але втрачаємо центральний контроль revocation (JWT не можна "забрати назад")

**Наслідки:**
- `RefreshToken` entity з `TokenHash`, `ExpiresAt`, `IsRevoked`, `RevokedAt`
- `IRefreshTokenRepository` + специфікації `RefreshTokenByHashSpecification`, `ActiveRefreshTokensByUserSpecification`
- `RefreshTokenCleanupHostedService` (B-11.5) — фонова задача чистить токени старші `ExpiresAt + 7 днів` раз на 24h
- Контролер `AuthController` керує cookie (`SetRefreshTokenCookie`, `ClearRefreshTokenCookie`) — handlers ні

---

## ADR-008: JWT claims — стандартні OIDC + custom для ролей

**Рішення:** Access token містить:
- `sub` — User Id (Guid)
- `email` — User email
- `jti` — унікальний id токена (для трейсингу/blacklist у майбутньому)
- `given_name` — FirstName
- `family_name` — LastName
- `name` — `"{FirstName} {LastName}"` (повне ім'я для display)
- `role` — повторюваний claim для кожної ролі юзера

`MapInboundClaims = false` у `AddJwtBearer` — щоб у коді API ми бачили claim names такими ж як у JWT (не перетворені на `ClaimTypes.NameIdentifier` тощо). `NameClaimType = "name"`, `RoleClaimType = "role"` — щоб `User.Identity.Name` і `[Authorize(Roles = "Instructor")]` працювали з нашими custom-claim-names.

**Чому:**
- Стандартні OIDC claims (`sub`, `email`, `given_name`, `family_name`, `name`) — фронтенд або сторонні системи що очікують OIDC отримають що очікують
- Окремі `given_name` + `family_name` + composite `name` — фронт може взяти будь-яке поле без додаткового парсингу
- `role` як повторюваний claim — стандартний механізм Identity, працює з `[Authorize(Roles = ...)]` за замовчуванням
- `MapInboundClaims = false` — узгодженість: те що у JWT == те що бачимо у коді. Дебаг простіший

**Альтернативи:**
- Тільки `name` без розбиття — фронту доводиться парсити "First Last", ламається на іменах з пробілами/множинних
- Власні короткі claim names (`uid`, `r`) для зменшення розміру токена — економія мінімальна, втрата сумісності з OIDC tooling
- `ClaimTypes.*` URI-based claim names (.NET default) — multi-kilobyte токени, погана читабельність

**Наслідки:**
- `JwtTokenService.GenerateAccessToken` приймає `firstName, lastName` (не тільки `firstName` як в першій ітерації)
- `UserAuthenticationInfo` містить обидва імені
- `AddJwtBearer` сконфігурований з `MapInboundClaims = false`, `NameClaimType`, `RoleClaimType`

---

## ADR-009: Розділення `AuthenticationError` (401) і `ForbiddenError` (403)

**Рішення:** Створено окремий typed error `AuthenticationError : Error` для 401 Unauthorized.
`ForbiddenError` тепер семантично відповідає 403 Forbidden — "автентифікований, але не має прав".

- `AuthenticationError` — invalid credentials, expired/replay refresh token, missing/invalid access token, locked out, unconfirmed email при логіні. Мапиться на 401.
- `ForbiddenError` — юзер автентифікований, але не має права на операцію (наприклад, Student намагається редагувати чужий курс). Мапиться на 403.

**Чому:**
- HTTP 401 і 403 семантично різні. RFC 9110 розділяє їх чітко: 401 = "treba автентифікуватись", 403 = "автентифікація є, але не допомагає"
- Попередня реалізація мапила `ForbiddenError` на 401 у контролері — це працювало, але плутало читача (ім'я типу → 403, маппінг → 401)
- Окремі типи дозволяють `result.ToActionResult()` extension працювати однозначно без перевизначень на рівні action

**Альтернативи:**
- Залишити один `ForbiddenError` і мапити на різні коди залежно від контексту — магія у mapping, сервіс не знає який код поверне його помилка
- Error codes (enum) в одному типі — менш виразно, втрачає compile-time check

**Наслідки:**
- `ResultExtensions.ToActionResult` має окремі гілки для обох типів
- Існуючі handlers (`UserAuthenticationService`, `RefreshTokenCommandHandler`) мігровано на `AuthenticationError`
- Майбутні authorization checks (роль-базовані) використовуватимуть `ForbiddenError`

---

## ADR-010: Google OAuth через Google Identity Services (ID token) замість OAuth code flow

**Рішення:** Фронтенд отримує Google ID token через Google Identity Services (GIS) SDK прямо в браузері. Бек отримує token на `POST /api/auth/google`, валідує через `Google.Apis.Auth` (`GoogleJsonWebSignature.ValidateAsync`) і видає свої JWT+refresh. Authorization Code flow з redirect_uri на беці і Client Secret **не використовуємо**.

**Чому:**
- GIS — sanctioned Google's підхід для SPA з 2022+. Простіший, менше рухомих частин.
- Client Secret не потрібен — ID token це self-contained JWT підписаний Google private key, бек валідує публічним ключем з JWKS. Secret потрібен тільки для обміну authorization code.
- Немає redirect endpoint на беці → нема окремої машинерії для callback, state parameter, CSRF protection на callback.
- ID token уже містить `email`, `email_verified`, `given_name`, `family_name`, `sub` — все що нам треба для find-or-create. Додаткових запитів на Google userinfo endpoint не робимо.

**Альтернативи:**
- **Authorization Code flow** — класика, "виглядає стандартніше" на інтерв'ю, але для SPA це anti-pattern у 2026. Потребує Client Secret, redirect endpoint, обміну code → tokens.
- **Implicit flow** — deprecated Google'ом, не обговорюється.

**Наслідки:**
- `GoogleSettings.ClientId` — єдине що треба сконфігурувати. `ClientId` публічний (виявиться в front-end коді), не secret.
- Endpoint `POST /api/auth/google` приймає `{ idToken }` → валідує → видає пару токенів (та сама `LoginResponse` як regular login).
- Якщо Google колись deprecates GIS — треба буде переписати на Authorization Code flow. Ризик низький: GIS — це їх стратегічний напрямок.

---

## ADR-011: `GoogleId` як денормалізоване поле на User замість `AspNetUserLogins`

**Рішення:** External provider linkage зберігається як `User.GoogleId` (nullable `string?`), не через Identity таблицю `AspNetUserLogins` / `UserManager.AddLoginAsync`.

**Чому:**
- У v1 Learnix тільки один external provider (Google). `AspNetUserLogins` — це таблиця для N провайдерів `(Provider, ProviderKey)`. Для одного — overhead без користі.
- `WHERE GoogleId = ?` — один простий lookup без join на `AspNetUserLogins`.
- Менше EF-конфігурації, менше рухомих частин у Identity schema.

**Альтернативи:**
- **`AspNetUserLogins` через `UserManager.AddLoginAsync`** — канонічний Identity шлях. Переваги: масштабується на N провайдерів з нульовими змінами коду (GitHub, Microsoft). Недолік: join на кожному Google login lookup.
- **Гібрид: `GoogleId` для швидкого lookup + запис в `AspNetUserLogins`** — дублювання, розсинхрон можливий.

**Наслідки:**
- Додавання другого external provider (GitHub, Microsoft) — це міграція `string? GoogleId` → `AspNetUserLogins`-based flow. Помітна робота: нова міграція схеми, перенесення даних, переписування `FindOrCreateGoogleUserAsync` на polymorphic `FindOrCreateExternalUserAsync`.
- Обмежено однопровайдерним сценарієм — документовано як свідомий tradeoff.

**Future work:** при додаванні другого провайдера — рефакторити на `UserManager.AddLoginAsync` + `FindByLoginAsync`. Задача в TODO як `B-XX` (поза v1).

---

## ADR-012: Rate limiting — in-memory FixedWindow per IP, один strict policy

**Рішення:** Sensitive auth endpoints (register, login, google login, forgot-password, reset-password, resend-confirmation, confirm-email) лімітовані вбудованим `Microsoft.AspNetCore.RateLimiting` — **5 запитів на 15 хвилин per IP**, FixedWindowLimiter, `QueueLimit = 0`. Refresh, logout не лімітовані. Перевищення → 429 `ProblemDetails` + `Retry-After` header.

**Чому:**
- `Microsoft.AspNetCore.RateLimiting` — вбудований у .NET 8, нуль додаткових NuGet, підтримується Microsoft. AspNetCoreRateLimit — це legacy з часів .NET Core 2.
- FixedWindow — найпрозоріший для юзера ("5 спроб на 15 хв, потім reset"). SlidingWindow і TokenBucket не дають переваг для sensitive auth де нам треба **strict cap**, а не smooth rate.
- `QueueLimit = 0` — юзер що перебрав ліміт одразу отримує 429, не зависає у черзі.
- Refresh без лімітування — легітимний клієнт з 3 вкладками може зробити 3 одночасних refresh при wake з sleep; strict ліміт створював би false positives без security-переваги (replay detection і так працює в `RefreshTokenCommandHandler` через ADR-007).
- Per-IP партиціонування (не per IP+email) — простіше, достатньо для портфоліо. Per-user партиціонування потребує юзер знайдений на цій стадії — але rate limit застосовується **до** auth, коли юзера ще нема.

**Альтернативи:**
- **AspNetCoreRateLimit (NuGet)** — legacy, більше коду, менше підтримки.
- **Redis-backed distributed rate limiter** — правильно для scale-out. Поза scope v1: монолітний деплой на одному Container App instance → in-memory достатньо. Додавати при переході на multi-instance (Phase 10+).
- **Per IP+email для login** — захищає конкретний акаунт від brute force при розподіленій атаці з багатьох IP. Trade-off: складніше, вимагає нестандартного partitioning key (email з body). Для v1 не виправдано.

**Наслідки:**
- In-memory counters — **при scale-out лічильники розсинхронізовані**. Атакувальник може отримати 5×N спроб на N instance. Документований трейд-оф, поки один instance — не критично.
- `HttpContext.Connection.RemoteIpAddress` за reverse proxy повертає IP проксі, не клієнта. При деплої в Azure App Service / Container Apps **обов'язково** додати `UseForwardedHeaders()` — інакше всі юзери отримують один partition key і rate limit стає одним глобальним лічильником. Задача D-06.5 у TODO.

---

## ADR-013: Authorization checks live in handlers, not controllers

**Рішення:** Перевірки "чи може поточний користувач виконати цю операцію над цим ресурсом" (owner check, role check) виконуються в command/query handlers через `ICurrentUserService`. Контролер не бере на себе цю відповідальність — він робить тільки HTTP-речі (read body, return ToActionResult).

**Чому:**
- Контролер не знає про `course.InstructorId` без fetch'у через repository. Якщо контролер робить fetch — він уже фактично робить частину роботи handler'а → порушення SRP
- ASP.NET `[Authorize(Policy = ...)]` добре працює для статичних правил (роль у claims, claim value). Owner-check вимагає fetch resource → resource-based authorization → динамічно → природне місце — handler
- Handler повертає `Result.Fail(new ForbiddenError(...))` → `ToActionResult()` маппить на 403. Узгоджено з існуючим pipeline (DECISIONS_ARCHITECTURE.md ADR-002, ADR-004)
- Одне місце для look — усі business rules видно в одному шарі

**Альтернативи:**
- Authorization в контролері через кастомний `IAuthorizationRequirement + AuthorizationHandler` — офіційний ASP.NET підхід для resource-based auth. Відкинуто: додає шар непрямої взаємодії без виграшу для соло-проекту з одним типом owner-check (`InstructorId`)
- Authorization в domain entity методі (`course.UpdateDetails(..., requestingUserId)`) — змішує знання про identity з бізнес-логікою entity, порушує SRP

**Наслідки:**
- Кожен mutating handler робить 2 перевірки: `currentUser.UserId is null` (401) + owner/admin (403)
- Один додатковий fetch на mutation для entity що й так буде fetched — прийнятний трейд-оф
- При зростанні кількості handler'ів можна винести в extension method: `ResultExtensions.EnsureOwnership(Guid resourceOwnerId, ICurrentUserService user)` — cosmetic refactor, не blocker

---

## ADR-014: Email confirmation soft restriction via ASP.NET Core authorization policy

**Рішення:** Після реєстрації юзер автоматично логіниться, але email залишається непідтвердженим. Persistent banner у UI нагадує підтвердити email. Write-дії з реальним платформним впливом захищені named policy `EmailConfirmed`, яка перевіряє claim `email_verified` у JWT. Непідтверджені юзери можуть вільно переглядати каталог і профіль; конкретні endpoints повертають 403 коли policy не виконується.

**Заблоковані endpoints:**

| Endpoint | Причина |
|---|---|
| `POST /api/enrollments` | Запис на безкоштовний курс — всі downstream дії (прогрес, тести, сертифікати) каскадують від цього gate |
| Stripe checkout (Phase 9) | Запис на платний курс — при успішній оплаті створюється той самий `Enrollment` запис |
| `POST /api/instructor-applications` | Запускає admin review; spam-заявки від неверифікованих email є модераційним ризиком |
| `POST /api/courses/{id}/reviews` | Публічний контент прив'язаний до реальної ідентичності |
| `PUT /api/courses/{id}/reviews/{reviewId}` | Те саме — редагування публічного контенту |
| `POST /api/messages/conversations/start-or-get` | Перша точка людського контакту; вимагає довіри |
| `POST /api/messages/conversations/{id}/messages` | Те саме |

**Вільні (не блокуємо):** перегляд каталогу, сторінка курсу, читання відгуків, профіль (read + edit), AI chat. Прогрес / тести / сертифікати каскадують з Enrollment — окремий gate не потрібен.

**Чому:**
- **Controller-level concern, not domain concern.** "Чи підтверджена ідентичність юзера?" — це питання автентифікації/авторизації, не бізнес-логіки. Природне місце — `[Authorize]` attribute (той самий рівень що і role checks), не всередині handlers — узгоджується з ADR-013, який резервує handler-level auth-перевірки для resource-based (owner) рішень.
- **Один механізм, не сім.** Єдина named policy декорує 7 endpoints. Альтернатива (перевірка `ICurrentUserService.IsEmailConfirmed` в кожному handler) розсіює auth-логіку по Application шару і ускладнює аудит.
- **Soft restriction (не hard block).** Hard-block логіну/доступу до підтвердження email дає високий abandonment rate. Дозволити досліджувати платформу перед підтвердженням — industry standard (Slack, GitHub, Vercel).
- **JWT claim = нуль зайвих DB-запитів.** Claim `email_verified: "true"/"false"` встановлюється під час логіну/реєстрації і живе в токені — без додаткових запитів на кожен request. Frontend зчитує той самий claim для відображення banner.

**Альтернативи:**
- **Hard-block логіну** — максимальна безпека, але висока ціна: UX погіршується, abandonment зростає. Відкинуто.
- **Перевірка `IsEmailConfirmed` в кожному handler** — auth concern потрапляє в Application шар, порушує принцип "auth at the gate". Також ускладнює аудит: перевірки розсіяні по 7 handlers. Відкинуто.
- **Middleware з перевіркою конкретних шляхів** — fragile: string path matching ламається при рефакторингу маршрутів. Відкинуто.
- **Кастомний атрибут `[RequireEmailConfirmed]`** — еквівалентний named policy, але менш стандартний; ASP.NET Core authorization policies — правильний механізм для іменованих правил авторизації. Відкинуто.

**Наслідки:**
- `JwtTokenService.GenerateAccessToken` додає claim `email_verified: "true"/"false"` (string, consistent with OIDC standard)
- `ICurrentUserService` розширюється: `bool IsEmailConfirmed`
- `CurrentUserService` зчитує claim `email_verified` з `ClaimsPrincipal`
- Нова named policy `EmailConfirmed` реєструється в `AddApiServices` (`Learnix.API`)
- 7 endpoints отримують `[Authorize(Policy = "EmailConfirmed")]` поверх існуючого `[Authorize]`
- Frontend: `isEmailConfirmed: boolean` додається до auth store; persistent banner відображається якщо `false`; при 403 від gated endpoint — modal "спочатку підтверди email" з кнопкою resend

---

## Шаблон для нових записів

```
## ADR-XXX: [Назва рішення]

**Рішення:** [Що саме вирішили]

**Чому:** [Обґрунтування]

**Альтернативи:** [Що розглядали і чому відкинули]

**Наслідки:** [Що це змінює в коді / архітектурі]
```
