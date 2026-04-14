# Learnix — Data Model

> **Note:** This is a living document. Fields and relations will evolve during implementation.
> PostgreSQL entities are mapped via EF Core. MongoDB documents via MongoDB.Driver.

---

## PostgreSQL Entities

---

### User
Primary identity entity. Managed by ASP.NET Core Identity (`IdentityUser<Guid>` base).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Email` | `string` | Unique, required |
| `NormalizedEmail` | `string` | Identity |
| `PasswordHash` | `string?` | Null for Google-only accounts |
| `FirstName` | `string` | Required |
| `LastName` | `string` | Required |
| `AvatarUrl` | `string?` | Azure Blob URL |
| `Bio` | `string?` | Max 500 chars |
| `Role` | `UserRole` (enum) | Student / Instructor / Admin |
| `IsEmailConfirmed` | `bool` | Default: false |
| `GoogleId` | `string?` | For OAuth users |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

**Relations:**
- Has many `Enrollment`
- Has many `LessonProgress`
- Has many `LessonLike`
- Has many `Payment`
- Has many `UserAchievement`
- Has many `Notification`
- Has many `Message` (as sender)
- Has one `InstructorApplication`
- Has many `Course` (as instructor)
- Has many `RefreshToken`
- Has many `UserPreference` → via `UserCategoryPreference`

---

### RefreshToken
Stored refresh tokens for JWT rotation.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `TokenHash` | `string` | Hashed, never stored plain |
| `ExpiresAt` | `DateTime` | UTC |
| `IsRevoked` | `bool` | Default: false |
| `CreatedAt` | `DateTime` | UTC |
| `RevokedAt` | `DateTime?` | UTC |

---

### InstructorApplication
Student submits to become an Instructor. Admin reviews.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User, Unique (one active at a time) |
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

**Relations:**
- Has many `Course`

---

### UserCategoryPreference
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
| `CoverImageUrl` | `string?` | Azure Blob URL |
| `Price` | `decimal` | 0 = free |
| `Status` | `CourseStatus` (enum) | Draft / Published / Archived |
| `EnrollmentsCount` | `int` | Denormalized for sorting, updated on enroll |
| `Tags` | `string[]` | PostgreSQL array |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

**Relations:**
- Belongs to `User` (instructor)
- Belongs to `Category`
- Has many `Section`
- Has many `Enrollment`
- Has many `Payment`
- Has many `Message`

---

### Section
Groups lessons within a course.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `CourseId` | `Guid` | FK → Course |
| `Title` | `string` | Required |
| `Order` | `int` | Display order within course |

**Relations:**
- Belongs to `Course`
- Has many `Lesson`

---

### Lesson
**TPH (Table Per Hierarchy)** — single `Lessons` table with `LessonType` discriminator.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `SectionId` | `Guid` | FK → Section |
| `Title` | `string` | Required, always rendered bold |
| `Order` | `int` | Display order within section |
| `LessonType` | `LessonType` (enum) | Video / Post / Test — EF discriminator |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

#### VideoLesson (extends Lesson)
| Field | Type | Notes |
|---|---|---|
| `VideoUrl` | `string` | Azure Blob URL |
| `Description` | `string?` | Markdown |
| `DurationSeconds` | `int?` | Optional metadata |

#### PostLesson (extends Lesson)
| Field | Type | Notes |
|---|---|---|
| `Content` | `string` | Markdown — supports bold, italic, underline, lists |

#### TestLesson (extends Lesson)
| Field | Type | Notes |
|---|---|---|
| `Description` | `string?` | Instructions for student |
| `AttemptLimit` | `int?` | Null = unlimited |
| `CooldownMinutes` | `int?` | Wait time after exhausting attempts |
| `PassingThreshold` | `int` | Percentage required to pass (e.g. 70) |

**Relations (Lesson):**
- Belongs to `Section`
- Has many `LessonProgress`
- Has many `LessonLike`

**Relations (TestLesson):**
- Has many `Question`
- Has many `TestAttempt`

---

### Question

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `TestLessonId` | `Guid` | FK → Lesson (TestLesson) |
| `Text` | `string` | Required |
| `Type` | `QuestionType` (enum) | SingleChoice / MultipleChoice / TextInput |
| `Order` | `int` | Display order |
| `Points` | `int` | Default: 1 |

**Relations:**
- Belongs to `TestLesson`
- Has many `QuestionOption` (for choice types)
- Has one `TextAnswerConfig` (for text input type)
- Has many `TestAttemptAnswer`

---

### QuestionOption
Options for SingleChoice and MultipleChoice questions.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `QuestionId` | `Guid` | FK → Question |
| `Text` | `string` | Required |
| `IsCorrect` | `bool` | |

---

### TextAnswerConfig
Config for TextInput questions.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `QuestionId` | `Guid` | FK → Question, Unique |
| `CorrectAnswer` | `string` | Expected answer |
| `IgnoreCase` | `bool` | Default: true |
| `AllowFuzzy` | `bool` | Allow 1 char diff if length > 1 |

---

### TestAttempt

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `TestLessonId` | `Guid` | FK → Lesson |
| `Score` | `int?` | Percentage, filled on completion |
| `IsPassed` | `bool?` | Filled on completion |
| `StartedAt` | `DateTime` | UTC |
| `CompletedAt` | `DateTime?` | UTC |

**Relations:**
- Has many `TestAttemptAnswer`

---

### TestAttemptAnswer

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `AttemptId` | `Guid` | FK → TestAttempt |
| `QuestionId` | `Guid` | FK → Question |
| `SelectedOptionIds` | `Guid[]` | PostgreSQL array, for choice questions |
| `TextAnswer` | `string?` | For text input questions |
| `IsCorrect` | `bool` | Evaluated on submission |

---

### Enrollment

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `PaymentId` | `Guid?` | FK → Payment, null for free courses |
| `Status` | `EnrollmentStatus` (enum) | Active / Completed |
| `EnrolledAt` | `DateTime` | UTC |
| `CompletedAt` | `DateTime?` | UTC |

**Unique constraint:** `(UserId, CourseId)`

---

### LessonProgress

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `LessonId` | `Guid` | FK → Lesson |
| `IsCompleted` | `bool` | Default: false |
| `CompletedAt` | `DateTime?` | UTC |

**Unique constraint:** `(UserId, LessonId)`

---

### LessonLike

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
| `UserId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `EnrollmentId` | `Guid` | FK → Enrollment, Unique |
| `UniqueCode` | `string` | Public verification code, Unique |
| `PdfUrl` | `string` | Azure Blob URL |
| `IssuedAt` | `DateTime` | UTC |

---

### Achievement

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Key` | `string` | Unique code e.g. `first_lesson`, `complete_5_courses` |
| `Name` | `string` | Display name |
| `Description` | `string` | |
| `IconUrl` | `string?` | |
| `XpValue` | `int` | |

Seeded at startup. Not created by users or admins.

---

### UserAchievement

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `AchievementId` | `Guid` | FK → Achievement |
| `EarnedAt` | `DateTime` | UTC |

**Unique constraint:** `(UserId, AchievementId)`

---

### Payment

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

### Notification

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `Type` | `NotificationType` (enum) | NewMessage / AchievementEarned / EnrollmentConfirmed / CertificateReady / InstructorApproved / InstructorRejected |
| `Title` | `string` | |
| `Body` | `string` | |
| `IsRead` | `bool` | Default: false |
| `RelatedEntityId` | `Guid?` | Optional reference |
| `RelatedEntityType` | `string?` | e.g. `Course`, `Achievement` |
| `CreatedAt` | `DateTime` | UTC |

---

### Message
In-course chat between Student and Instructor.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `CourseId` | `Guid` | FK → Course |
| `SenderId` | `Guid` | FK → User |
| `ReceiverId` | `Guid` | FK → User |
| `Content` | `string` | Plain text, max 2000 chars |
| `IsRead` | `bool` | Default: false |
| `SentAt` | `DateTime` | UTC |

---

## Enums

```csharp
public enum UserRole          { Student, Instructor, Admin }
public enum ApplicationStatus { Pending, Approved, Rejected }
public enum CourseStatus      { Draft, Published, Archived }
public enum LessonType        { Video, Post, Test }
public enum QuestionType      { SingleChoice, MultipleChoice, TextInput }
public enum EnrollmentStatus  { Active, Completed }
public enum PaymentStatus     { Pending, Completed, Failed }
public enum NotificationType
{
    NewMessage,
    AchievementEarned,
    EnrollmentConfirmed,
    CertificateReady,
    InstructorApproved,
    InstructorRejected
}
```

---

## MongoDB Documents

---

### ChatSession
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

### CourseReview
Student review after completing a course.

```json
{
  "_id": "ObjectId",
  "userId": "Guid (string)",
  "courseId": "Guid (string)",
  "rating": 1-5,
  "reviewText": "string | null",
  "createdAt": "DateTime"
}
```

**Index:** `courseId`, `userId`  
**Unique:** `(userId, courseId)` — one review per student per course  
**Collection:** `course_reviews`

---

## Key Relations Summary

```
User ──< Enrollment >── Course
User ──< LessonProgress >── Lesson
User ──< LessonLike >── Lesson
User ──< TestAttempt >── TestLesson
User ──< UserAchievement >── Achievement
User ──< UserCategoryPreference >── Category
User ──< Message >── Course (with Receiver)
Course ──< Section ──< Lesson
TestLesson ──< Question ──< QuestionOption
Question ──── TextAnswerConfig
TestAttempt ──< TestAttemptAnswer
Enrollment ──── Certificate
Payment ──── Enrollment
```

---

## Notes

- `EnrollmentsCount` on `Course` is denormalized. Updated via `CourseEnrolledIntegrationEvent` consumer. Avoids COUNT query on every course listing.
- `Achievement` records are seeded — not managed at runtime. Checking logic lives in `CheckAchievementsConsumer`.
- `Tags` stored as PostgreSQL array (`string[]`). If filtering by tags becomes complex, consider separate `Tag` + `CourseTag` tables later.
- `Message` is simple polling-based for v1. Can be upgraded to SignalR without changing the entity.
