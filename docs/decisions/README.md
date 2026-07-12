# Repository-Wide Decision Records

Decisions that belong to **the repository as a whole**, not to the backend or the frontend
separately. If a decision would have to be written twice — once for each side — it lives here.

| Document | What it decides |
|---|---|
| [Repository & Workflow](REPOSITORY.md) | Monorepo layout, commit convention, where documentation lives |

Backend-specific decisions: [`docs/backend/decisions/`](../backend/decisions/README.md).
Frontend-specific decisions: [`docs/frontend/decisions/`](../frontend/decisions/README.md).

## Conventions

Numbering is scoped per file: `ADR-REPO-NNN`, restarting at `001` in each document — the same rule
the backend and frontend registers follow. Numbers are never reused; gaps mean an ADR was removed.
