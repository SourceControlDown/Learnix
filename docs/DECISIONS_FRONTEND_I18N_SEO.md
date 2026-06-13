# Learnix — Frontend Architecture Decision Records (I18n & SEO)

> Формат: що вирішили → чому → які альтернативи відкинули.
> Архітектурні рішення беку — в [DECISIONS.md](./DECISIONS.md).

---

## FADR-INTL-001: Localization — react-i18next з JSON namespace-файлами

**Рішення:** Весь UI-текст зберігається у JSON-файлах по одному на namespace (page/domain), окремо для кожної мови. Компоненти і хуки використовують хук `useTranslation(namespace)` з `react-i18next`. Перемикач мови — персистований у localStorage через `useLocaleStore`.

**Підтримувані мови:** `en` (English) та `uk` (Українська). Fallback: `en`.

**Чому `react-i18next`:**
- Стандарт індустрії для React i18n у 2025
- Namespace = один файл на сторінку/домен → маппиться 1:1 на стару структуру `const/localization/`
- Вбудована плюралізація через CLDR (`Intl.PluralRules`) — правильно для UK
- `LanguageDetector` автоматично підтягує мову з localStorage або браузера
- `interpolation.escapeValue: false` — React сам екранує, немає подвійного екранування
- Статичний імпорт JSON (не lazy-loading) — прийнятно для 20 namespace-ів, нульова latency

**Альтернативи:**
- Static TS const-словники (FADR-012 v1) — зручно для однієї мови, не підтримує runtime-перемикання
- `react-intl` (FormatJS) — більш формальний ICU format, overkill для простих потреб
- Lingui — compile-time extraction, кращий DX, але складніший сетап для solo-проєкту
- JSON без бібліотеки — потребує власного контексту і provider'а

**Наслідки:**
- Нова сторінка → новий JSON-файл у `en/` і `uk/`, реєстрація в `config.ts` у `resources`
- Нові рядки з параметрами → i18next interpolation `{{variable}}`, не JS-функції
- Не використовувати `i18n.t()` напряму в компонентах — тільки `useTranslation` hook

---

## FADR-INTL-002: SEO — react-helmet-async, без SSR

**Рішення:** Базова SEO-оптимізація реалізована через `react-helmet-async` у клієнтському SPA без SSR. Підхід охоплює чотири рівні:

1. **`index.html`** — статичні fallback-теги (`<title>`, `<meta name="description">`, OG-теги, Twitter Card), які бачать краулери до виконання JS.
2. **`react-helmet-async`** — перезаписує теги на рівні сторінки при рендері React. `<HelmetProvider>` обгортає застосунок у `main.tsx`.
3. **Публічні сторінки** — `LandingPage`, `CourseCatalogPage`, `FaqPage` використовують `t('seo.title')` / `t('seo.description')` з відповідних i18n namespace-ів. `CourseDetailPage` будує теги динамічно з `CourseDetailDto` (title, description[:160], coverImageUrl).
4. **Приватні layouts** — `AdminLayout` та `InstructorLayout` рендерять `<meta name="robots" content="noindex,nofollow">` — краулери не індексують сторінки навіть якщо доберуться до них.
5. **`public/robots.txt`** — забороняє краулерам `/admin/`, `/instructor/`, та всі приватні student-маршрути.
6. **`public/sitemap.xml`** — статичні публічні URL (`/`, `/courses`, `/faq`). Динамічні URL курсів потребують build-time генератора або серверного ендпоінту (не реалізовано у v1).

**Чому без SSR:**
- Learnix — Vite SPA. Міграція на Vite SSR або Remix — тиждень роботи і суттєве ускладнення деплою.
- Googlebot рендерить JS, тому теги через `react-helmet-async` ефективні для Google.
- Для соцмереж (og:image в Twitter, Facebook) fallback-теги в `index.html` достатні для лендингу, а для курсів Googlebot все одно потрібен JS.
- Для MVP-рівня портфоліо-проєкту цей підхід дає 80% цінності SEO за 5% зусиль SSR.

**Альтернативи:**
- **Vite SSR / Vike** — повний контроль, але складний деплой (Node.js сервер, не статика).
- **Remix або Next.js** — найкраще для SEO, але потребує рефакторингу роутингу і стейт-менеджменту.
- **`vite-plugin-prerender`** — statically prerenders known routes at build time. Має сенс для статичних публічних сторінок (`/`, `/faq`), але не для динамічних курсів.

**Наслідки:**
- Нова публічна сторінка → додати `<Helmet>` з `<title>` та `<meta name="description">` в компонент сторінки; додати `seo.title` / `seo.description` ключі у відповідний i18n namespace (en + uk).
- Нова приватна сторінка / layout → додати `<meta name="robots" content="noindex,nofollow">` у layout-компонент.
- `robots.txt` оновлювати вручну при додаванні нових приватних маршрутів.
