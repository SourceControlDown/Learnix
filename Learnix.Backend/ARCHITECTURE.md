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
```

### When to use
- `Result.Fail(new NotFoundError(...))` — expected domain errors
- `Result.Fail(new ConflictError(...))` — duplicates, already enrolled, etc.
- Throw exceptions — only for unexpected infrastructure failures (DB unavailable, etc.)

### Controller mapping
```csharp
var result = await _mediator.Send(command);

if (result.HasError<NotFoundError>()) return NotFound();
if (result.HasError<ConflictError>()) return Conflict();
if (result.HasError<ForbiddenError>()) return Forbid();
if (result.IsFailed) return BadRequest(result.Errors);
return Ok(result.Value);
```

Error responses use ProblemDetails (RFC 7807) — see ADR-017.

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

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var failures = _validators
            .Select(v => v.Validate(request))
            .SelectMany(r => r.Errors)
            .Where(e => e != null)
            .ToList();

        if (failures.Any())
        {
            var result = new TResponse();
            foreach (var failure in failures)
                result.Reasons.Add(new Error(failure.ErrorMessage));
            return result;
        }

        return await next();
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
        logger.LogInformation(
            "[START] Handle request={Request} - Response={Response} - RequestData={@RequestData}",
            typeof(TRequest).Name, typeof(TResponse).Name, request);

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

---

## Domain Entities

### BaseEntity
All PostgreSQL entities inherit from `BaseEntity`. Provides identity, audit fields, and domain events.
Audit fields are set automatically via `AuditableInterceptor` (see ADR-014).

```csharp
// Learnix.Domain/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public interface IDomainEvent : INotification { }
```

### ISoftDeletable
Entities that support soft delete implement this interface (see ADR-016).

```csharp
// Learnix.Domain/Common/ISoftDeletable.cs
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
}
```

Soft-deletable entities (User, Course) implement `ISoftDeletable` with `private set`.
`SoftDeleteInterceptor` sets `IsDeleted` / `DeletedAt` automatically on `Delete()`.

### Entity design conventions (ADR-015)
- Properties have `private set` — no public setters
- State changes through domain methods that represent business operations
- One method = one business action (not one method per property)

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

### Domain event example
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

---

## EF Core Interceptors

### AuditableInterceptor (ADR-014)
Sets `CreatedAt` / `UpdatedAt` automatically for all entities inheriting `BaseEntity`.

```csharp
// Learnix.Infrastructure/Persistence/Interceptors/AuditableInterceptor.cs
public class AuditableInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, ct);

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Property(nameof(BaseEntity.CreatedAt)).CurrentValue = DateTime.UtcNow;

            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
        }

        return base.SavingChangesAsync(eventData, result, ct);
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

Events are dispatched after SaveChanges in UnitOfWork.

```csharp
// Learnix.Infrastructure/Persistence/UnitOfWork.cs
public async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var result = await _context.SaveChangesAsync(ct);

    var events = _context.ChangeTracker
        .Entries<BaseEntity>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();

    foreach (var domainEvent in events)
        await _publisher.Publish(domainEvent, ct);

    _context.ChangeTracker
        .Entries<BaseEntity>()
        .ToList()
        .ForEach(e => e.Entity.ClearDomainEvents());

    return result;
}
```

---

## Pagination (ADR-013)

Shared classes in `Application/Common/Pagination/`.

```csharp
// Learnix.Application/Common/Pagination/PaginationRequest.cs
public record PaginationRequest(int PageIndex = 0, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public const int MinPageSize = 1;
    public const int DefaultPageSize = 20;

    public int PageIndex { get; init; } = Math.Max(0, PageIndex);
    public int PageSize { get; init; } = Math.Clamp(PageSize, MinPageSize, MaxPageSize);

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
        if (spec.Criteria != null)         query = query.Where(spec.Criteria);
        if (spec.OrderBy != null)          query = query.OrderBy(spec.OrderBy);
        if (spec.OrderByDescending != null) query = query.OrderByDescending(spec.OrderByDescending);
        if (spec.IsPagingEnabled)          query = query.Skip(spec.Skip).Take(spec.Take);

        query = spec.Includes.Aggregate(query, (q, i) => q.Include(i));
        query = spec.IncludeStrings.Aggregate(query, (q, i) => q.Include(i));

        if (spec.AsNoTracking) query = query.AsNoTracking();

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
// Learnix.Domain/Constants/CacheKeys.cs
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

- JWT access token: 15 min expiry (see ADR-003)
- Refresh token: HttpOnly cookie, 7 day expiry, rotation on each use
- Refresh token stored in PostgreSQL (hashed), invalidated on use
- Replay attack protection: reuse of revoked token → revoke ALL user tokens
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

## Project Structure Reference

```
Learnix.Domain/
├── Common/             ← BaseEntity, ISoftDeletable, IDomainEvent
├── Entities/
├── Documents/          ← MongoDB models
├── Events/             ← IDomainEvent implementations
├── Enums/
└── Constants/

Learnix.Application/
├── Common/
│   ├── Behaviors/      ← ValidationBehavior, LoggingBehavior, CachingBehavior
│   ├── Errors/         ← NotFoundError, ConflictError, ForbiddenError
│   ├── Interfaces/     ← IUnitOfWork, ICacheService, IBlobService, IEmailService...
│   ├── Pagination/     ← PaginatedResult<T>, PaginationRequest
│   └── Specifications/ ← Specification<T> base class
├── {Feature}/
│   ├── Commands/{Name}/
│   ├── Queries/{Name}/
│   └── Specifications/

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
├── Consumers/          ← MassTransit consumers
├── Services/           ← Cache, Blob, Email, AI, Payment
└── Identity/

Learnix.API/
├── Controllers/
├── Middleware/          ← ExceptionHandlingMiddleware, SecurityHeadersMiddleware
└── Extensions/
```
