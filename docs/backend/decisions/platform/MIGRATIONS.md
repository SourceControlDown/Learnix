# Learnix — ADR: Migrations & Seeding

> Format: what was decided → why → what alternatives were rejected.
> Updated after each chat where migration/seeding decisions were made.

Related files: [INFRA.md](INFRA.md) · [CICD.md](../operations/CICD.md) · [BLOB.md](BLOB.md)

## Status Convention

ADRs are not deleted. If a decision is reviewed — the old ADR is marked `Superseded by ADR-XXX`, the new one — `Supersedes ADR-YYY`. This preserves the history of thought and shows how the architecture evolved.

---

## ADR-BACK-MIGR-001: Migrations and seeding live in a standalone `Learnix.DbMigrator`

**Decision:** all schema migration and data seeding execution was extracted from `Learnix.API` and `Learnix.Infrastructure` into a standalone console application, `Learnix.DbMigrator`.

1. **The API never applies migrations — in any environment.** `Learnix.DbMigrator` calls `Database.MigrateAsync()`, and it is the only thing in the solution that does; `Learnix.Infrastructure` carries no migration-running code at all. Locally that is `docker compose --profile init up migrator` (or `dotnet run --project Learnix.DbMigrator`); in CI/CD it is a dedicated deploy step. `dotnet ef database update` is **not** the supported path.
2. **Seeders are plain classes implementing `IDataSeeder`** (`Learnix.DbMigrator/Seeders/`), run sequentially by the migrator. They are no longer `IHostedService` implementations inside `Learnix.Infrastructure`.
3. **Seed data is split by intent:** `System` seeders always run (Roles, Admin, Categories); `Demo` seeders (fake Courses and Students) run only behind the `--seed-demo` flag.
4. **The migrator flushes Redis when it finishes** — see ADR-BACK-INFRA-014 for why a cache must not outlive its database.

**Why:**
- **Scale-out safety.** Multiple API instances starting at once would run the startup seeders concurrently — race conditions, duplicate rows, unique-constraint violations. And auto-migration on boot means a schema error takes the API down instead of failing one deploy step.
- **Least privilege.** DDL rights (CREATE/DROP TABLE) belong to the migrator identity. The API runtime only needs DML.
- **A human review gate.** A destructive schema change should not reach production because a container restarted.
- **Startup cost.** The API boots without EF tooling, seeders or asset uploads in the path.
- **Environment control.** Production is deployed without demo content simply by omitting `--seed-demo`.

**Alternatives (this is where the previous design is recorded):**
- **Migrations in `Learnix.Infrastructure`, auto-applied by the API on startup in Development only.** This is what the project did before: an `ApplyMigrationsAsync()` extension over `IHost`, called from `Program.cs` behind `app.Environment.IsDevelopment()`, while staging and production applied migrations some other way. It bought a shorter dev loop — `docker compose up -d` + `dotnet run` and the schema was there. **Rejected**, and the extension has since been deleted: it kept two different mechanisms alive for the same job, so the path a developer exercised every day was never the path production took. A migration that only fails under the CI/CD mechanism is a migration nobody tested. One path in every environment is worth the extra `docker compose --profile init up migrator` step.
- **Seeders as `IHostedService` inside `Learnix.Infrastructure`.** Also the previous design (`CourseSeederHostedService`, `StudentSeederHostedService`). **Rejected:** on scale-out every API replica runs them at once — duplicate rows and unique-constraint violations — and it forces the API to carry seed assets and blob-upload code it never uses at runtime.
- **Always auto-migrate, in every environment.** **Rejected** — the same scale-out race, plus a schema error takes the API down instead of failing one deploy step, and destructive changes reach production without a review gate.
- **`dotnet ef database update` in CI.** Requires EF tooling on the runner and cannot seed.
- **An idempotent SQL script (`dotnet ef migrations script --idempotent`) applied with `psql`.** A legitimate approach, but it needs `psql` on the runner and an artifact to manage. More moving parts for no gain here.
- **`Database.EnsureCreatedAsync()`.** Incompatible with migrations; only usable for throwaway test databases.

**Consequences:**
- A new project to maintain in the solution, plus a small amount of duplicated DI wiring for `ApplicationDbContext`.
- After a `git pull` that brings a new migration, the database is **not** updated by running the API. The migrator has to be run.
- `Learnix.Infrastructure` owns the migration *files* (`Persistence/EntityFramework/Migrations/`, the `--output-dir` of `dotnet ef migrations add`) but nothing that applies them.

---

## ADR-BACK-MIGR-002: Seed assets are embedded resources in the migrator, and every seeded entity gets its own blob

**Decision:** the images and videos needed to seed courses, lessons and avatars are embedded in the **`Learnix.DbMigrator`** assembly (`Learnix.DbMigrator/Assets/`, declared as `<EmbeddedResource>` in the `.csproj`, read via `Assembly.GetExecutingAssembly().GetManifestResourceStream("Learnix.DbMigrator.Assets.{name}")`). On upload, **each** seeded course or video lesson receives its own copy in Blob Storage under a unique `blobPath`.

**Why:**
- **Environment independence.** The seeder does not depend on the host file system or the current working directory — both of which are unreliable in Docker and in tests.
- **The assets belong with the seeders.** They exist for one purpose, and that purpose left `Learnix.Infrastructure` along with the seeders (ADR-BACK-MIGR-001).
- **A shared blob is a deletion hazard.** If every seeded lesson pointed at one `placeholder.mp4`, deleting a video in a single lesson would enqueue an Outbox `DeleteBlob` (ADR-BACK-BLOB-003) and destroy the file every other lesson is still serving. Unique copies make deletion safe.

**Alternatives:**
- **The same embedded resources, but in `Learnix.Infrastructure`.** The previous design, read by `CourseSeederHostedService` / `StudentSeederHostedService`. **Rejected** for the same reason the seeders themselves moved (ADR-BACK-MIGR-001): the API would keep shipping several megabytes of course covers and a placeholder video it never opens.
- **Base64 constants in C# (the design before that).** **Rejected:** enormous literals, unreadable files.
- **Reading from the file system (`File.ReadAllBytes`).** **Rejected:** depends on `Copy to Output Directory` and on the working directory; breaks in a container.
- **One shared blob per asset type.** **Rejected** — see the deletion hazard above.

**Consequences:**
- Adding a new seed asset means dropping the file into `Learnix.DbMigrator/Assets/` and registering it as an `<EmbeddedResource>` (the `.csproj` globs `*.png`, `*.webp`, `*.mp4`).
- Seeding uploads N copies of the placeholder video rather than one. Storage in the demo environment is cheap; a shared-blob 404 is not.
