# Learnix — Frontend Architecture Decision Records (Architecture)

> Формат: що вирішили → чому → які альтернативи відкинули.
> Архітектурні рішення беку — в [DECISIONS.md](./DECISIONS.md).

---

## FADR-ARCH-001: Layer-based структура з feature-розбивкою всередині шарів

**Рішення:** `src/` організовано по шарах (api, components, pages, hooks, store, schemas, types, utils). Feature-розбивка живе всередині кожного шару (наприклад `api/courses.api.ts`, `schemas/course.schema.ts`).

**Структура верхнього рівня:**
```
src/
├── api/             # Axios instance + endpoint modules
├── components/
│   ├── ui/          # shadcn/ui primitives
│   ├── common/      # Reusable custom components (2+ pages)
│   └── layout/      # Layout components (Header, Sidebar, Footer, *Layout)
├── pages/
│   ├── public/      # No auth required
│   ├── student/     # Role: Student
│   ├── instructor/  # Role: Instructor
│   └── admin/       # Role: Admin
├── hooks/           # Custom React hooks
├── store/           # Zustand stores
├── schemas/         # Zod schemas
├── types/           # TypeScript types (DTOs, enums)
├── utils/           # Pure utilities
├── routes/          # React Router config
└── styles/          # global.css with Tailwind + CSS variables
```

**Чому:**
- Layer-based простіший для старту — зрозумілий звідкись переносимо з React tutorials
- Feature-all-the-way (як `features/courses/{api,components,hooks}/`) краще масштабується, але для LMS з 20-30 фічами layer-based ще нормально
- Page-level компоненти живуть з самою сторінкою → не засмічують `components/common/`

**Альтернативи:**
- Pure feature-based (`features/courses/`, `features/auth/`) — краще для 50+ фіч, зайвий overhead для v1
- Pure layer-based без page-level co-location — `components/` розростеться до 100+ файлів

**Наслідки:**
- Конвенція: компонент використовується на 2+ сторінках → `components/common/`. Тільки на одній → лежить поруч зі сторінкою.

---

## FADR-ARCH-002: Page co-location + правила ad-hoc компонентів

**Рішення:**
- Сторінки групуються за роллю: `pages/public/`, `pages/student/`, `pages/instructor/`, `pages/admin/`
- Кожна сторінка — окрема папка (наприклад `pages/student/CoursePlayer/`)
- Ad-hoc компоненти сторінки живуть поруч з нею:
  - **1-3 допоміжних компоненти:** flat файли в папці сторінки
  - **4+ допоміжних компоненти:** у підпапці `components/` всередині сторінки

**Приклад (few components — flat):**
```
pages/public/Landing/
├── LandingPage.tsx
├── HeroSection.tsx
├── FeaturedCourses.tsx
└── HowItWorks.tsx
```

**Приклад (many components — subfolder):**
```
pages/instructor/CourseEditor/
├── CourseEditorPage.tsx
└── components/
    ├── CourseInfoForm.tsx
    ├── SectionManager.tsx
    ├── LessonEditor.tsx
    ├── QuestionEditor.tsx
    ├── DragDropList.tsx
    └── VideoUploader.tsx
```

**Чому:**
- Ad-hoc компоненти не забруднюють `components/common/`
- Близькість до сторінки → легше знайти, легше видалити разом зі сторінкою
- Правило "1-3 flat / 4+ subfolder" тримає обидва випадки чистими

**Правило міграції:** Коли ad-hoc компонент починає використовуватись на другій сторінці — переноситься в `components/common/`.

---

## FADR-ARCH-003: Роутинг — React Router v6 з nested layouts та route guards

**Рішення:**
- React Router v6 з `createBrowserRouter`
- Route guards реалізовані як компонент `<RequireRole />`, що огортає елементи маршутів або лейаути.
- Lazy loading для всіх сторінок — кожна сторінка завантажується при переході

**Структура роутера:**
```tsx
// src/routes/index.tsx
const guardStudent = (el: React.ReactElement) => (
    <RequireRole roles={['Student', 'Instructor', 'Admin']}>{el}</RequireRole>
);

export const router = createBrowserRouter([
  {
    element: <PublicLayout />,
    children: [
       ...publicRoutes,
       {
           path: '/profile',
           element: guardStudent(wrap(<ProfilePage />)),
       },
    ],
  },
  {
    path: '/instructor',
    element: guardInstructor(wrap(<InstructorLayout />)),
    children: [
      { index: true, element: wrap(<InstructorDashboardPage />) },
      // ...
    ],
  },
]);
```

**RequireRole:**
```tsx
// src/components/common/RequireRole.tsx
interface RequireRoleProps {
  roles: UserRole[];
  children: React.ReactElement;
}

export function RequireRole({ roles, children }: RequireRoleProps) {
  const { user, isInitializing } = useAuthStore();
  const location = useLocation();

  if (isInitializing) return <FullScreenSpinner />;

  if (!user) {
    // Запам'ятовуємо звідки прийшли, щоб повернутись після логіну
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (!roles.includes(user.role)) {
    return <Navigate to="/dashboard" replace />; // або інші редіректи залежно від ролі
  }

  return children;
}
```

**Чому:**
- Огортання через guard дозволяє гнучко захищати як окремі сторінки, так і цілі лейаути.
- Вся логіка перевірки доступу в одному місці (`RequireRole`).
- Lazy loading — admin panel не завантажується студенту.

**Альтернативи:**
- Router data loaders (v6.4+ feature) — потужніше, але складніше для команди що не знайома з ними.

---

## FADR-ARCH-004: Tooling & Libraries

**Рішення:** Фіксуємо tooling та вибір бібліотек одним записом — щоб не було дискусій "яку бібліотеку взяти" під час розробки.

### Core

| Інструмент | Вибір |
|---|---|
| Package manager | **npm 10+** (ships with Node 20) |
| Node version | **20 LTS** (зафіксовано в `.nvmrc`) |
| Bundler | Vite (йде з шаблону React + TS) |

### Code quality

| Інструмент | Конфіг |
|---|---|
| Linter | ESLint з `@typescript-eslint/recommended` + `eslint-plugin-react-hooks` + `eslint-plugin-jsx-a11y` |
| Formatter | Prettier + `prettier-plugin-tailwindcss` (автосортування Tailwind класів) |
| Git hooks | Husky + lint-staged (pre-commit: ESLint + Prettier на staged файлах) |

### UI / DX libraries

| Потреба | Бібліотека |
|---|---|
| Icons | `lucide-react` (йде з shadcn/ui) |
| Date utilities | `date-fns` |
| Drag-and-drop (reorder sections / lessons) | `@dnd-kit/core` |
| Markdown editor (Instructor, Post lesson) | `@uiw/react-md-editor` |
| Markdown renderer (Student, Post lesson view) | `react-markdown` |
| Video player (Video lessons) | Native `<video controls>` (v1 — play/pause/seekbar достатньо) |
| Toast notifications | `sonner` (через shadcn/ui) |
| Conditional classNames | `clsx` + `tailwind-merge` (через `cn()` helper) |

### File uploads

| Що | Як |
|---|---|
| Video lessons | Presigned URL → direct upload до Azure Blob |
| Course cover images | Presigned URL → direct upload до Azure Blob |
| User avatars | Presigned URL → direct upload до Azure Blob |

Бек повертає `uploadUrl` (тимчасовий SAS URL) + `blobUrl` (фінальний URL для збереження в entity). Фронт робить `PUT` файлу напряму на Azure, потім шле `blobUrl` у відповідний Command (наприклад `CreateVideoLessonCommand`).

**Чому:**
- **Зміна від першої редакції:** спочатку планувався pnpm, але на Windows він призвів до критичної несумісності з drive layout (store-dir у корені диска, EPERM на системних папках, втрата даних). Для cross-platform надійності (Win/Mac/Linux) повертаємось до npm — у v10 він достатньо швидкий, lockfile детермінований, не має платформо-специфічних квирків.
- **Node 20 LTS** — активний LTS до квітня 2026, потім maintenance LTS до 2027
- **Husky + lint-staged** — не дають закомітити код з ESLint/Prettier помилками. Врятовує від "забув запустити lint"
- **lucide-react** — бандлиться shadcn CLI, 1500+ іконок, tree-shakable
- **date-fns** — tree-shakable (імпортуєш тільки те що треба), функціональний API
- **@dnd-kit** — accessible (keyboard nav), активний проєкт, використовується в великих продуктах
- **react-markdown** + **@uiw/react-md-editor** — проста пара, без overkill від TipTap
- **Presigned URLs для upload** — файл не проходить через бек → API не буферизує 500MB відео в пам'яті
