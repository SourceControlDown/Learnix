# Learnix — Architecture Specification

> For detailed architectural decision records (ADRs) and rationale, refer to the [Documentation Index](#documentation-index).

## Overview

**Pattern:** Clean Architecture + Light DDD + CQRS  
**API:** ASP.NET Core 8  
**Mediator:** MediatR  
**Validation:** FluentValidation (pipeline behavior, returns Result)  
**Result pattern:** FluentResults  
**ORM:** Entity Framework Core (PostgreSQL via Npgsql)  
**Blob storage:** Azure Blob Storage (via Azure SDK, SAS URLs)  
**NoSQL:** MongoDB.Driver — **[Planned Phase 7+]**  
**Cache:** Redis (StackExchange.Redis) — distributed cache via `ICacheable<TValue>` + `CachingBehavior`

---

## Layer Structure

```text
Learnix.Domain           — Entities, Value Objects, Domain Events, Enums, Constants
Learnix.Application      — CQRS, Validators, Specifications, Interfaces, Pipeline Behaviors
Learnix.Infrastructure   — EF Core, Azure Blob, External Services, Outbox Worker
Learnix.API              — Controllers, Middleware, DI registration
```

### Dependency rule
```text
API → Application → Domain        (allowed)
Infrastructure → Application      (allowed, implements interfaces)
Domain → nothing                  (no dependencies)
Application → Infrastructure      (FORBIDDEN — only via interfaces)
```

---

## Request Flow

```text
HTTP Request
    ↓
Controller               (routes to MediatR, returns IActionResult)
    ↓
MediatR Pipeline
    ├── LoggingBehavior         (logs request name + duration, warns >3s)
    ├── ValidationBehavior      (FluentValidation → Result.Fail if invalid)
    └── DomainExceptionBehavior (catches DomainException → ConflictError)
    ↓
Command / Query Handler  (business logic, happy path only)
    ↓
    ├── [Query]   → repository.FirstOrDefaultAsync/ListAsync(specification)
    │             → map to DTO → return Result<T>
    │
    └── [Command] → repository.FirstOrDefaultAsync(specification, forUpdate: true)
                  → call entity method (entity raises Domain Event + enqueues Outbox messages)
                  → unitOfWork.SaveChangesAsync()
                       ↓ (DomainEventsInterceptor fires after commit)
                  → Domain Event dispatched via MediatR INotificationHandler (in-process)
                  → Outbox worker (background) processes blob confirm/delete messages
```

---

## Documentation Index

The detailed architectural decisions and rules are organized into specific topical documents. Please review them for the rationale behind our tech stack and patterns:

- [**Project Structure**](PROJECT_STRUCTURE.md) — Directory tree and file conventions.
- [**Architecture & CQRS**](DECISIONS_ARCHITECTURE.md) — CQRS rules, Result pattern, Pagination, Specifications.
- [**Domain Model**](DECISIONS_DOMAIN.md) — Entity design, soft delete, invariants, value objects.
- [**Infrastructure**](DECISIONS_INFRA.md) — PostgreSQL, MongoDB, Redis, Outbox, EF Core interceptors, Repository pattern.
- [**Blob Storage**](DECISIONS_BLOB.md) — Upload flow, validation, pre-signed SAS URLs.
- [**Authentication & Security**](DECISIONS_AUTH.md) — JWT, Google OAuth, Refresh Tokens, Email confirmation gate.
- [**Messaging**](DECISIONS_MESSAGING.md) — Direct messaging system architecture.
- [**AI Chat**](DECISIONS_CHAT.md) — AI Chatbot context architecture.
- [**Reviews & Ratings**](DECISIONS_REVIEWS.md) — Course reviews and denormalized ratings.

> **Note:** When introducing new architectural decisions or modules, please refer to [**DECISIONS_TEMPLATE.md**](DECISIONS_TEMPLATE.md) for formatting and numbering conventions.
