# Learnix — ADR: Domain Model

> Format: what was decided → why → what alternatives were rejected.

Related files: [ARCHITECTURE.md](ARCHITECTURE.md) · [AUTH.md](AUTH.md) · [INFRA.md](INFRA.md)

The records are ordered the way the model is built up: first what an entity *is*, then how it refuses
invalid state, then the aggregate and how it is loaded, then the lifecycle it moves through, and only
then the individual modelling choices. Read from the top if you are new to the domain.

> **This file was renumbered once**, during the audit that reordered it — the one exception to
> "numbers are never reused". It was safe because nothing outside this file referenced a
> `ADR-BACK-DOMAIN-NNN` id. Two records left: the `CourseCommandHandler` base class was an Application
> decision, not a domain one (now [ADR-BACK-ARCH-019](ARCHITECTURE.md)), and the domain-event
> interceptor is Infrastructure (now [ADR-BACK-INFRA-015](INFRA.md)). The constants ADR was dropped as
> a duplicate of [ADR-BACK-ARCH-018](ARCHITECTURE.md). From here on the numbers are stable.

---

## ADR-BACK-DOMAIN-001: `BaseEntity` split into `IAuditable` + `IHasDomainEvents`

**Decision:** `BaseEntity` is an abstract class providing `Id : Guid` and implementing two interfaces:
`IAuditable` (`CreatedAt`, `UpdatedAt`) and `IHasDomainEvents` (`DomainEvents`, `ClearDomainEvents`).
`User : IdentityUser<Guid>` cannot inherit `BaseEntity` — both would supply `Id` — so it implements
the two interfaces directly.

**Why:**
- The interceptors key off the *interfaces*, not the base class: `AuditableInterceptor` stamps anything
  that is `IAuditable`, `DomainEventsInterceptor` dispatches from anything that is `IHasDomainEvents`.
  One mechanism covers both `BaseEntity` descendants and `User`.
- The alternative — duplicating the audit fields and the event list inside `User` — means every new
  audit field has to be added in two places, and the day someone forgets is the day `User` stops being
  audited silently.

**Alternatives:**
- **`User` without audit and events** — we would lose the creation/update trail on the one entity whose
  history matters most.

---

## ADR-BACK-DOMAIN-002: Private setters, state changes only through business methods

**Decision:** every entity property has a `private set`. State changes go through methods named after
business operations.

**Rules:**
- One method = one business action (`course.UpdateDetails()`, `course.Publish()`).
- **Not** a setter per field. `SetTitle` / `SetPrice` are the anemic model wearing a hat.
- A bulk `Update(...)` taking every editable field at once is fine — it is still one business action.

**Why:**
- An invariant can only be enforced in one place if there is only one way in. A public setter is a
  second way in, and it does not check anything.
- The entity carries behaviour, so the rules live next to the state they constrain rather than being
  scattered across handlers.

---

## ADR-BACK-DOMAIN-003: `DomainException` for invariant violations — never `InvalidOperationException`

**Decision:** invariant checks in entities throw `DomainException` (`Learnix.Domain.Common.Exceptions`).
Handlers that mutate an aggregate catch **that** exception and turn it into `ConflictError` → 409.

**Why:**
- Catching `InvalidOperationException` in the Application layer would be catching the framework: a
  `.First()` on an empty sequence, an EF Core failure, and a deliberate business rule violation all
  arrive as the same type. Two of those are bugs and must produce a 500, not a 409.
- A dedicated exception type *is* the contract: if it reaches the handler, a business rule said no.

**Alternatives:**
- **Return `Result` from domain methods** — rejected: the domain would then depend on FluentResults, a
  control-flow library, for the sake of a case that is exceptional by definition.

**Consequences:**
- Everything else propagates to `ExceptionHandlingMiddleware` and becomes a 500. That is intended:
  an unexpected exception is not a business outcome.

---

## ADR-BACK-DOMAIN-004: `Course` is the aggregate root for structure

**Decision:** every structural mutation — create/update/delete/reorder of sections and lessons — goes
through a public method on `Course`. `Section` and `Lesson` mutators are `internal`, reachable only
from inside the Domain assembly, i.e. only from `Course`.

The handler shape for any structure mutation:

1. Load `Course` with the structure it needs (ADR-BACK-DOMAIN-005), tracked.
2. Owner-or-admin check.
3. Call the domain method (`course.AddSection(...)`, `course.RemoveLesson(...)`).
4. `SaveChangesAsync()`.
5. `DomainException` → `ConflictError` (ADR-BACK-DOMAIN-003).

Steps 1–2 are not written by hand in each handler — they are the base class `CourseCommandHandler`
(ADR-BACK-ARCH-019).

**Why:**
- The publish invariants (ADR-BACK-DOMAIN-007) are statements about the *whole* course. Checking them
  after a mutation requires the in-memory state of the structure, which only the root can see.
- Section and Lesson have no life of their own: a lesson outside a course is meaningless.

**Alternatives:**
- **Section/Lesson as separate aggregates.** Simpler create/update — but deleting a lesson from a
  Published course would still have to load the Course to re-check the invariant, so the invariant
  logic would live in the handler as well as the domain. Two paths, one of which is easy to forget.

**Consequences:**
- `Course` is a large entity (~15 mutating methods). That is the shape of a rich model, not a smell.
- `ILessonRepository` and `ISectionRepository` **do** exist — but only for persistence of a mutation
  the root has already authorized and validated (see ADR-BACK-DOMAIN-005). They are not a second door
  into the aggregate.

---

## ADR-BACK-DOMAIN-005: Aggregate loading — full for invariants, point-loaded for content

**Decision:** how much of the aggregate a handler loads depends on whether the operation can break a
lifecycle invariant.

**Full load** (`includeLessons: true`) — the operation can violate `EnsurePublishableInvariants()`:

| Operation | Why |
|---|---|
| `Publish` | Checks cover + ≥1 section + ≥1 **visible** lesson |
| `DeleteLesson` | Can leave the course with no visible lesson |
| `ToggleLessonVisibility` | Hiding the last visible lesson breaks the invariant |
| `RemoveSection` | Can leave the course with no sections or no lessons |

**Point load** — content edits that cannot change the course's lifecycle state (`UpdateVideoLesson`,
`UpdatePostLesson`, `UpdateTestLesson`). The aggregate is loaded **only** to authorize the caller and
to check `EnsureStructureMutable()`; the lesson itself is then fetched and saved through
`ILessonRepository`. `CourseSectionCommandHandler` adds a third mode, `lessonsBySectionId`, which
loads only the targeted section's lessons — enough to validate a section-scoped operation without
dragging in the rest of the course.

**Why:**
- Canonical DDD says every mutation goes through the root. Loading 10 sections × 50 lessons to change
  a video's `DurationSeconds` is the letter of that rule with none of its value.
- Safety does not depend on the loading mode: `CourseCommandHandler` runs `IsOwnerOrAdmin` and
  `EnsureStructureMutable()` before any handler body executes, whatever was loaded.

**Alternatives:**
- **Always load everything** — safe, and pointless: ~500 rows read to update one text field.
- **Denormalize `CourseId` onto `Lesson`** to skip the join — does not help: `InstructorId` still lives
  on `Course`, so the course must be loaded anyway to answer "may this user touch it?".

**Consequences:**
- `DeleteLesson` and `ToggleLessonVisibility` pass `includeLessons: true` — and they must. Both once
  defaulted to `false`, and `EnsurePublishableInvariants()` reading an unloaded (therefore empty)
  `Lessons` collection would have thrown *"must have at least one visible lesson"* on a course with
  fifty of them. The bug never fired only because those handlers did not call the invariant at all —
  an accident, not a design. This is why the loading mode is part of the decision and not an
  optimization detail.
- A new handler that can delete or hide lessons is a full load. A new handler that only edits content
  is a point load. This is a convention, not something the compiler enforces — check it in review.

---

## ADR-BACK-DOMAIN-006: Course lifecycle — three states, invariants gate Publish

**Decision:** `Course` has three states, plus soft-deleted:

- **Draft** — on create; editable; visible only to the owner and Admin.
- **Published** — visible to everyone, open for enrollment.
- **Archived** — visible to owner and Admin, read-only, closed for enrollment.
- soft-deleted — visible only via `IgnoreQueryFilters` (ADR-BACK-DOMAIN-010).

Transitions: `Create → Draft`; `Draft → Published` (`Publish()`, invariants checked);
`Published → Draft` (`Unpublish()`); `any → Archived` (`Archive()`); `any → soft-deleted` (`Delete()`,
even with active enrollments).

**Publish invariants** — exactly what `Course.EnsurePublishableInvariants()` enforces:

1. `CoverBlobPath` is set (the entity stores the blob path; the DTO exposes it as `coverImageUrl`).
2. At least one section.
3. At least one **visible** lesson across all sections (`!l.IsHidden`).

**Why:**
- Without them, empty courses reach the catalog. That is a platform-quality problem, not a user error.
- A cover is optional in Draft and mandatory on Publish: the instructor can write the content first and
  attach the artwork later.
- Archive is "take it out of the catalog, keep it for the owner" — there is nothing to validate.

**Alternatives:**
- **Publish without invariants** — the catalog fills with empty shells.
- **Invariants as DB CHECK constraints** — "at least one visible lesson across all sections" is not
  expressible without triggers.

---

## ADR-BACK-DOMAIN-007: Publish invariants hold continuously, not just at Publish

**Decision:** while `Status == Published`, the three invariants must hold *at all times*. They are
re-checked after every mutation that could break them, not only during the Draft → Published
transition:

- `SetCoverImage(null)` on a Published course → throw.
- `RemoveSection(...)` that leaves zero sections, or leaves no visible lesson → throw.
- `RemoveLesson(...)` / hiding a lesson that leaves no visible lesson → throw.

Archived is fully read-only (`EnsureStructureMutable()` rejects every structure mutation). Draft
allows anything.

**Why:**
- The instructor can keep working on a Published course without an Unpublish → edit → Publish dance,
  and the course still cannot become an empty shell.

**Alternatives:**
- **Structure frozen while Published** — one invariant fewer, considerably worse UX.
- **Additive only** (adds allowed, deletes not) — a half-rule that would have to be enforced in every
  delete handler individually; a continuous invariant in the entity is less code and harder to forget.

**Consequences:**
- `EnsurePublishableInvariants()` is private and called from `SetCoverImage`, `RemoveSection`,
  `RemoveLesson` and `Publish`.
- When it throws, the in-memory entity may already be modified — but `SaveChangesAsync` is never
  reached, the DbContext is scoped per request, and nothing is written.
- **Convention:** any new mutation that could touch cover, sections or lesson visibility must call it.

---

## ADR-BACK-DOMAIN-008: `Question`, `QuestionOption`, `TextAnswerConfig` are value objects in JSONB

**Decision:** these three, plus `StudentAnswer`, are value objects persisted as JSONB — `OwnsMany` /
`OwnsOne` with `ToJson()` — inside `TestLesson` and `TestAttempt`. They have no tables of their own.

**Why:**
- None of them has a lifecycle independent of its owner. A question without its test is meaningless,
  an option without its question likewise, an answer belongs to exactly one attempt and is never reused.
- Questions are replaced wholesale (`ReplaceQuestions`), never patched one at a time — which is exactly
  what a JSON document does well and what a relational child table does badly (delete-all + insert-all
  in a transaction, plus cascade rules and ordering columns).

**Scoring lives with the data:** `Question.IsAnsweredCorrectly(StudentAnswer)` holds the full scoring
rule, including the fuzzy text match (ADR-BACK-DOMAIN-014).

**Alternatives:**
- **Separate tables** — join-heavy reads for something always fetched as a whole, and a bulk replace
  that becomes a small transaction script.

**Consequences:**
- Adding a field to a question is a JSON shape change, not a migration — backward compatible as long as
  it is nullable.

---

## ADR-BACK-DOMAIN-009: Reorder is a bulk operation with full set equality

**Decision:** reordering is its own endpoint (`.../sections/reorder`, `.../lessons/reorder`) taking an
array of `{ id, order }`. The domain requires the payload to contain **exactly** the existing set — no
more, no fewer. The validator checks shape (non-empty, ≤500 sections / ≤1000 lessons, unique ids,
`order >= 0`); the domain checks set equality via `ReorderValidation.EnsureValid`.

**Why:**
- **Atomicity.** One transaction. N individual `PATCH /order` calls pass through states where two
  siblings share an order, and keeping that consistent needs locks nobody wants to write.
- **A complete snapshot is simpler than a diff.** "This is the order it should be in" beats "shift these
  three and hope".
- Set equality is an aggregate invariant, not a shape check — hence the domain, not the validator.

**Alternatives:**
- **`PATCH` an individual order** — collisions, and a lock to resolve them.
- **Fractional indexing (Lexorank)** — avoids rewriting every row on insert; overkill for an LMS where
  reorder is an explicit, occasional action, not a continuous drag.

---

## ADR-BACK-DOMAIN-010: Soft delete for `User` and `Course`, hard delete for the rest

**Decision:**
- `User` — soft delete, then **anonymization** after a 30-day recovery window
  (`UserConstants.AccountRecoveryWindowDays`, executed by `DeletedAccountPurgeService`).
- `Course` — soft delete, or Archive when the owner just wants it out of the catalog. A soft-deleted
  course stays in the database indefinitely; nothing purges it.
- Everything else (`LessonProgress`, wishlist rows, …) — hard delete.

`ISoftDeletable` gives `IsDeleted` + `DeletedAt`; a global EF query filter hides the rows;
`SoftDeleteInterceptor` turns a `Remove()` into a flag update.

**Why:**
- Deleting a user for real is not possible today and possibly never should be: reviews, conversations
  and payments reference them. Anonymization keeps the deletion promise for the personal data while the
  records that other people depend on survive. The full reasoning, and the FK graph that forces it,
  is in `ADR-BACK-USERS-001` and TD-001.
- A course carries enrollments, progress and certificates. Erasing it would erase somebody else's
  history of having learned from it.

**Consequences:**
- "Deleted" means two different things for a user (recoverable for 30 days, then anonymized) and for a
  course (recoverable indefinitely, by an admin). That asymmetry is deliberate — and it is the reason
  no background job purges courses.

---

## ADR-BACK-DOMAIN-011: `EnrollmentsCount` is denormalized on `Course`

**Decision:** `Course.EnrollmentsCount` is a column, incremented by the domain method
`IncrementEnrollmentsCount()` in the same transaction as the enrollment that caused it — from
`EnrollInCourse` (free courses) and from `InitiateMockPayment` (paid ones).

**Why:**
- The catalog sorts and filters by popularity on its hottest path. `COUNT(*)` over `Enrollments` per
  course per page render is the wrong shape of query for that.
- Incrementing inside the same `SaveChangesAsync` as the `Enrollment` insert means the counter cannot
  drift from the rows it counts: either both land or neither does.

**Alternatives:**
- **`COUNT(*)` on read** — correct by construction, and a join on every catalog page.
- **A nightly recount job** — simple, but the counter is wrong for up to 24 hours right after the event
  a user is most likely to look at.
- **Async update via an integration event** — eventual consistency where none is needed: the user who
  just enrolled would see the old number.

**Consequences:**
- Un-enrolling is not supported, so there is no decrement. If it is ever added, it must decrement in the
  same transaction — and the seeder (`StudentSeeder`) has to keep doing the same.

---

## ADR-BACK-DOMAIN-012: `IsFree` is computed, not stored

**Decision:** `Course` stores `Price : decimal`. "Free" means `Price == 0`. There is no `IsFree` column;
the DTO exposes a computed `IsFree => Price == 0m`.

**Why:**
- Two fields that must agree will eventually disagree — `Price = 10, IsFree = true` is a one-line bug
  and a support nightmare. One source of truth cannot desynchronize with itself.

**Alternatives:**
- **An `IsFree` column** — buys a marginally cheaper filter (an index on `Price` gets the same result)
  and guarantees a class of bug.
- **A computed column in PostgreSQL** — available if the free-courses filter ever becomes hot. It is not.

---

## ADR-BACK-DOMAIN-013: `Category.IsSystem` protects seeded categories

**Decision:** `Category.IsSystem : bool`. The seeder (`CategorySeeder` in `Learnix.DbMigrator`) creates
its categories with `IsSystem = true`. `Category.Rename` refuses to rename them, and
`DeleteCategoryCommandHandler` refuses to delete them.

**Why:**
- Seeded categories are platform data that courses are attached to. Hiding the delete button in the
  admin UI is not protection — a `DELETE` via curl would still go through.
- One flag on the entity means one place to check, rather than a rule re-implemented in the UI and in
  the API.

**Alternatives:**
- **A hardcoded list of protected slugs** — works until the seeder and the list disagree.
- **A separate `SystemCategories` table** — a table for one bit.

**Consequences:**
- A user-created category whose slug is later added to the seeder stays `IsSystem = false` (the seeder
  skips duplicates). Promoting it is a manual `UPDATE`.

---

## ADR-BACK-DOMAIN-014: Fuzzy text answers — threshold by expected-answer length

**Decision:** `Question.IsFuzzyMatch` derives its Levenshtein threshold from the length of the
**expected** answer: `<= 2` chars → `0` edits, `<= 5` → `1`, longer → `2`. Both strings are always
`Trim()`-ed, regardless of `IgnoreCase`.

**Why these thresholds:**
- `<= 2 → 0`: one edit turns `"C#"` into `"C"` and `"Go"` into `"Do"`. At that length any tolerance
  changes the answer.
- `3–5 → 1`: one typo forgiven. (`"cat"`/`"bat"` is the boundary; `"bet"` is not.)
- `> 5 → 2`: long answers can absorb two typos and still be unmistakable — `"JavaScrip"` for
  `"JavaScript"`.

**What this replaced** — the previous line was `b.Length > 2 ? 0 : (b.Length <= 5 ? 1 : 2)`, which got
it wrong in both directions at once: everything longer than two characters demanded an exact match (so
`AllowFuzzy` did nothing for any real answer), two-character answers got a *whole* edit of tolerance
(`"C"` passed for `"C#"`), and the `2` branch was unreachable. The comment above it described the
opposite of what it did, and a `#pragma warning disable S3358` kept the nested ternary out of review.
`Trim()` living inside `if (IgnoreCase)` meant `"Paris "` was wrong when case-sensitivity was on.

**Consequences:**
- Grading changed for existing courses with `AllowFuzzy` on: they used to be graded as exact matches,
  and now behave the way the UI has always promised ("Allow typos").
- `GetPlatformInfoTool` used to tell the AI tutor "1 character error" — it now describes the real
  thresholds.
- `Learnix.Domain.UnitTests/ValueObjects/QuestionTests.cs` pins all three levels at their boundaries.

**Alternatives:**
- **Always 1 edit** — matches the old marketing copy, serves long answers badly.
- **Proportional (`length / 5`)** — smoother, but stricter than today for 3–4 character answers and a
  behaviour nobody asked for.
