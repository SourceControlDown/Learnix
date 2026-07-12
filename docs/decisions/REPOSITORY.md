# Learnix — ADR: Repository & Workflow

> Format: what was decided → why → what alternatives were rejected.

Related files: [backend decisions](../backend/decisions/README.md) · [frontend decisions](../frontend/decisions/README.md)

---

## ADR-REPO-001: Monorepo — frontend and backend in one repository

**Decision:** one repository holding both applications: `Learnix.Backend/` (the .NET solution) and
`learnix-client/` (the React SPA), alongside `infrastructure/` (Terraform) and `docs/`.

**Why:**
- **One release cycle.** A feature is rarely backend-only or frontend-only. In one repo a feature is
  one pull request that can be reviewed, tested and reverted as a unit; across two repos it is two
  PRs with an implicit merge order and a window in which `main` is inconsistent with itself.
- **Shared tooling.** One `docker-compose.yml` brings up the whole system; one CI workflow builds,
  tests and lints both sides and can skip the half that did not change.
- **Contract changes are visible.** A change to a DTO and the change to the TypeScript interface that
  consumes it show up in the same diff. Split repos hide exactly this class of break until deploy.
- **It is a portfolio project.** One link is the whole system.

**Alternatives:**
- **Two repositories.** The right call when two teams ship on different cadences and own their own
  release trains — neither is true here. It would buy independent versioning at the price of
  cross-repo coordination for every feature.
- **A monorepo with a shared package for API types** (generated client). Genuinely tempting, and it
  would remove the hand-written TypeScript DTOs — but it adds a codegen step and a build order to a
  project that currently has neither. Worth revisiting if the DTOs start drifting.

**Consequences:**
- CI has to know what changed: `.github/workflows/checks.yml` and `deploy.yml` skip the jobs for
  packages a PR does not touch.
- The repository root carries its own `package.json` for cross-cutting checks (duplication, secret
  scanning, the API-surface check) that belong to neither side.
