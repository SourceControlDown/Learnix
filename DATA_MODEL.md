# Learnix — Data Model

> **Note:** This is a living document. Updated to reflect the codebase as of Phase 3.
> PostgreSQL entities are mapped via EF Core. MongoDB documents via MongoDB.Driver (planned Phase 7+).
> Entities marked **[Planned]** are not yet implemented in Domain/Infrastructure.

---

## PostgreSQL Entities

---

### User
Primary identity entity. Managed by ASP.NET Core Identity (`IdentityUser<Guid>` base).
Implements `IAuditable`, `IHasDomainEvents` directly (cannot inherit `BaseEntity` due to `IdentityUser<Guid>` conflict — see ADR-023).

NOTE: Roles managed by ASP.NET Identity tables (AspNetRoles, AspNetUserRoles); access via `UserManager.GetRolesAsync()`.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK (from `IdentityUser<Guid>`) |
| `Email` | `string` | Unique, required |
| `NormalizedEmail` | `string` | Identity-managed |
| `UserName` | `string` | Mirrors Email |
| `PasswordHash` | `string?` | Null for Google-only accounts |
| `EmailConfirmed` | `bool` | Default: false |
| `FirstName` | `string` | Required, max 100 |
| `LastName` | `string` | Required, max 100 |
| `AvatarBlobPath` | `string?` | Azure Blob path (not a full URL) |
| `Bio` | `string?` | Max 500 chars |
| `GoogleId` | `string?` | For Google OAuth accounts (see ADR-037) |
| `CreatedAt` | `DateTime` | UTC, set by `AuditableInterceptor` |
| `UpdatedAt` | `DateTime` | UTC, set by `AuditableInterceptor` |

**Domain methods:** `UpdateProfile()`, `SetAvatar()`, `SetGoogleId()`, `ClaimViaGoogle()`, `ConfirmEmailFromGoogle()`, `RaiseUserRegistered()`, `RaisePasswordResetRequested()`

**Relations:**
- Has many `Enrollment`
- Has many `LessonProgress`
- Has many `TestAttempt`
- Has many `Certificate`
- Has many `CourseReview`
- Has many `Course` (as instructor)
- Has many `RefreshToken`
- Has many `LessonLike` **[Planned]**
- Has many `Payment` **[Planned]**
- Has many `Notification` **[Planned]**
- Has many `UserAchievement` **[Planned]**
- Has one `InstructorApplication` **[Planned]**
- Has many `UserCategoryPreference` **[Planned]**

---

### RefreshToken
Stored hashed refresh tokens for JWT rotation (see ADR-033).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `TokenHash` | `string` | SHA-256 hash, never stored plain |
| `ExpiresAt` | `DateTime` | UTC |
| `IsRevoked` | `bool` | Default: false |
| `RevokedAt` | `DateTime?` | UTC, set on revocation |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

Computed: `IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow`

---

### OutboxMessage
Reliable blob-storage operation queue. Written in the same transaction as entity changes; processed by a background worker (see ADR-047).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Type` | `string` | `DeleteBlob` / `MarkBlobConfirmed` (max 100) |
| `Payload` | `string` | JSON payload, stored as JSONB |
| `OccurredAt` | `DateTime` | UTC |
| `ProcessedAt` | `DateTime?` | UTC, set on successful processing |
| `AttemptCount` | `int` | Default: 0 |
| `LastAttemptAt` | `DateTime?` | UTC |
| `LastError` | `string?` | Last error message (max 2000) |
| `NextRetryAt` | `DateTime?` | UTC, exponential backoff |

**Index:** `IX_OutboxMessages_Processing` on `(ProcessedAt, NextRetryAt, OccurredAt)` — used by polling worker to find unprocessed messages.

Payload types:
- `DeleteBlobPayload(string BlobPath)` — blob to delete
- `MarkBlobConfirmedPayload(string BlobPath)` — blob to confirm after entity persisted

---

### InstructorApplication **[Planned — Phase 5]**
Student submits to become an Instructor. Admin reviews.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User, Unique |
| `MotivationText` | `string` | Required, max 2000 chars |
| `PortfolioUrl` | `string?` | Optional link |
| `Status` | `ApplicationStatus` (enum) | Pending / Approved / Rejected |
| `RejectionReason` | `string?` | Filled on rejection |
| `ReviewedByAdminId` | `Guid?` | FK → User |
| `CreatedAt` | `DateTime` | UTC |
| `ReviewedAt` | `DateTime?` | UTC |

---

### Category

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Name` | `string` | Required, Unique |
| `Slug` | `string` | URL-friendly, Unique |
| `IsSystem` | `bool` | True for seeded categories — protected from rename/delete (see ADR-042) |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

**Relations:**
- Has many `Course`

---

### UserCategoryPreference **[Planned]**
Many-to-many between User and Category for recommendation tuning.

| Field | Type | Notes |
|---|---|---|
| `UserId` | `Guid` | FK → User, PK composite |
| `CategoryId` | `Guid` | FK → Category, PK composite |

---

### Course

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `InstructorId` | `Guid` | FK → User |
| `CategoryId` | `Guid` | FK → Category |
| `Title` | `string` | Required, max 200 chars |
| `Description` | `string` | Markdown supported |
| `CoverBlobPath` | `string?` | Azure Blob path — required before Publish |
| `Price` | `decimal` | 0 = free (no separate `IsFree` flag — see ADR-043) |
| `Status` | `CourseStatus` (enum) | Draft / Published / Archived |
| `EnrollmentsCount` | `int` | Denormalized for sorting (see ADR-041) |
| `Tags` | `string[]` | PostgreSQL array |
| `IsDeleted` | `bool` | Soft delete flag |
| `DeletedAt` | `DateTime?` | UTC, set on soft delete |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

**Aggregate root.** All structural operations (sections, lessons) go through `Course` methods. Publishes domain events on lifecycle transitions.

**Relations:**
- Belongs to `User` (instructor)
- Belongs to `Category`
- Has many `Section`
- Has many `Enrollment`
- Has many `CourseReview`
- Has many `LessonProgress`
- Has many `Payment` **[Planned]**

---

### Section
Groups lessons within a course. Part of the Course aggregate — all mutations via `Course` methods (see ADR-044).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `CourseId` | `Guid` | FK → Course |
| `Title` | `string` | Required |
| `DisplayOrder` | `int` | Display order within course |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

**Relations:**
- Belongs to `Course`
- Has many `Lesson`

---

### Lesson
**TPH (Table Per Hierarchy)** — single `Lessons` table with `LessonType` discriminator. Part of the Course aggregate.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `SectionId` | `Guid` | FK → Section |
| `Title` | `string` | Required |
| `DisplayOrder` | `int` | Display order within section |
| `IsHidden` | `bool` | Defaults true; set to false when lesson is ready |
| `LessonType` | `LessonType` (enum) | Video / Post / Test — EF discriminator |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

#### VideoLesson (extends Lesson)
| Field | Type | Notes |
|---|---|---|
| `VideoBlobPath` | `string` | Azure Blob path |
| `Description` | `string?` | Markdown |
| `DurationSeconds` | `int?` | Optional metadata |

Raises `LessonVideoAttachedDomainEvent` when video is set, `LessonVideoReleasedDomainEvent` when replaced.

#### PostLesson (extends Lesson)
| Field | Type | Notes |
|---|---|---|
| `Content` | `string` | Markdown — required for `IsPublishReady` |

#### TestLesson (extends Lesson)
| Field | Type | Notes |
|---|---|---|
| `Description` | `string?` | Instructions for student |
| `AttemptLimit` | `int?` | Null = unlimited |
| `CooldownMinutes` | `int?` | Wait time after exhausting attempts |
| `PassingThreshold` | `int` | Percentage required to pass (e.g. 70) |
| `Questions` | owned collection | `Question[]` value objects, stored as JSONB |

**Relations (Lesson):**
- Belongs to `Section`
- Has many `LessonProgress`
- Has many `LessonLike` **[Planned]**

**Relations (TestLesson):**
- Has many `TestAttempt`

---

### Question
**Owned type (value object)** — stored as JSONB inside `TestLesson`. Not a separate table.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Auto-generated on creation |
| `Text` | `string` | Required |
| `Type` | `QuestionType` (enum) | SingleChoice / MultipleChoice / TextInput |
| `Order` | `int` | Display order within test |
| `Options` | `QuestionOption[]` | Owned collection (choice types) |
| `TextAnswer` | `TextAnswerConfig?` | Owned (text input type only) |

Contains scoring logic: `IsAnsweredCorrectly()`, `EvaluateTextAnswer()`, Levenshtein distance for fuzzy matching.

---

### QuestionOption
**Owned value object** — nested inside `Question.Options`.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Auto-generated |
| `Text` | `string` | Required |
| `IsCorrect` | `bool` | |
| `Order` | `int` | Display order |

---

### TextAnswerConfig
**Owned value object** — nested inside `Question.TextAnswer`.

| Field | Type | Notes |
|---|---|---|
| `CorrectAnswer` | `string` | Expected answer |
| `IgnoreCase` | `bool` | Default: true |
| `AllowFuzzy` | `bool` | Allow 1-char Levenshtein distance if length > 1 |

---

### TestAttempt

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `StudentId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `TestLessonId` | `Guid` | FK → Lesson |
| `AttemptNumber` | `int` | Sequence per student per test |
| `StartedAt` | `DateTime` | UTC |
| `SubmittedAt` | `DateTime?` | UTC, set on submit |
| `Score` | `int?` | Percentage, filled on submission |
| `MaxScore` | `int?` | Total possible points |
| `Passed` | `bool?` | Filled on submission |
| `Answers` | `StudentAnswer[]` | Owned value objects (JSONB) |

Computed: `IsSubmitted => SubmittedAt.HasValue`

#### StudentAnswer (owned value object)
```csharp
record StudentAnswer(Guid QuestionId, List<Guid> SelectedOptionIds, string? TextValue);
```

---

### Enrollment

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `StudentId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `Status` | `EnrollmentStatus` (enum) | Active / Completed |
| `PaymentStatus` | `PaymentStatus` (enum) | Pending / Completed / Failed |
| `PricePaid` | `decimal` | 0 for free courses |
| `EnrolledAt` | `DateTime` | UTC |
| `CompletedAt` | `DateTime?` | UTC |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

**Unique constraint:** `(StudentId, CourseId)`

---

### LessonProgress

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `StudentId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `LessonId` | `Guid` | FK → Lesson |
| `IsCompleted` | `bool` | Default: false |
| `LastAccessedAt` | `DateTime` | UTC |
| `CompletedAt` | `DateTime?` | UTC |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

**Unique constraint:** `(StudentId, LessonId)`

---

### LessonLike **[Planned]**

| Field | Type | Notes |
|---|---|---|
| `UserId` | `Guid` | FK → User, PK composite |
| `LessonId` | `Guid` | FK → Lesson, PK composite |
| `CreatedAt` | `DateTime` | UTC |

---

### Certificate

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `StudentId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `EnrollmentId` | `Guid` | FK → Enrollment, Unique |
| `Code` | `string` | Public verification code, Unique |
| `FileUrl` | `string?` | Azure Blob URL, attached after PDF generation |
| `IssuedAt` | `DateTime` | UTC |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

---

### CourseReview

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `CourseId` | `Guid` | FK → Course |
| `StudentId` | `Guid` | FK → User |
| `Rating` | `int` | 1–5 |
| `Comment` | `string?` | Optional text |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

**Unique constraint:** `(StudentId, CourseId)` — one review per student per course

---

### Achievement **[Planned — Phase 6]**

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Key` | `string` | Unique code e.g. `first_lesson` |
| `Name` | `string` | Display name |
| `Description` | `string` | |
| `IconUrl` | `string?` | |
| `XpValue` | `int` | |

Seeded at startup. Not created by users or admins.

---

### UserAchievement **[Planned — Phase 6]**

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `AchievementId` | `Guid` | FK → Achievement |
| `EarnedAt` | `DateTime` | UTC |

**Unique constraint:** `(UserId, AchievementId)`

---

### Payment **[Planned — Phase 5]**

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `Amount` | `decimal` | |
| `Currency` | `string` | Default: `USD` |
| `Status` | `PaymentStatus` (enum) | Pending / Completed / Failed |
| `StripeSessionId` | `string?` | Stripe Checkout session ID |
| `CreatedAt` | `DateTime` | UTC |
| `CompletedAt` | `DateTime?` | UTC |

---

### Notification **[Planned — Phase 7]**

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `Type` | `NotificationType` (enum) | |
| `Title` | `string` | |
| `Body` | `string` | |
| `IsRead` | `bool` | Default: false |
| `RelatedEntityId` | `Guid?` | Optional reference |
| `RelatedEntityType` | `string?` | e.g. `Course`, `Achievement` |
| `CreatedAt` | `DateTime` | UTC |

---

## Enums

```csharp
public enum CourseStatus      { Draft, Published, Archived }
public enum EnrollmentStatus  { Active, Completed }
public enum PaymentStatus     { Pending, Completed, Failed }
public enum LessonType        { Video, Post, Test }
public enum QuestionType      { SingleChoice, MultipleChoice, TextInput }
public enum ApplicationStatus { Pending, Approved, Rejected }     // [Planned]
public enum NotificationType  { ... }                              // [Planned]

// Blob storage upload targets
public enum UploadTarget
{
    Avatar,       // 5 MB max, jpeg/png/webp
    CourseCover,  // 10 MB max, jpeg/png/webp
    LessonVideo,  // 2 GB max, mp4/webm
    Certificate   // 5 MB max, pdf
}
```

---

## Domain Events

Events raised by entities and dispatched after `SaveChangesAsync` via `DomainEventsInterceptor`.

| Event | Raised by | Carries |
|---|---|---|
| `UserRegisteredDomainEvent` | `User.RaiseUserRegistered()` | UserId, Email, FirstName, EmailConfirmationToken |
| `PasswordResetRequestedDomainEvent` | `User.RaisePasswordResetRequested()` | UserId, Email, FirstName, Token |
| `UserAvatarSetDomainEvent` | `User.SetAvatar()` | UserId, BlobPath |
| `UserAvatarRemovedDomainEvent` | `User.SetAvatar()` (when replacing old) | UserId, ReleasedBlobPath |
| `CourseCreatedDomainEvent` | `Course.Create()` | CourseId, InstructorId, CategoryId |
| `CoursePublishedDomainEvent` | `Course.Publish()` | CourseId |
| `CourseUnpublishedDomainEvent` | `Course.Unpublish()` | CourseId |
| `CourseArchivedDomainEvent` | `Course.Archive()` | CourseId |
| `CourseDeletedDomainEvent` | `Course.MarkForDeletion()` | CourseId |
| `LessonVideoAttachedDomainEvent` | `VideoLesson.ReplaceVideo()` | LessonId, AttachedBlobPath |
| `LessonVideoReleasedDomainEvent` | `VideoLesson.ReplaceVideo()` | LessonId, ReleasedBlobPath |

All events extend `DomainEvent` abstract record (adds `EventId: Guid`, `OccurredAt: DateTime`).

---

## MongoDB Documents **[Planned — Phase 7+]**

---

### ChatSession **[Planned]**
AI assistant conversation history per user.

```json
{
  "_id": "ObjectId",
  "userId": "Guid (string)",
  "createdAt": "DateTime",
  "updatedAt": "DateTime",
  "messages": [
    {
      "role": "user | assistant",
      "content": "string",
      "sentAt": "DateTime"
    }
  ]
}
```

**Index:** `userId`
**Collection:** `chat_sessions`

---

## Key Relations Summary

```
User ──< Enrollment >── Course
User ──< LessonProgress >── Lesson
User ──< TestAttempt >── TestLesson
User ──< Certificate >── Enrollment
User ──< CourseReview >── Course
Course ──< Section ──< Lesson
TestLesson ──< Question (JSONB) ──< QuestionOption (nested)
TestAttempt ──< StudentAnswer (JSONB)

[Planned]
User ──< LessonLike >── Lesson
User ──< UserAchievement >── Achievement
User ──< UserCategoryPreference >── Category
User ──< Payment >── Course
```

---

## Notes

- `AvatarBlobPath`, `CoverBlobPath`, `VideoBlobPath` store Azure Blob **paths** (e.g. `avatars/users/{id}/{uploadId}.jpg`), not full SAS URLs. Read URLs are generated on demand via `IBlobStorageService.GenerateReadUrl()`.
- `EnrollmentsCount` on `Course` is denormalized. Oновлюється в Phase 4 (стратегія TBD — see ADR-041).
- `Question`, `QuestionOption`, `TextAnswerConfig`, `StudentAnswer` are **value objects** stored as JSONB inside `TestLesson` / `TestAttempt`. No separate tables.
- `Tags` stored as PostgreSQL array (`string[]`). If filtering by tags becomes complex, consider separate `Tag` + `CourseTag` tables.
- `CourseReview` lives in PostgreSQL (not MongoDB) — rating+comment are structured, benefit from joins with Course/User.
- `OutboxMessage` records are written in the same EF transaction as entity changes, ensuring blob operations (confirm/delete) are never lost on process crash (see ADR-047).
