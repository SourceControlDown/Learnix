# Learnix — ADR: LMS Core Functionality

> Covers the design of the core learning platform (Course, Section, Lesson, Enrollment, TestAttempt).

> **Endpoints:** see [`docs/backend/ENDPOINTS.md`](../../ENDPOINTS.md) — one generated table for
> the whole API, verified against the controllers in CI. An ADR records a decision; it is not the
> place to keep a copy of the API surface.

---
## ADR-BACK-LMS-001: Course as the Single Aggregate Root for Structure

**Decision:** The `Course` entity is the Aggregate Root for `Section` and `Lesson`. All operations that modify the course structure (adding/removing sections or lessons, changing their order) are performed exclusively through public methods on the `Course` class (e.g., `Course.AddSection()`, `Course.RemoveLesson()`). Mutation methods inside `Section` and `Lesson` are marked as `internal` and are only accessible within the Domain assembly.

**Why:**
- **Protecting Publish Invariants:** A published course has strict rules (must have a cover image, at least one section, and at least one visible lesson). If sections/lessons could be edited directly via a repository, one might accidentally delete the last lesson of a published course and break the system. When everything flows through `Course`, the aggregate checks these rules on every action.
- **Single Source of Truth for Updates:** Structural changes automatically update the course's `UpdatedAt` timestamp.
- **Simplified Command Handlers:** The Application layer operates on a single pattern: fetch course → call method → save. A base `CourseCommandHandler` encapsulates this logic.

**Alternatives considered:**
- **Separate Repositories for Section and Lesson:** Allows for faster targeted updates, but completely destroys course consistency. Rejected in accordance with Domain-Driven Design (DDD) principles.

---

## ADR-BACK-LMS-002: Table Per Hierarchy (TPH) for Lesson Types

**Decision:** Various lesson types (`VideoLesson`, `PostLesson`, `TestLesson`) inherit from the abstract base class `Lesson` and are stored in a single database table `Lessons` using the TPH (Table Per Hierarchy) pattern. EF Core automatically uses the discriminator column `LessonType`.

**Why:**
- **Read Efficiency:** When loading the course structure (all sections and lessons), EF Core only makes one `JOIN` to the `Lessons` table, regardless of how many lesson types exist.
- **Shared Data:** All lessons have many common properties (`Title`, `Order`, `SectionId`, `IsFreePreview`, `CreatedAt`), which perfectly map to a single table. Type-specific fields (e.g., `VideoBlobPath` or `TestConfiguration`) simply remain `NULL` for other types.

**Alternatives considered:**
- **Table Per Type (TPT):** Storing base data in `Lessons` and specific data in `VideoLessons`, `TestLessons` tables. Rejected due to the `N+1 JOIN` problem when reading the full list of a course's lessons, which is critical for API performance.
- **Separate Entities without Inheritance:** Destroys the global sort order (`Order`) within a section. Rejected.

---

## ADR-BACK-LMS-003: Value Objects as JSONB for the Testing System

**Decision:** Test structures (`Question`, `QuestionOption`, `TextAnswerConfig`) and student answers (`StudentAnswer`) are designed as **Value Objects** and stored in the PostgreSQL database in **JSONB** format (a column in the `Lessons` table for configuration, and a column in the `TestAttempts` table for answers). They do not have their own surrogate Primary Keys (IDs) and separate SQL tables.

**Why:**
- **Document-oriented Nature of Tests:** Questions and answers perfectly fit into a JSON structure. There is no need to perform SQL `JOIN`s or search for a specific answer option in tests. A test is always loaded and validated as a whole.
- **Avoiding Table Bloat:** If questions and options were separate tables, it would create 4 new tables and a huge number of unnecessary relationships. JSONB keeps the DB schema clean.
- **Encapsulated Logic:** Test validation logic is encapsulated in the domain: `TestLesson.CalculateScore(List<StudentAnswer>)`.

**Alternatives considered:**
- **Full Relational Model (Questions, Options, Answers):** Correct from a classical DB perspective, but overly complex for a platform where tests are just a part of the content (a read-heavy scenario). Rejected.
- **Storing in MongoDB:** Since MongoDB is already configured (for chats and reviews), tests could be stored there. However, tests are tied to the course creation transactions in PostgreSQL. Storing the test structure in Mongo would break the data integrity (ACID) of the course.

---

## ADR-BACK-LMS-004: Idempotent Progress Tracking (LessonProgress)

**Decision:** Learning progress is recorded at the individual lesson level by creating a record in the `LessonProgress` table. The overall course completion status (`Enrollment.Status = Completed`) is not a separate user action, but is calculated and set automatically (via Domain Events) the moment a user completes the last required lesson of the course.

**Why:**
- **Atomicity:** A student completes a lesson → `LessonProgress` is saved → `LessonCompletedDomainEvent` is generated → the system checks if there are any remaining uncompleted lessons in this `Enrollment`.
- **Idempotency:** A duplicate request to complete a lesson is simply ignored (the `LessonProgress` record already exists), protecting against network retries.
- **Flexibility for Course Updates:** If an instructor adds a new lesson to an already published course, the completion percentage for existing students (who have completed, for example, 10/10) automatically adapts (becomes 10/11), and the system handles this without complex `Enrollment` status migrations.

**Alternatives considered:**
- **Storing an Array of Completed Lessons as JSONB in Enrollment:** Simplifies the database but complicates aggregation queries (e.g., "how many students completed this specific lesson"). A separate `LessonProgress` table is much more effective for analytics.
