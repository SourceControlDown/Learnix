# Learnix — Frontend Architecture Decision Records (UI)

> Формат: що вирішили → чому → які альтернативи відкинули.
> Архітектурні рішення беку — в [DECISIONS.md](./DECISIONS.md).

---

## FADR-UI-001: Tailwind everywhere + shadcn/ui, CSS змінні для темної теми

**Рішення:**
- **Усі стилі через Tailwind.** Без SCSS, без CSS Modules
- **shadcn/ui** — копіюємо через CLI, працює з коробки (бо сам на Tailwind)
- **Design tokens** — в `tailwind.config.ts` (статичні: spacing, breakpoints, font sizes) + `src/index.css` (CSS variables для кольорів, бо темна тема)
- **Темна тема** — через клас `.dark` на `<html>`, керується через Zustand + localStorage
- **`cn()` helper** — для умовних класів з конфлікт-резолюшеном через `tailwind-merge`

**Конвенції Tailwind:**
- Семантичні кольори: `bg-primary`, `text-foreground`, `border-border` — ніколи не hard-coded (`bg-blue-600`)
- Mobile-first: базові класи для мобільного, `md:`, `lg:` для більших екранів
- `prettier-plugin-tailwindcss` — автосортування класів (встановити одразу)
- Довгий `className` (>5 класів що повторюються) → витягти в компонент

**Чому:**
- Один ментальний контекст (Tailwind everywhere) — швидкість delivery
- shadcn/ui з коробки (бо він сам на Tailwind)
- CSS змінні в HSL формат — стандарт shadcn/ui, працює з `hsl(var(--primary) / 0.5)` для opacity
- Dark mode готовий з самого старту — як мінімум токени визначені

**Альтернативи (відкинуто):**
- SCSS Modules + shadcn hybrid — два ментальні контексти, складний setup Tailwind config під свої токени
- SCSS Modules без shadcn — тиждень-два на написання accessible примітивів
- CSS-in-JS (styled-components, emotion) — runtime overhead, не дружить з SSR

---

## FADR-UI-002: MarkdownRenderer — єдиний безпечний рендерер markdown

**Рішення:** Весь markdown у студентському UI рендериться через `src/components/common/MarkdownRenderer.tsx`. Компонент огортає `react-markdown` з кастомним рендерером для тегу `<a>`, що блокує будь-який `href` без протоколу `http://` або `https://`.

```tsx
// src/components/common/MarkdownRenderer.tsx
const safeComponents: Components = {
    a: ({ href, children }) => {
        if (!href?.match(/^https?:\/\//)) return <span>{children}</span>;
        return <a href={href} target="_blank" rel="noopener noreferrer">{children}</a>;
    },
};

export function MarkdownRenderer({ content, className }: MarkdownRendererProps) {
    return (
        <div className={cn('prose prose-neutral dark:prose-invert max-w-none', className)}>
            <Markdown components={safeComponents}>{content}</Markdown>
        </div>
    );
}
```

**Чому:** `react-markdown` за замовчуванням рендерить `[текст](javascript:alert(1))` як живе посилання — JS виконується при кліку. Це критично для контенту, що пишуть інструктори (PostLessonView) або генерує AI (AiChatMessage). Zod `.url()` не захищає, бо браузерний `URL` API вважає `javascript:` валідною схемою.

**Альтернативи:**
- `rehype-sanitize` плагін — потужніший (фільтрує будь-який HTML), але надлишково для markdown без `rehype-raw`; додає залежність
- DOMPurify перед передачею в компонент — не підходить, бо `react-markdown` не використовує `dangerouslySetInnerHTML`; sanitize відбувається на рівні рядка, а не DOM

**Наслідки:**
- **Заборонено** використовувати `react-markdown` або `ReactMarkdown` напряму в компонентах — тільки через `MarkdownRenderer`
- Новий компонент з markdown-контентом → імпортуй `MarkdownRenderer`, передавай `className` для кастомних prose-класів
- `javascript:`, `data:`, відносні URL в посиланнях рендеряться як plain text без кліку
