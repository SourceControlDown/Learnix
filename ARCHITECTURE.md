# Learnix — Architecture Specification

> For architectural decision records and rationale, see [DECISIONS.md](./DECISIONS.md).

---

## Overview

**Pattern:** Clean Architecture + Light DDD + CQRS  
**API:** ASP.NET Core 8  
**Mediator:** MediatR  
**Validation:** FluentValidation (pipeline behavior, returns Result — see ADR-009)  
**Result pattern:** FluentResults (see ADR-002, ADR-010)  
**ORM:** Entity Framework Core (PostgreSQL)  
**NoSQL:** MongoDB.Driver  
**Cache:** Redis (StackExchange.Redis)  
**Message Broker:** MassTransit + Azure Service Bus  

---

## Layer Structure

```
Learnix.Domain           — Entities, Domain Events, Enums, Constants
Learnix.Application      — CQRS, Validators, Specifications, Interfaces, Integration Events
Learnix.Infrastructure   — EF Core, MongoDB, Redis, MassTransit, External Services
Learnix.API              — Controllers, Middleware, DI registration
```

### Dependency rule
```
API → Application → Domain        (allowed)
Infrastructure → Application      (allowed, implements interfaces)
Domain → nothing                  (no dependencies)
Application → Infrastructure      (FORBIDDEN — only via interfaces)
```

---

## Request Flow

```
HTTP Request
    ↓
Controller               (routes to MediatR, returns IActionResult)
    ↓
MediatR Pipeline
    ├── LoggingBehavior      (logs request/response, warns on slow requests)
    ├── ValidationBehavior   (FluentValidation — returns Result.Fail if invalid, see ADR-009)
    └── CachingBehavior      (for queries marked ICacheable)
    ↓
Command / Query Handler  (business logic)
    ↓
    ├── [Query]  → Repository.ListAsync(specification) → map to DTO → return Result<T>
    └── [Command]→ Repository.FirstOrDefaultAsync(specification)
                 → call entity method (entity raises Domain Event)
                 → UnitOfWork.SaveChangesAsync()
                     ↓ (after commit)
                 → Domain Event Handler (MediatR INotificationHandler)
                     ↓
                 → publish Integration Event via MassTransit
                     ↓
                 → Consumer (async: email, PDF, achievements, notifications)
```

---

## CQRS

All operations go through MediatR. No business logic in controllers.

### Command structure
Every feature folder under `Application/{Feature}/Commands/{Name}/` contains:
```
EnrollInCourseCommand.cs          — record with input data
EnrollInCourseCommandHandler.cs   — implements IRequestHandler<,>
EnrollInCourseValidator.cs        — AbstractValidator<EnrollInCourseCommand>
```

### Query structure
Every feature folder under `Application/{Feature}/Queries/{Name}/` contains:
```
GetCourseByIdQuery.cs             — record with input data, optionally implements ICacheable
GetCourseByIdQueryHandler.cs      — implements IRequestHandler<,>
GetCourseByIdResponse.cs          — DTO returned to controller
```

### Rules
- Commands return `Result` or `Result<T>` (FluentResults)
- Queries return `Result<TResponse>` (FluentResults)
- Expected errors → `Result.Fail(new NotFoundError(...))` — never throw for business errors (see ADR-010)
- Throw exceptions — only for unexpected infrastructure failures
- Never return domain entities from handlers — always map to DTOs (see ADR-012)

### Naming & file paths convention

| Element | Pattern | Example |
|---|---|---|
| Feature folder | `Application/{Feature}/` | `Application/Auth/` |
| Command folder | `Application/{Feature}/Commands/{Name}/` | `Application/Auth/Commands/Register/` |
| Query folder | `Application/{Feature}/Queries/{Name}/` | `Application/Courses/Queries/GetCourseById/` |
| Validator | `{Name}Validator.cs` (alongside command/query) | `RegisterValidator.cs` |
| Handler | `{Name}CommandHandler.cs` / `{Name}QueryHandler.cs` | `RegisterCommandHandler.cs` |
| Response/DTO | `{Name}Response.cs` (для queries) або в файлі команди (для commands) | `GetCourseByIdResponse.cs` |
| Specifications | `Application/{Feature}/Specifications/{Name}Specification.cs` | `Application/Courses/Specifications/PublishedCoursesSpecification.cs` |
| Abstractions (cross-cutting) | `Application/Common/Abstractions/{Category}/I{Name}.cs` | `Application/Common/Abstractions/Persistence/IUnitOfWork.cs` |
| Abstractions (feature) | `Application/{Feature}/Abstractions/I{Name}.cs` | `Application/Auth/Abstractions/ITokenService.cs` |
| Models (feature) | `Application/{Feature}/Models/{Name}.cs` | `Application/Auth/Models/UserAuthenticationInfo.cs` |

**Без проміжної папки `Features/`** — feature names напряму під `Application/`. Поряд з ними лежить `Application/Common/` (інфраструктура шару). Це канонічний .NET підхід (eShopOnWeb, Jason Taylor template).

**Правило для нових інтерфейсів:** "Цей інтерфейс має сенс поза однією фічею?" Так → `Common/Abstractions/{Category}/`. Ні → `{Feature}/Abstractions/`. Див. ADR-030.

---

## Result Pattern (FluentResults)

Application layer uses [FluentResults](https://github.com/altmann/FluentResults) library for Result pattern (see ADR-002). No custom Result<T> implementation.

### Typed errors (ADR-010)
```csharp
// Learnix.Application/Common/Errors/NotFoundError.cs
public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message) { }
}

// Learnix.Application/Common/Errors/ConflictError.cs
public class ConflictError : Error
{
    public ConflictError(string message) : base(message) { }
}

// Learnix.Application/Common/Errors/ForbiddenError.cs
public class ForbiddenError : Error
{
    public ForbiddenError(string message) : base(message) { }
}

// Learnix.Application/Common/Errors/ValidationError.cs
public sealed class ValidationError : Error
{
    public ValidationResult ValidationResult { get; }

    public ValidationError(ValidationResult validationResult)
        : base("One or more validation errors occurred.")
    {
        ValidationResult = validationResult;
    }

    public IReadOnlyDictionary<string, string[]> ToDictionary()
        => ValidationResult.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
}
```

### When to use
- `Result.Fail(new NotFoundError(...))` — expected domain errors
- `Result.Fail(new ConflictError(...))` — duplicates, already enrolled, etc.
- Throw exceptions — only for unexpected infrastructure failures (DB unavailable, etc.)

### Controller mapping
```csharp
var result = await _mediator.Send(command);

if (result.HasError<ValidationError>(out var validationErrors))
{
    var problem = new ValidationProblemDetails(validationErrors.First().ToDictionary());
    return BadRequest(problem);
}
if (result.HasError<NotFoundError>()) return NotFound();
if (result.HasError<ConflictError>()) return Conflict();
if (result.HasError<ForbiddenError>()) return Forbid();
if (result.IsFailed) return BadRequest(result.Errors);
return Ok(result.Value);
```

Error responses use ProblemDetails (RFC 7807) — see ADR-017.

### HTTP status code mapping (стандарт для всіх контролерів)

| FluentResults error type | HTTP status | Body |
|---|---|---|
| `ValidationError` | 400 Bad Request | `ValidationProblemDetails` з `errors` dictionary |
| `NotFoundError` | 404 Not Found | `ProblemDetails` |
| `ConflictError` | 409 Conflict | `ProblemDetails` |
| `ForbiddenError` | 403 Forbidden | `ProblemDetails` |
| Інша `Error` (без типу) | 400 Bad Request | `ProblemDetails` з агрегованими повідомленнями |
| `Result.IsSuccess` без значення | 204 No Content | empty |
| `Result<T>.IsSuccess` зі значенням | 200 OK / 201 Created | DTO |

Кожен новий контролер дотримується цієї таблиці. У майбутньому (B-16 polish pass) — винесемо в `result.ToActionResult()` extension method для DRY.

---

## Validation Pipeline

FluentValidation runs automatically for every Command and Query via `ValidationBehavior<TRequest, TResponse>`.
Returns `Result.Fail()` on validation errors — no exceptions thrown (see ADR-009).

```csharp
// Learnix.Application/Common/Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : ResultBase, new()
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var aggregated = new ValidationResult(failures);
        var response = new TResponse();
        response.Reasons.Add(new ValidationError(aggregated));
        return response;
    }
}
```

Validation errors are returned as `Result.Fail()` — consistent with ADR-002 and ADR-009.
`ExceptionHandlingMiddleware` handles only unexpected infrastructure failures.

---

## Logging Behavior

Logs every MediatR request with name, duration, and warns on slow requests (>3s).

```csharp
// Learnix.Application/Common/Behaviors/LoggingBehavior.cs
public class LoggingBehavior<TRequest, TResponse>
    (ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        logger.LogInformation("[START] {Request}", typeof(TRequest).Name);

        var timer = Stopwatch.StartNew();
        var response = await next(ct);
        timer.Stop();

        if (timer.Elapsed.TotalSeconds > 3)
        {
            logger.LogWarning(
                "[PERFORMANCE] The request {Request} took {TimeTaken} seconds.",
                typeof(TRequest).Name, timer.Elapsed.TotalSeconds);
        }

        logger.LogInformation(
            "[END] Handled {Request} with {Response}",
            typeof(TRequest).Name, typeof(TResponse).Name);

        return response;
    }
}
```

> Request payload свідомо НЕ логується — це запобігає випадковому зливу PII 
> (паролі в `RegisterCommand`, токени в `ResetPasswordCommand`). 
> Якщо handler потребує логування specific полів — робить це явно всередині.

---

## Domain Entities

### Domain primitives — інтерфейси

Domain layer розділяє три ортогональних concerns на окремі інтерфейси:

```csharp
// Learnix.Domain/Common/IAuditable.cs
public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
}
```

```csharp
// Learnix.Domain/Common/IHasDomainEvents.cs
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

```csharp
// Learnix.Domain/Common/ISoftDeletable.cs
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
}
```

Інтерфейси використовуються EF interceptors і `ApplicationDbContext` для уніфікованої обробки cross-cutting concerns без прив'язки до конкретного базового класу. Див. ADR-023.

### BaseEntity — sugar для більшості entities

```csharp
// Learnix.Domain/Common/BaseEntity.cs
public abstract class BaseEntity : IAuditable, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

Усі звичайні entities (Course, Section, Lesson, Enrollment, RefreshToken, ...) наслідують `BaseEntity` — отримують `Id`, audit fields і domain events механізм за замовчуванням.

### User — окремий випадок (наслідує IdentityUser)

`User` не може наслідувати `BaseEntity`, бо `IdentityUser<Guid>` уже надає `Id`. Тому `User` імплементує два інтерфейси вручну:

```csharp
// Learnix.Domain/Entities/User.cs
public class User : IdentityUser<Guid>, IAuditable, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public string? GoogleId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    // Business methods (UpdateProfile, SetAvatar, ...) and event triggers
    // ...
}
```

Цей дубль `IHasDomainEvents`/`IAuditable` коду в `User` — свідома ціна за можливість мати один клас на User замість двох сутностей (Domain User + Identity User з синхронізацією). Див. ADR-018, ADR-023.

### Soft delete

Entities що підтримують soft delete (User, Course) додатково імплементують `ISoftDeletable`. Інтерсептор + global query filter автоматично:
- При `Remove()` — встановлює `IsDeleted = true`, `DeletedAt = UtcNow` замість фізичного видалення (`SoftDeleteInterceptor`)
- При читанні — глобально виключає soft-deleted записи (query filter в `OnModelCreating`)

Для адмін-панелі чи службових сценаріїв — `.IgnoreQueryFilters()` на конкретному запиті. Див. ADR-016.

### Конвенції дизайну entities (ADR-015)

- Properties з `private set` — без публічних setter'ів
- Зміна стану — через методи що відображають бізнес-операції
- Один метод = одна бізнес-дія

```csharp
// Learnix.Domain/Entities/Course.cs
public class Course : BaseEntity, ISoftDeletable
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Good: one method per business operation
    public void UpdateDetails(string title, string description, decimal price, Guid categoryId) { ... }
    public void Publish() { ... }
    public void Archive() { ... }

    // Bad: setter per property — don't do this
    // public void SetTitle(string title) { ... }
}
```

### Domain event приклад

```csharp
// Learnix.Domain/Entities/Enrollment.cs
public class Enrollment : BaseEntity
{
    public void Complete()
    {
        Status = EnrollmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        RaiseDomainEvent(new EnrollmentCompletedDomainEvent(UserId, CourseId));
    }
}
```

Diff'и в Identity flow (де `UserManager.CreateAsync` персистить юзера до того як handler може повернути control) — допускається публічний `RaiseXxx` метод на entity, що викликається з Application layer **після** успішного створення в Identity. Це локальне відхилення, документоване в коментарях `User`. Див. також B-34.6 у TODO (плановий рефакторинг разом з міграцією на MassTransit).

### Конвенція констант

Обмеження entities (max length полів, тощо) розділені за рівнем:

- **Domain** (`Learnix.Domain/Constants/{Entity}Constants.cs`) — інваріанти сутності. Споживаються EF configurations та Application validators. Single source of truth для "що entity вважає валідним станом".
- **Application** (`Learnix.Application/{Feature}/Constants/{Feature}ValidationConstants.cs`) — feature-специфічні валідаційні політики (password complexity, RFC stadards). Не стосуються інваріантів сутності.

Приклад: `UserConstants.FirstNameMaxLength = 100` живе в Domain (інваріант User), `AuthValidationConstants.PasswordMinLength = 8` — в Application (політика реєстрації, не інваріант User бо User зберігає лише hash).

Що **НЕ** виноситься в константи: одноразові regex'и, повідомлення помилок (до появи локалізації). Див. ADR-027.

---

## Domain Event → MediatR Adapter

`IDomainEvent` в Domain layer — marker interface без залежності від MediatR (див. ADR-019). 
Для публікації через MediatR використовується адаптер:

```csharp
// Learnix.Application/Common/Events/IDomainEventNotification.cs
public interface IDomainEventNotification<out TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    TDomainEvent DomainEvent { get; }
}

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent)
    : IDomainEventNotification<TDomainEvent>
    where TDomainEvent : IDomainEvent;
```

Handlers підписуються на обгортку, не на голий event:

```csharp
public class SendCertificateHandler 
    : INotificationHandler<DomainEventNotification<EnrollmentCompletedDomainEvent>>
{
    public Task Handle(DomainEventNotification<EnrollmentCompletedDomainEvent> n, CancellationToken ct)
    {
        var domainEvent = n.DomainEvent;
        // ...
    }
}
```

---

## EF Core Interceptors

### AuditableInterceptor (ADR-014)
Sets `CreatedAt` / `UpdatedAt` automatically for all entities inheriting `IAuditable`.

Інтерсептор реагує на IAuditable, не на BaseEntity, тому покриває і User (який наслідує IdentityUser<Guid>, не BaseEntity), і всіх нащадків BaseEntity. Див. ADR-023.

```csharp
// Learnix.Infrastructure/Persistence/Interceptors/AuditableInterceptor.cs
public class AuditableInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        
        if (context is null) 
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
                entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

### SoftDeleteInterceptor (ADR-016)
Intercepts `Delete()` calls on `ISoftDeletable` entities — sets `IsDeleted` + `DeletedAt` instead of removing.

```csharp
// Learnix.Infrastructure/Persistence/Interceptors/SoftDeleteInterceptor.cs
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, ct);

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted) continue;

            entry.State = EntityState.Modified;
            entry.Property(nameof(ISoftDeletable.IsDeleted)).CurrentValue = true;
            entry.Property(nameof(ISoftDeletable.DeletedAt)).CurrentValue = DateTime.UtcNow;
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

### Global query filter for soft delete
Applied in `ApplicationDbContext` to automatically exclude soft-deleted entities from all queries.

```csharp
// Learnix.Infrastructure/Persistence/ApplicationDbContext.cs (in OnModelCreating)
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
    {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var filter = Expression.Lambda(Expression.Not(property), parameter);
        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
    }
}
```

To query soft-deleted entities (e.g. admin panel): use `.IgnoreQueryFilters()`.

---

## Domain Event Dispatching

Events публікуються **після** успішного `SaveChangesAsync` безпосередньо в `ApplicationDbContext` (який реалізує `IUnitOfWork` — див. ADR-021). Кожен `IDomainEvent` обгортається в `DomainEventNotification<T>` через reflection, щоб MediatR міг знайти відповідні handlers.
Публікація domain events працює через IHasDomainEvents, не BaseEntity — те ж обґрунтування що й для AuditableInterceptor.

```csharp
// Learnix.Infrastructure/Persistence/ApplicationDbContext.cs
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = Activator.CreateInstance(notificationType, domainEvent)!;
            await publisher.Publish(notification, cancellationToken);
        }

        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();

        return result;
    }
```

> **Відомий ризик:** якщо процес впаде між `SaveChangesAsync` і `Publish`, event втратиться. Свідомо прийнятний на поточному етапі (див. ADR-022). Заміняється Outbox pattern перед Phase 6 (TODO: B-34.5).

---

## Pagination (ADR-013)

Shared classes in `Application/Common/Pagination/`.

```csharp
// Learnix.Application/Common/Pagination/PaginationRequest.cs
public record PaginationRequest
{
    public const int MaxPageSize = 100;
    public const int MinPageSize = 1;
    public const int DefaultPageSize = 20;

    public int PageIndex { get; init; }
    public int PageSize { get; init; }

    public PaginationRequest(int pageIndex = 0, int pageSize = DefaultPageSize)
    {
        PageIndex = Math.Max(0, pageIndex);
        PageSize = Math.Clamp(pageSize, MinPageSize, MaxPageSize);
    }

    public int Skip => PageIndex * PageSize;
    public int Take => PageSize;
}
```

```csharp
// Learnix.Application/Common/Pagination/PaginatedResult.cs
public record PaginatedResult<TEntity>(
    int PageIndex,
    int PageSize,
    long TotalCount,
    IReadOnlyList<TEntity> Data
) where TEntity : class
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageIndex < TotalPages - 1;
    public bool HasPreviousPage => PageIndex > 0;

    public static PaginatedResult<TEntity> Create(
        IEnumerable<TEntity> items, int pageIndex, int pageSize, long totalCount)
        => new(pageIndex, pageSize, totalCount, items.ToList().AsReadOnly());

    public static PaginatedResult<TEntity> Empty(int pageIndex, int pageSize)
        => new(pageIndex, pageSize, 0, Array.Empty<TEntity>());
}
```

---

## Specification Pattern

Used by all repositories to decouple query logic from infrastructure.

### Base class
```csharp
// Learnix.Application/Common/Specifications/Specification.cs
public abstract class Specification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; protected set; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; protected set; }
    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }
    public int Take { get; protected set; }
    public int Skip { get; protected set; }
    public bool IsPagingEnabled { get; protected set; }
    public bool AsNoTracking { get; protected set; } = true;

    protected void AddInclude(Expression<Func<T, object>> include) => Includes.Add(include);
    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy) => OrderBy = orderBy;
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderBy) => OrderByDescending = orderBy;
    protected void ApplyPaging(int skip, int take) { Skip = skip; Take = take; IsPagingEnabled = true; }
}
```

### SpecificationEvaluator
Lives in `Infrastructure/Persistence/`. Applies specification to IQueryable.

```csharp
// Learnix.Infrastructure/Persistence/SpecificationEvaluator.cs
public static class SpecificationEvaluator<T> where T : class
{
    public static IQueryable<T> GetQuery(IQueryable<T> query, Specification<T> spec)
    {
        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        query = spec.Includes.Aggregate(query, (q, i) => q.Include(i));
        query = spec.IncludeStrings.Aggregate(query, (q, i) => q.Include(i));

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.IsPagingEnabled)
            query = query.Skip(spec.Skip).Take(spec.Take);

        if (spec.AsNoTracking)
            query = query.AsNoTracking();

        return query;
    }
}
```

### Specification location
```
Application/{Feature}/Specifications/{Name}Specification.cs
```

### AsNoTracking convention
- All specifications default to `AsNoTracking = true`
- Set `AsNoTracking = false` explicitly only in specifications used by Commands that update entities

---

## Repository Pattern

Specific repository per aggregate root. No generic repository.

### Auth-related repositories

`IRefreshTokenRepository` — тонкий repository поверх `RefreshToken` entity. Інтерфейс живе в `Auth/Abstractions/` (feature-scoped, ADR-030), реалізація в `Infrastructure/Persistence/Repositories/`. Використовується тільки з Auth handlers.

### Interface (Application layer)
```csharp
// Learnix.Application/Common/Interfaces/ICourseRepository.cs
public interface ICourseRepository
{
    Task<Course?> FirstOrDefaultAsync(Specification<Course> spec, CancellationToken ct = default);
    Task<List<Course>> ListAsync(Specification<Course> spec, CancellationToken ct = default);
    Task<int> CountAsync(Specification<Course> spec, CancellationToken ct = default);
    Task AddAsync(Course course, CancellationToken ct = default);
    void Update(Course course);
    void Delete(Course course);
}
```

### Implementation (Infrastructure layer)
```csharp
// Learnix.Infrastructure/Persistence/Repositories/CourseRepository.cs
public class CourseRepository(ApplicationDbContext context) : ICourseRepository
{
    public async Task<Course?> FirstOrDefaultAsync(Specification<Course> spec, CancellationToken ct)
        => await SpecificationEvaluator<Course>.GetQuery(context.Courses, spec).FirstOrDefaultAsync(ct);

    public async Task<List<Course>> ListAsync(Specification<Course> spec, CancellationToken ct)
        => await SpecificationEvaluator<Course>.GetQuery(context.Courses, spec).ToListAsync(ct);

    public async Task<int> CountAsync(Specification<Course> spec, CancellationToken ct)
        => await SpecificationEvaluator<Course>.GetQuery(context.Courses, spec).CountAsync(ct);

    public async Task AddAsync(Course course, CancellationToken ct)
        => await context.Courses.AddAsync(course, ct);

    public void Update(Course course) => context.Courses.Update(course);
    public void Delete(Course course) => context.Courses.Remove(course);
}
```

### Unit of Work
SaveChanges is called only in handlers via `IUnitOfWork`, not inside repositories.

```csharp
// Learnix.Application/Common/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

**Реалізація:** `ApplicationDbContext` сам імплементує `IUnitOfWork` (ADR-021). Окремого класу `UnitOfWork` немає. DI резолвить обидва інтерфейси в один scope instance:

```csharp
// Learnix.Infrastructure/DependencyInjection.cs
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
```

---

## Integration Events & MassTransit

Domain Event Handlers publish Integration Events to Azure Service Bus via MassTransit.

### Flow
```
DomainEventHandler (Application, in-process)
    → publishes IntegrationEvent via IBus
        → MassTransit → Azure Service Bus
            → Consumer (Infrastructure, async)
```

### What goes through the bus
| Integration Event | Consumer | Action |
|---|---|---|
| `UserRegisteredIntegrationEvent` | `SendVerificationEmailConsumer` | Send verification email |
| `CourseEnrolledIntegrationEvent` | `SendEnrollmentEmailConsumer` | Send welcome email |
| `LessonCompletedIntegrationEvent` | `CheckAchievementsConsumer` | Award achievements |
| `CourseCompletedIntegrationEvent` | `GenerateCertificateConsumer` | Generate PDF + notify |
| `PaymentCompletedIntegrationEvent` | `ConfirmEnrollmentConsumer` | Activate enrollment |
| `InstructorApprovedIntegrationEvent` | `SendApprovalEmailConsumer` | Notify instructor |

### What stays in-process (MediatR only)
- Progress updates
- Permission checks
- Cache invalidation

---

## Caching Strategy (Redis)

### ICacheable interface
```csharp
// Learnix.Application/Common/Interfaces/ICacheable.cs
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan Expiry { get; }
}
```

### Queries that implement ICacheable
```csharp
public record GetPopularCoursesQuery : IRequest<Result<List<CourseDto>>>, ICacheable
{
    public string CacheKey => CacheKeys.PopularCourses;
    public TimeSpan Expiry => TimeSpan.FromMinutes(10);
}
```

### CachingBehavior in MediatR pipeline
Checks Redis before executing handler. Stores result after execution.

### Cache keys (Domain/Constants/CacheKeys.cs)
```csharp
// Learnix.Application/Common/Constants/CacheKeys.cs
public static class CacheKeys
{
    public static string PopularCourses => "popular-courses";
    public static string Course(Guid id) => $"course:{id}";
    public static string UserAchievements(Guid userId) => $"user-achievements:{userId}";
}
```

### Invalidation
Commands that modify data call `ICacheService.RemoveAsync(key)` after SaveChanges.

---

## MongoDB Usage

MongoDB is used for data with flexible schema that requires no joins (see ADR-004).

| Collection | Entity | Reason |
|---|---|---|
| `chat_sessions` | `ChatSession` | Variable message array, no relational structure needed |
| `course_reviews` | `CourseReview` | Flexible metadata, no joins required |

Accessed via dedicated repository interfaces (`IChatSessionRepository`, `ICourseReviewRepository`).  
No EF Core. Uses `MongoDB.Driver` directly in Infrastructure.

---

## Security

- Auth-flow повністю описаний у секції "Authentication" вище. Стисло: JWT 15 min + refresh 7d з rotation + replay protection + HttpOnly cookie (ADR-003, ADR-033, ADR-034).
- Refresh tokens — SHA-256 hash в PostgreSQL. Plain token живе тільки у клієнтській cookie. Витік БД не компрометує сесії.
- Background cleanup: `RefreshTokenCleanupHostedService` видаляє revoked/expired токени старші `ExpiresAt + 7d` раз на добу (B-11.5).
- Google OAuth via ASP.NET Core Identity external login (see ADR-018)
- Rate limiting on: `/auth/login`, `/auth/register`, `/auth/forgot-password`, `/ai/chat`
- Security headers applied via `SecurityHeadersMiddleware`: CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy
- File upload validation: allowed MIME types whitelist + max size enforced in middleware
- Input validation: FluentValidation (backend) + Zod (frontend)
- Error responses: ProblemDetails (RFC 7807) — see ADR-017

---

## Logging

Serilog with structured logging throughout.

```csharp
Log.Information("User {UserId} enrolled in course {CourseId}", userId, courseId);
```

Sinks:
- Console (development)
- Azure Application Insights (production)

`LoggingBehavior` in MediatR pipeline logs every request name and duration automatically.
Requests taking >3 seconds are logged as warnings.

---

## Authentication

### Token strategy

| Token | Lifetime | Storage | Transport |
|---|---|---|---|
| Access (JWT) | 15 min | Client memory | `Authorization: Bearer` header |
| Refresh | 7 days | HttpOnly cookie + PostgreSQL (SHA-256 hash) | Cookie auto-sent on `/api/auth/*` |

### Flow

```
Login                           Refresh                         Logout
─────                           ───────                         ──────
POST /auth/login                POST /auth/refresh              POST /auth/logout
  ↓                               ↓ (cookie auto-attached)        ↓ (cookie auto-attached)
ValidateCredentials             Find token by SHA-256 hash      Find token by SHA-256 hash
  ↓                               ↓                               ↓
Generate JWT + refresh          IsRevoked → REPLAY              Revoke if active
  ↓                               → revoke ALL active            ↓
Store refresh hash in DB        → 401 + clear cookie            Clear cookie
  ↓                               ↓                               ↓
Set HttpOnly cookie             Else: revoke old, issue new    204
  ↓                             pair, store, set cookie         
Return access in body           Return access in body
```

### Replay attack protection

Якщо клієнт надсилає refresh token з полем `IsRevoked = true` — це сигнал що токен було перехоплено і вже використано легітимною сесією (або навпаки). Реакція: revoke **усіх** активних refresh tokens для цього юзера, інцидент логується як warning з `UserId`. Юзер вилогінюється з усіх пристроїв і має заново ввести креденшели. Див. ADR-033.

### Cookie configuration

```csharp
new CookieOptions
{
    HttpOnly = true,                   // not readable from JS — XSS-resistant
    Secure = Request.IsHttps,          // HTTPS-only in production
    SameSite = SameSiteMode.Strict,    // not sent on cross-site requests — CSRF-resistant
    Path = "/api/auth",                // not sent with non-auth requests — least exposure
    Expires = refreshTokenExpiresAt
}
```

### JWT configuration

`AddJwtBearer` validates: issuer, audience, lifetime, signing key. `ClockSkew = 30s` — толерантність до розсинхронізації годинників. `MapInboundClaims = false` — claims у коді мають ті ж імена що й у JWT. Див. ADR-034 для повного списку claims.

### Controller responsibility

Контролер відповідає за HTTP-специфіку (читання cookie → передача рядка в команду; запис cookie з результату). Application handlers оперують голими рядками — нічого не знають про HTTP.

---

## Application Settings

Конфігураційні секції з `appsettings.json` мапляться на типізовані POCO в `Application/Common/Settings/{Name}Settings.cs`. Реєструються через `services.Configure<T>(configuration.GetSection("..."))` в `Infrastructure/DependencyInjection.cs`. Споживаються в handlers / services через `IOptions<T>`.

### Приклад

```csharp
// Learnix.Application/Common/Settings/AppSettings.cs
public class AppSettings
{
    public string ClientBaseUrl { get; init; } = null!;
}
```

```json
// appsettings.json
{
  "App": {
    "ClientBaseUrl": "http://localhost:5173"
  }
}
```

```csharp
// Реєстрація
services.Configure<AppSettings>(configuration.GetSection("App"));

// Споживання
public class SomeHandler(IOptions<AppSettings> appSettings)
{
    private readonly AppSettings _settings = appSettings.Value;
    // ...
}
```

### Конвенції

- POCO settings лежать у `Application/Common/Settings/` — Application шар знає про конфігурацію типізовано, не знає про `IConfiguration` напряму
- Реєстрація через `Configure<T>` — тільки в `Infrastructure/DependencyInjection.cs`
- Connection strings — окремо у `ConnectionStrings:Postgres`, `ConnectionStrings:Mongo`, `ConnectionStrings:Redis` (стандарт ASP.NET)
- Імена секцій конфігурації документуються у `.env.example` і README, **не дублюються в архітектурному документі**
- Кожна нова інтеграція (Stripe, Anthropic, Azure Blob, ...) → новий `{Service}Settings` POCO + секція в `appsettings.json`

### Приклад: JWT секрет (ADR-031)

```csharp
// Learnix.Application/Common/Settings/JwtSettings.cs
public sealed class JwtSettings
{
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public string Secret { get; init; } = null!;
    public int AccessTokenExpiryMinutes { get; init; }
    public int RefreshTokenExpiryDays { get; init; }
}
```

Стратегія значень:
- `appsettings.json` — `Secret = ""` (placeholder, fail-fast валідація на старті)
- `appsettings.Development.json` — статичний dev-секрет (>32 байт), безпечний бо ніколи не йде в production-білд
- Production — `JWT__Secret` env var (double underscore = nested key в .NET configuration)

### Чому через IOptions, а не статичні класи / `IConfiguration` напряму

- Тестабельність: `Options.Create(new AppSettings { ... })` для unit-тестів handler'ів
- Перевірка при старті: `services.AddOptions<AppSettings>().ValidateDataAnnotations().ValidateOnStart()` (поки не використовуємо, але двері відкриті)
- Уникаємо знання Application шару про конкретну реалізацію конфігурації

---

## Database Migrations

EF Core Code-First. Міграції живуть в `Learnix.Infrastructure/Persistence/Migrations/`.

### Створення нової міграції

```bash
dotnet ef migrations add {Name} \
    --project Learnix.Infrastructure \
    --startup-project Learnix.API \
    --output-dir Persistence/Migrations
```

Назва — PascalCase, описує **що** змінилось (`AddCoursesAndSections`, `AddInstructorApplications`). Не `Update1`, `Update2`.

### Застосування

- **Development:** автоматично при старті API (`app.ApplyMigrationsAsync()` під `if IsDevelopment()`). Розробник підняв `docker compose up -d` і `dotnet run` — БД готова. Див. ADR-029.
- **Staging / Production:** окремий контрольований крок CI/CD або ручний `dotnet ef database update` з міграційного хоста. Авто-міграції в prod заборонені (race condition при scale-out, ризик руйнівних змін без review).

### Rollback

```bash
# Відкат до конкретної міграції
dotnet ef database update {PreviousMigrationName} --project Learnix.Infrastructure --startup-project Learnix.API

# Видалення останньої незастосованої міграції
dotnet ef migrations remove --project Learnix.Infrastructure --startup-project Learnix.API
```

Видалена міграція що вже застосована до БД — спершу `database update {Previous}`, потім `migrations remove`.

### Що НЕ робити

- Не редагувати застосовані міграції вручну — створюй нову коригувальну міграцію
- Не комітити міграції з `dotnet ef migrations add Test` без переглянутого `Up()` / `Down()`
- Не міксувати схему від `EnsureCreated()` з міграціями — обирай одне (у Learnix — тільки міграції)

---

## Project Structure Reference

```
Learnix.Domain/
├── Common/             ← BaseEntity, IAuditable, IHasDomainEvents, ISoftDeletable, IDomainEvent
├── Constants/          ← Roles, UserConstants, etc.
├── Entities/
├── Documents/          ← MongoDB models
├── Events/             ← IDomainEvent implementations
└── Enums/

Learnix.Application/
├── Common/
│   ├── Abstractions/
│   │   ├── Persistence/    ← IUnitOfWork
│   │   ├── Caching/        ← ICacheable, ICacheService
│   │   ├── Messaging/      ← IEmailSender
│   │   └── Time/           ← (later: IDateTimeProvider)
│   ├── Behaviors/      ← ValidationBehavior, LoggingBehavior, CachingBehavior (later)
│   ├── Constants/      ← CacheKeys
│   ├── Errors/         ← NotFoundError, ConflictError, ForbiddenError, ValidationError
│   ├── Events/         ← IDomainEventNotification<T>, DomainEventNotification<T>
│   ├── Pagination/     ← PaginatedResult<T>, PaginationRequest
│   ├── Settings/       ← AppSettings, JwtSettings
│   └── Specifications/ ← Specification<T> base class
├── Auth/
│   ├── Abstractions/   ← IUserRegistrationService, IUserAuthenticationService, ITokenService, IRefreshTokenRepository
│   ├── Commands/{Name}/
│   ├── Constants/      ← AuthValidationConstants
│   ├── Models/         ← UserAuthenticationInfo, AccessTokenResult, RefreshTokenResult
│   └── Specifications/ ← RefreshTokenByHashSpecification, ActiveRefreshTokensByUserSpecification
└── {Feature}/          ← same structure (Abstractions/Commands/Queries/Models/Specifications)

Learnix.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/
│   ├── Interceptors/   ← AuditableInterceptor, SoftDeleteInterceptor
│   ├── Repositories/
│   ├── SpecificationEvaluator.cs
│   └── Migrations/
├── MongoDB/
│   ├── MongoDbContext.cs
│   └── Repositories/
├── Identity/           ← UserRegistrationService, UserAuthenticationService, JwtTokenService
├── Consumers/          ← MassTransit consumers (later)
├── Services/           ← Cache, Blob, Email, AI, Payment + RefreshTokenCleanupHostedService, RoleSeederHostedService
└── Extensions/

Learnix.API/
├── Controllers/
├── Middleware/         ← ExceptionHandlingMiddleware, SecurityHeadersMiddleware
└── Extensions/
```

---

## Testing Strategy

Тести у v1 свідомо відкладені до завершення Phase 5 (Payments) — пріоритет на feature delivery. Архітектурне місце зарезервоване:

```
Learnix.Backend/
├── Learnix.Domain/
├── Learnix.Application/
├── Learnix.Infrastructure/
├── Learnix.API/
└── tests/
├── Learnix.Domain.UnitTests/        — entity behavior, domain methods
├── Learnix.Application.UnitTests/   — handlers (mock IRepository, IIdentityService, ...)
└── Learnix.Integration.Tests/       — full HTTP flow з testcontainers (Postgres, Mongo, Redis)
```

Stack: xUnit + FluentAssertions + NSubstitute (mocks) + Testcontainers (integration).

Unit-тести для handlers — мокати інтерфейси з Application/Common/Interfaces. Integration — піднімати реальний Postgres/Mongo через Testcontainers, ходити через `WebApplicationFactory<Program>`.
