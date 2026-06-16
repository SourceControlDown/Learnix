# Learnix — Frontend Architecture Specification

> For detailed architectural decision records (ADRs) and rationale, refer to the [Documentation Index](#documentation-index).

## Overview

**Pattern:** Layer-based architecture with feature-sliced grouping inside layers  
**Framework:** React 19 + Vite  
**Routing:** React Router v7 (Nested layouts, lazy loading, Route Guards)  
**Type Safety:** TypeScript + Zod (Form schemas)  
**Server State:** TanStack Query (cache, refetch, mutations)  
**Client State:** Zustand (Auth, UI, Theme)  
**HTTP Client:** Axios (Interceptor-based token refresh)  
**Realtime:** SignalR (WebSockets with automatic reconnects)  
**Styling:** Tailwind CSS + shadcn/ui (CSS variables for theming)  

---

## Data Flow

```text
User Interaction
    ↓
Component (calls Custom Hook)
    ↓
Hook ─┬─→ [Server State] TanStack Query → Axios (API Calls)
      │
      └─→ [Client State] Zustand → Local UI/Auth State
    ↓
Backend
```

---

## Documentation Index

The detailed architectural decisions and rules are organized into specific topical documents. Please review them for the rationale behind our tech stack and patterns:

- [**Project Structure**](PROJECT_STRUCTURE.md) — Directory tree, file conventions, and component location rules.
- [**Architecture & Routing**](DECISIONS_ARCHITECTURE.md) — Layer-based structure, page co-location, router guards, and tooling.
- [**API & State Management**](DECISIONS_API.md) — Axios interceptors, React Query setup, SignalR real-time hubs, and state boundary.
- [**Authentication**](DECISIONS_AUTH.md) — In-memory tokens, HTTP-only refresh cookies, Google OAuth token flow.
- [**Forms & Error Handling**](DECISIONS_FORMS.md) — Zod schemas vs DTOs, 3-level error handling strategy.
- [**UI & Styling**](DECISIONS_UI.md) — Tailwind conventions, shadcn/ui integration, safe Markdown rendering.
- [**I18n & SEO**](DECISIONS_I18N_SEO.md) — react-i18next namespaces, react-helmet-async SEO strategy.
- [**Deployment**](DECISIONS_DEPLOYMENT.md) — Static hosting vs Node.js server, client-side routing fallback.

> **Note:** When introducing new architectural decisions or modules, please refer to [**DECISIONS_TEMPLATE.md**](DECISIONS_TEMPLATE.md) for formatting and numbering conventions.
