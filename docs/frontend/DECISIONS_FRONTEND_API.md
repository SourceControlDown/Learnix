# Learnix — Frontend Architecture Decision Records (API & State)

> Формат: що вирішили → чому → які альтернативи відкинули.
> Архітектурні рішення беку — в [DECISIONS.md](./DECISIONS.md).

---

## FADR-API-001: API layer — Axios instance + interceptors + queued refresh

**Рішення:**
- Один Axios instance в `src/api/axios.instance.ts`
- Request interceptor додає JWT з Zustand store
- Response interceptor ловить 401 (перевіряючи чи є Bearer) → робить silent refresh → повторює запит
- **Черга для concurrent 401s** — якщо 5 запитів впадуть одночасно, робимо ОДИН refresh, не 5
- API модулі (`courses.api.ts`, `auth.api.ts`) — тонкі обгортки з типізованими функціями

**Axios instance:**
```ts
// src/api/axios.instance.ts
import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/store/auth.store';
import { env } from '@/utils/env';

export const api = axios.create({
    baseURL: env.API_URL,
    withCredentials: true,
});

api.interceptors.request.use((config) => {
    const token = useAuthStore.getState().accessToken;
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
});

let isRefreshing = false;
let failedQueue: Array<{
    resolve: (token: string) => void;
    reject: (err: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
    failedQueue.forEach((p) => (error ? p.reject(error) : p.resolve(token!)));
    failedQueue = [];
};

api.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
        const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

        const hasBearer = original.headers?.Authorization?.toString().startsWith('Bearer ');
        if (error.response?.status !== 401 || original._retry || !hasBearer) {
            return Promise.reject(error);
        }

        if (isRefreshing) {
            return new Promise((resolve, reject) => {
                failedQueue.push({
                    resolve: (token) => {
                        original.headers.Authorization = `Bearer ${token}`;
                        resolve(api(original));
                    },
                    reject,
                });
            });
        }

        original._retry = true;
        isRefreshing = true;

        try {
            const { data } = await axios.post(
                `${env.API_URL}/auth/refresh`,
                {},
                { withCredentials: true },
            );
            useAuthStore.getState().setAccessToken(data.accessToken);
            processQueue(null, data.accessToken);
            original.headers.Authorization = `Bearer ${data.accessToken}`;
            return api(original);
        } catch (refreshError) {
            processQueue(refreshError, null);
            useAuthStore.getState().logout();
            window.location.href = '/login';
            return Promise.reject(refreshError);
        } finally {
            isRefreshing = false;
        }
    },
);
```

**Чому:**
- Черга 401-запитів критична: без неї 5 одночасних запитів → 5 refresh-викликів → race condition
- Типізовані API-функції дозволяють TypeScript ловити помилки контракту з беком
- Додаткова перевірка `hasBearer` відфільтровує запити, які не використовують авторизацію, уникаючи безкінечного циклу рефрешу.

**Альтернативи:**
- OpenAPI codegen (openapi-typescript) — має сенс коли бек стабільний. Для проєкту де бек і фронт розробляються паралельно — ручні типи дають більше контролю
- fetch замість axios — можна, але interceptors з axios коротші і зрозуміліші

---

## FADR-API-002: Server state vs client state — чіткий розподіл

**Рішення:**
- **TanStack Query** — усі дані з бекенду (курси, юзери, enrollments, нотифікації). Жодних API-даних у Zustand
- **Zustand** — тільки клієнтський стан: access token, user summary, UI стан (sidebar open, mobile menu), theme
- **useState** — локальний UI стан в межах одного компонента (open/close dropdown, form state)

**Правило коли йти в `ui.store` vs `useState`:**

| Стан | Де | Приклад |
|---|---|---|
| Shared між незв'язаними компонентами | `ui.store` | Sidebar open state (Header button + Sidebar body) |
| Локальний для компонента | `useState` | Dropdown open, accordion section expanded |
| Локальний для форми | `react-hook-form` | Field values, errors, touched |
| З бекенду | `React Query` | Курси, юзери, progress |

**Чому:**
- Дублювання API-даних в Zustand = складна синхронізація і stale data
- React Query з коробки: кеш, refetch, stale-while-revalidate, optimistic updates
- Zustand для auth — бо треба доступ в axios interceptors (де React-хуків немає)

---

## FADR-API-003: React Query — ієрархічні query keys, optimistic для дешевих дій

**Рішення:**
- Query keys — ієрархічна структура в `src/api/queryKeys.ts`
- Дефолтні налаштування QueryClient: `staleTime: 60s`, `gcTime: 5m`, `retry: 1`, `refetchOnWindowFocus: false`
- Optimistic updates — для low-stakes дій (likes, mark complete). Pessimistic — для mutations з бізнес-наслідками (create, update, payment)
- Інвалідація — явна в `onSuccess` мутації

**Чому ієрархічні keys:**
- Одним викликом інвалідуємо "всі списки курсів" (різні фільтри) без зачіпання деталей:
  `queryClient.invalidateQueries({ queryKey: queryKeys.courses.lists() })`
- `queryKeys.courses.all` скасовує всі запити курсів разом

**Чому staleTime 60s:**
- 0 (default) — агресивний refetch на кожне mount
- Infinity — дані ніколи не оновлюються без явного invalidate
- 60s — розумний компроміс для LMS (дані не критично свіжі)
