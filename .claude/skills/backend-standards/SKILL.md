---
name: backend-standards
description: Backend coding standards, architecture rules, CQRS patterns, Result<T> usage, Specification pattern, domain entity conventions, and pre/post task checklists for the Learnix .NET 8 backend. Use when implementing backend features, writing handlers, adding commands/queries, modifying domain entities, or reviewing backend architecture.
when_to_use: backend, C#, .NET, handler, command, query, controller, entity, domain, specification, repository, validator, MediatR, FluentResults, FluentValidation, EF Core, migration
---

# Learnix Backend Standards

## Pre-Task Checklist

Before implementing **any** backend feature, complete these steps in order:

1. **Read `docs/TODO.md`** — identify the exact task(s), their phase, and current status.
2. **Read `docs/FEATURES.md`** — understand the functional spec for the feature being built.
3. **Read `docs/backend/ARCHITECTURE.md`** — review layer rules, request flow, and relevant patterns.
4. **Check relevant ADR files in `docs/backend/decisions/`** — read only those that apply to the current task scope (Auth → `AUTH.md`, Domain → `DOMAIN.md`, Infra → `INFRA.md`, Blob → `BLOB.md`, etc.). The index is `docs/backend/decisions/README.md`.
5. **Check the relevant `Controllers/*.cs`** — verify route prefix, HTTP method, request/response shape, and auth requirements before writing any handler or DTO.
6. **Look at an existing similar handler** — find a handler in the same feature folder or a nearby one that does something similar. Match its style exactly.

---

## Layer Rules (never break these)

```
Learnix.Domain        → no external dependencies
Learnix.Application   → depends only on Domain + interfaces
Learnix.Infrastructure→ implements Application interfaces; depends on Application
Learnix.API           → depends on Application; thin, no business logic
```

- **Domain** has zero NuGet dependencies. No MediatR, no EF, no FluentValidation.
- **Application** never references `DbContext`, `EF Core`, or any Infrastructure type directly — only via interfaces.
- **Controllers** never call repositories, never call domain methods, never contain `if` based on domain state. They delegate to `ISender` and call `result.ToActionResult()`.
- **Infrastructure** never makes business decisions. It only performs technical side-effects (write to DB, call Azure, write Outbox). If Infrastructure code contains an `if` based on a domain entity's state — it's a bug (see ADR-BACK-ARCH-010).

---

## CQRS — File Structure

Every operation lives in its own folder. No exceptions.

### Command
```
Application/{Feature}/Commands/{Name}/
    {Name}Command.cs              — record : IRequest<Result> or IRequest<Result<T>>
    {Name}CommandHandler.cs       — sealed class : IRequestHandler<,>
    {Name}Validator.cs            — sealed class : AbstractValidator<{Name}Command>
```

### Query
```
Application/{Feature}/Queries/{Name}/
    {Name}Query.cs                — record : IRequest<Result<TResponse>>
    {Name}QueryHandler.cs         — sealed class : IRequestHandler<,>
    {Name}Response.cs             — DTO co-located here, NOT in a separate DTOs folder
```

### Specifications
```
Application/{Feature}/Specifications/{Name}Specification.cs
```

### Naming table

| Element | Pattern | Example |
|---|---|---|
| Feature folder | `Application/{Feature}/` | `Application/Courses/` |
| Command folder | `Application/{Feature}/Commands/{Name}/` | `Application/Auth/Commands/Register/` |
| Query folder | `Application/{Feature}/Queries/{Name}/` | `Application/Courses/Queries/GetCourseById/` |
| Handler | `{Name}CommandHandler.cs` / `{Name}QueryHandler.cs` | `RegisterCommandHandler.cs` |
| Validator | `{Name}Validator.cs` | `RegisterValidator.cs` |
| Response/DTO | `{Name}Response.cs` co-located with query | `CourseDetailDto.cs` |
| Specification | `{Name}Specification.cs` | `CourseByIdSpecification.cs` |
| Feature abstractions | `Application/{Feature}/Abstractions/I{Name}.cs` | `Auth/Abstractions/ITokenService.cs` |
| Cross-cutting abstractions | `Application/Common/Abstractions/{Category}/I{Name}.cs` | `Common/Abstractions/Persistence/IUnitOfWork.cs` |

---

## Result Pattern

All handlers return `Result` or `Result<T>` from **FluentResults**. Never use exceptions for business errors.

### Typed errors → HTTP status mapping

| Error type | HTTP | When to use |
|---|---|---|
| `NotFoundError` | 404 | Entity not found |
| `ConflictError` | 409 | Duplicate, invariant violation, already enrolled |
| `ForbiddenError` | 403 | Not owner / wrong role |
| `AuthenticationError` | 401 | Not authenticated |
| `ValidationError` | 400 | Validation (usually from `ValidationBehavior`, rarely manual) |

All error types live in `Application/Common/Errors/`.

### Handler auth check pattern
```csharp
// Always check auth first in command handlers
if (currentUser.UserId is null)
    return Result.Fail(new AuthenticationError("User is not authenticated."));

// Then ownership / role
if (course.InstructorId != currentUser.UserId && !currentUser.IsInRole(Roles.Admin))
    return Result.Fail(new ForbiddenError("You are not the owner of this course."));
```

Resource-based authorization (owner checks) belongs **in handlers**, not in controllers (see ADR-BACK-AUTH-013). Static rules — role membership, confirmed email — stay as `[Authorize]` / `[Authorize(Policy = "EmailConfirmed")]` attributes on the controller.

### Typical command handler shape
```csharp
public sealed class CreateCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCourseCommand, Result<CreateCourseResponse>>
{
    public async Task<Result<CreateCourseResponse>> Handle(
        CreateCourseCommand request, CancellationToken cancellationToken)
    {
        // 1. Auth check
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("User is not authenticated."));

        // 2. Role / ownership check
        if (!currentUser.IsInRole(Roles.Instructor) && !currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("Only instructors can create courses."));

        // 3. Load dependency via specification
        if (!await categoryRepository.AnyAsync(new CategoryByIdSpecification(request.CategoryId), cancellationToken))
            return Result.Fail(new NotFoundError($"Category '{request.CategoryId}' was not found."));

        // 4. Call domain factory / method (happy path only)
        var course = Course.Create(currentUser.UserId.Value, request.CategoryId, request.Title, ...);

        // 5. Persist
        await courseRepository.AddAsync(course, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Return
        return Result.Ok(new CreateCourseResponse(course.Id));
    }
}
```

### Typical query handler shape
```csharp
public sealed class GetCourseByIdQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<GetCourseByIdQuery, Result<CourseDetailDto>>
{
    public async Task<Result<CourseDetailDto>> Handle(
        GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, includeSections: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError($"Course '{request.CourseId}' was not found."));

        // Map to DTO — never return domain entity
        return Result.Ok(new CourseDetailDto(course.Id, course.Title, ...));
    }
}
```

---

## Controller Pattern

Controllers are **thin**. The only logic allowed: route → send → map result.

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CoursesController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCourseByIdQuery(id), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Create([FromBody] CreateCourseCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult(onSuccess: value =>
            CreatedAtAction(nameof(GetById), new { id = value.CourseId }, value));
    }

    // Void result (204 No Content):
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new PublishCourseCommand(id), ct);
        return result.ToActionResult(); // → 204 on success
    }
}
```

`ToActionResult()` lives in `Learnix.API/Extensions/ResultExtensions.cs`. Always use it — never manually check `result.IsFailed` in controllers.

---

## Validation

FluentValidation validators run automatically via `ValidationBehavior` in the MediatR pipeline.

```csharp
public sealed class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(CourseConstants.TitleMaxLength); // Always use Domain Constants, not magic numbers
    }
}
```

- Always reference `Domain/Constants/{Entity}Constants.cs` for length limits — never hardcode numbers.
- Application-layer validation policies (password rules, RFC email) go in `Application/{Feature}/Constants/{Feature}ValidationConstants.cs`.
- Validators are registered automatically via assembly scanning — no manual DI needed.

---

## Specification Pattern

Uses **Ardalis.Specification**. Never write raw LINQ in handlers.

```csharp
public sealed class CourseByIdSpecification : Specification<Course>, ISingleResultSpecification<Course>
{
    public CourseByIdSpecification(Guid id, bool includeSections = false, bool forUpdate = false)
    {
        Query.Where(c => c.Id == id);

        if (includeSections)
            Query.Include(c => c.Sections).ThenInclude(s => s.Lessons);

        if (!forUpdate)
            Query.AsNoTracking(); // Default: always AsNoTracking
    }
}
```

**Key rule:**
- All specifications default to `AsNoTracking()` — read-only.
- Omit `AsNoTracking()` **only** for specifications used in Commands that modify the entity (`forUpdate: true`).
- Location: `Application/{Feature}/Specifications/{Name}Specification.cs`

---

## Repository & Unit of Work

```csharp
// Application layer interface — extends Ardalis IRepositoryBase<T>
public interface ICourseRepository : IRepositoryBase<Course> { }

// Usage in handlers:
var course = await courseRepository.FirstOrDefaultAsync(new CourseByIdSpec(id, forUpdate: true), ct);
// Modify via domain methods...
await unitOfWork.SaveChangesAsync(ct); // Always via IUnitOfWork, never DbContext directly
```

- `SaveChangesAsync` is called **only in handlers**, never inside repositories.
- `IUnitOfWork` and `ApplicationDbContext` resolve to the same scoped instance.
- `ICurrentUserService` is also scoped — inject it in handlers, not controllers.

---

## Domain Entity Conventions

- Properties have `private set` — never public setters.
- State change only via methods that reflect business operations:
  ```csharp
  // Good — one method = one business action
  public void UpdateDetails(string title, string description, decimal price, Guid categoryId) { ... }
  public void Publish() { ... }
  
  // Bad — property-level setters
  public void SetTitle(string title) { ... }
  ```
- Domain methods that violate invariants throw `DomainException` (caught by `DomainExceptionBehavior` → `ConflictError`). Handlers don't need try/catch for these.
- Entities implement marker interfaces: `IAuditable`, `IHasDomainEvents`, `ISoftDeletable` (if applicable), `IOrderable` (if applicable).
- Most entities extend `BaseEntity` (provides `Id`, `CreatedAt`, `UpdatedAt`, domain events). Exception: `User` extends `IdentityUser<Guid>` and implements the interfaces directly.

### Soft delete
- Soft-deletable entities implement `ISoftDeletable` (currently: `Course` via `SoftDeletableEntity`, and `User`, which implements it directly since it extends `IdentityUser<Guid>`).
- `SoftDeleteInterceptor` intercepts `Remove()` — sets `IsDeleted + DeletedAt`.
- Global EF query filter auto-excludes soft-deleted records.
- Use `.IgnoreQueryFilters()` only for admin queries that intentionally need deleted records.

---

## Domain Events

Events are raised inside entity methods and dispatched by `DomainEventsInterceptor` **after** `SaveChangesAsync` succeeds.

```csharp
// In Domain entity:
protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

// Example:
public void Publish()
{
    EnsurePublishableInvariants();
    Status = CourseStatus.Published;
    RaiseDomainEvent(new CoursePublishedDomainEvent(Id));
}
```

Event handlers in Application subscribe to the MediatR wrapper:
```csharp
public class CoursePublishedHandler
    : INotificationHandler<DomainEventNotification<CoursePublishedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<CoursePublishedDomainEvent> n, CancellationToken ct)
    {
        // React to the event
    }
}
```

`IDomainEvent` has no MediatR dependency — the `DomainEventNotification<T>` wrapper lives in `Application/Common/Events/` (see ADR-BACK-ARCH-008).

---

## Blob Storage — Upload Pattern

All file uploads follow the **temp → final** SAS flow (ADR-BLOB-003). **Never use `IFormFile` or server-side uploads** for user-initiated files.

```
1. Client → POST /api/uploads/request-url { target, contentType }
   ← returns { uploadUrl (SAS, Create-only, 15 min), blobPath: "temp-uploads/{guid}" }

2. Client → PUT {uploadUrl}   (direct to Azure Blob, into temp-uploads)

3. Client → PUT /api/{resource} { blobPath: "temp-uploads/{guid}" }
   Handler → IBlobStorageService.CommitUploadAsync(tempBlobPath, target)   ← SYNCHRONOUS
              ├─ size check, magic-byte content-type check (a bad file deletes the temp blob → Result.Fail)
              ├─ copy temp → final container, delete temp, rewrite Content-Type with the trusted value
              └─ returns the PERMANENT blobPath
   Handler → entity.SetAvatar(commitResult.Value.BlobPath)   ← store the permanent path, not the temp one
   Handler → unitOfWork.SaveChangesAsync()
   DomainEventsInterceptor → the *Removed/*Released event handler enqueues OutboxMessage(DeleteBlob, oldPath)
   OutboxWorker → IBlobStorageService.DeleteAsync(oldPath)   ← deletes the REPLACED blob
```

**The Outbox never confirms a blob — it only deletes one.** There is no `MarkBlobConfirmed` message type and no `IBlobStorageService.ValidateAsync`. Promotion to the permanent container happens synchronously, in the handler, before `SaveChangesAsync`. Abandoned uploads are reaped by an Azure lifecycle policy that clears `temp-uploads` after 24 h.

Entities store the relative path `{container}/{blobName}` (e.g. `avatars/9f2c...`), **not** a full SAS URL. The container prefix is mandatory — `DeleteAsync` / `GenerateReadUrl` / `GetPublicUrl` all call `ParseBlobPath`, which throws without a `/`. That is also why domain events carry only the path and no entity type: the consumer derives the container from the value itself.

`{blobName}` is opaque; its shape depends on the producer (`CommitUploadAsync` → bare `guid:N`; the seeder → `{guid}-cover.webp`; certificates → `{code}.pdf`). Never parse it — never assume it is a GUID.

Read URLs are generated on demand: `GetPublicUrl()` for public containers (avatars, covers, category images), `GenerateReadUrl(path, ttl)` for protected ones (videos, certificates).

---

## MediatR Pipeline Order

```
LoggingBehavior            ← outermost; logs request name + duration; warns >3s
  ValidationBehavior       ← rejects invalid requests; returns Result.Fail(ValidationError)
    DomainExceptionBehavior ← catches DomainException → ConflictError
      CachingBehavior      ← innermost; only for requests implementing ICacheable<TValue>
        Handler            ← happy path only; no try/catch for domain rules
```

- Order is defined by registration order in `Application/DependencyInjection.cs`.
- `CachingBehavior` is registered as a **closed** generic per request type — only queries implementing `ICacheable<TValue>` get it. Cache keys live in `Application/Common/Constants/CacheKeys.cs` (ADR-BACK-ARCH-013), backed by Redis.
- Request payload is **never logged** (prevents PII leaks — passwords, tokens).
- `ExceptionHandlingMiddleware` catches only unexpected infrastructure failures (500).

---

## Application Settings

Config POCOs live in `Application/Common/Settings/{Name}Settings.cs`. Registered via `services.Configure<T>(...)` in `Infrastructure/DependencyInjection.cs`. Consumed via `IOptions<T>` in Infrastructure implementations.

Never inject `IConfiguration` directly into Application layer classes.

---

## Database Migrations

```bash
# Add new migration
dotnet ef migrations add {Name} \
    --project Learnix.Infrastructure \
    --startup-project Learnix.API \
    --output-dir Persistence/EntityFramework/Migrations

# Apply — via the standalone migrator, NOT `dotnet ef database update`
dotnet run --project Learnix.DbMigrator --launch-profile Development -- --create-blob --seed-demo
# or, through Docker Compose:
docker compose --profile init up migrator
```

- **Migrations are never auto-applied on API startup.** `Learnix.API/Program.cs` does not call `ApplyMigrationsAsync()`; `Learnix.DbMigrator` runs `Database.MigrateAsync()` as a separate step in dev and in CI/CD (see ADR MIGRATIONS.md).
- `--create-blob` initializes local blob containers; `--seed-demo` seeds fake courses + a demo student. Role, admin and category seeders always run.
- The API image ships without EF tooling by design — it has no DDL rights at runtime.

---

## Unit Tests

Two projects: `Learnix.Domain.UnitTests` (entity invariants, domain methods) and `Learnix.Application.UnitTests` (handlers, validators, behaviors).

Stack: **xUnit + FluentAssertions + NSubstitute**. `Xunit`, `FluentAssertions` and `NSubstitute` are global usings via `<Using Include="..." />` — don't re-import them.

- Mirror the production namespace in the test folder: `Application/Auth/Commands/Logout/` → `Application.UnitTests/Auth/Commands/Logout/LogoutCommandHandlerTests.cs`.
- Test class: `{TypeUnderTest}Tests`. Dependencies substituted in the constructor, handler built once into a `_handler` field.
- Test name: `Handle_When{Condition}_Should{ExpectedOutcome}`.
- Body uses explicit `// Arrange` / `// Act` / `// Assert` comments.
- Assert with FluentAssertions (`result.IsSuccess.Should().BeTrue()`), and verify collaborators with `Received()` / `DidNotReceive()`.
- Never mock `ApplicationDbContext` — substitute the repository interface instead.

```csharp
[Fact]
public async Task Handle_WhenRefreshTokenIsEmpty_ShouldReturnOkWithoutRevoking()
{
    // Arrange
    var command = new LogoutCommand(string.Empty);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
}
```

Coverage is collected in CI (`coverage.runsettings`) and reported to SonarCloud.

---

## Anti-Patterns — Never Do These

| Anti-pattern | Correct approach |
|---|---|
| Throw exceptions for business errors | `return Result.Fail(new NotFoundError(...))` |
| Raw LINQ in handlers | Build a `Specification<T>` |
| DTOs in a separate `/DTOs` folder | Co-locate `{Name}Response.cs` with its query handler |
| Business logic in controllers | Delegate to handler via `sender.Send()` |
| Business logic in Infrastructure | Move to Application handler or domain method |
| `currentUser.UserId` used in controller | Inject `ICurrentUserService` into the handler |
| `DbContext` injected directly in Application | Use `IUnitOfWork` / repository interface |
| AutoMapper | Manual `ToDto()` / `ToResponse()` extension methods |
| Public property setters on domain entities | `private set`; state via domain methods |
| `IFormFile` for file uploads | 3-step SAS URL flow via `UploadsController` |
| Entity returned from handler | Map to DTO before returning |
| `INotification` on `IDomainEvent` | Keep `IDomainEvent` clean; wrap in `DomainEventNotification<T>` |

---

## Post-Task Checklist

After completing **any** backend task:

1. **Build and test** — `dotnet build Learnix.Backend.slnx` then `dotnet test Learnix.Backend.slnx`. Every project sets `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild`, so an unused using or unused private member **fails the build**.
2. **Format** — `dotnet format Learnix.Backend.slnx`. CI runs `--verify-no-changes` and fails otherwise.
3. **Update `docs/TODO.md`** — set the task's `Status` column to `done` (the file uses status tables, not `[x]` checkboxes). Add a note in the `Notes` column if the implementation deviated from the spec or introduced constraints worth remembering.
4. **Update ADR files** — if a new architectural decision was made, a pattern was changed, or a constraint was added: add an entry to the appropriate file in `docs/backend/decisions/` using `ADR-BACK-<SCOPE>-NNN` numbering. Numbering is **scoped per file** — read the file first and take the next free number after its current highest. If an existing ADR was superseded, mark it `Superseded by ADR-BACK-<SCOPE>-NNN`. A brand-new topic gets a new file from `TEMPLATE.md`, registered in `decisions/README.md`.

> Note: `docs/backend/decisions/DOMAIN.md` still uses the legacy un-scoped `ADR-NNN` numbering and is written in Ukrainian. Follow its existing local convention when editing that file; use `ADR-BACK-<SCOPE>-NNN` everywhere else.
