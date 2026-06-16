# Learnix — Frontend Project Structure

This document outlines the standard directory layout and component placement conventions for the Learnix client application.

## High-Level Folder Structure

```
src/
├── api/                          # Axios instance + endpoint modules
│   ├── axios.instance.ts         # Base instance, interceptors, queued refresh
│   ├── queryKeys.ts              # Hierarchical React Query keys
│   └── *.api.ts                  # Domain-specific API wrappers
│
├── assets/
│   ├── images/
│   ├── icons/
│   └── fonts/
│
├── components/
│   ├── ui/                       # shadcn/ui primitives (generated, do not edit manually)
│   ├── common/                   # Reusable custom components (used on 2+ pages)
│   └── layout/                   # Layout components (Header, Footer, Sidebar, etc.)
│
├── pages/
│   ├── public/                   # No auth required (Landing, CourseCatalog, Login)
│   ├── student/                  # Role: Student (MyLearning, CoursePlayer)
│   ├── instructor/               # Role: Instructor (Dashboard, CourseEditor)
│   └── admin/                    # Role: Admin (UserManagement, PlatformLogs)
│
├── hooks/                        # Custom React hooks (useAuth, query hooks, hub hooks)
├── store/                        # Zustand stores (auth.store, ui.store, etc.)
├── i18n/                         # react-i18next configuration and JSON namespaces
├── const/                        # Constants (limits, hardcoded values)
├── schemas/                      # Zod schemas (source of truth for FormValues)
├── types/                        # TypeScript types (DTOs, enums, mapped to backend)
├── utils/                        # Pure utility functions (cn.ts, formatters, etc.)
├── routes/                       # React Router v7 configuration (index.tsx, guards)
├── styles/                       # index.css (Tailwind base + CSS variables)
├── App.tsx                       # RouterProvider mount point
└── main.tsx                      # ReactDOM + QueryClient + Global Providers
```

## Component Organization Rules

1. **`components/ui/`** — Tightly controlled directory for shadcn/ui primitives. Generated via `npx shadcn-ui add <component>`. Avoid creating ad-hoc files here.
2. **`components/common/`** — Shared components that are used across **2 or more pages** (e.g., `CourseCard`, `Pagination`).
3. **`components/layout/`** — Broad structure components (e.g., `Header`, `PublicLayout`).
4. **Ad-hoc Components (Page-Level Co-location):**
   - **1-3 helper components:** Keep them as flat files next to the page component.
   - **4+ helper components:** Group them in a `components/` subfolder inside the page directory.
5. **Migration Rule:** When a page-level component is needed on a second page, move it to `components/common/`.
6. **Constants (`const/`):** Extract all magic numbers (e.g., max lengths, default page sizes) into `*.constants.ts` files instead of hardcoding them in Zod schemas or UI elements.

## File Naming Conventions

| Item | Convention | Example |
|---|---|---|
| React Component | PascalCase | `CourseCard.tsx` |
| React Hook | camelCase, `use` prefix | `useAuth.ts` |
| Zustand Store | camelCase, `.store.ts` suffix | `auth.store.ts` |
| API Module | camelCase, `.api.ts` suffix | `courses.api.ts` |
| Zod Schema | camelCase, `.schema.ts` suffix | `course.schema.ts` |
| Types / DTOs | camelCase, `.types.ts` suffix | `course.types.ts` |
| Utility Function | camelCase | `formatDate.ts` |
