# Learnix — Frontend Architecture Decision Records (Auth)

> Формат: що вирішили → чому → які альтернативи відкинули.
> Архітектурні рішення беку — в [DECISIONS.md](./DECISIONS.md).

---

## FADR-AUTH-001: Auth — access token в memory, silent refresh on app start

**Рішення:**
- **Access token** — в Zustand store (in-memory). Не в `localStorage` (XSS vulnerable)
- **Refresh token** — HttpOnly cookie (встановлює бек, JS не має доступу)
- **Silent refresh on app start** — при завантаженні `AuthInitializer.tsx` робить `POST /auth/refresh`, парсить отриманий токен за допомогою `parseAccessToken` і якщо успішно — юзер залогінений.
- **На інших вкладках** — нічого спеціального. Дізнаються через 401 → refresh → continue
- **Google OAuth** — token-based flow через `@react-oauth/google` (не server-side redirect)

**Silent refresh (AuthInitializer):**
Логіка інкапсульована в компоненті `AuthInitializer`, який викликається при старті додатку.
```tsx
import { useEffect } from 'react';
import axios from 'axios';
import { useAuthStore } from '@/store/auth.store';
import { parseAccessToken } from '@/utils/parseAccessToken';
import { env } from '@/utils/env';

export function AuthInitializer({ children }: { children: React.ReactNode }) {
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);
    const finishInitialization = useAuthStore((s) => s.finishInitialization);

    useEffect(() => {
        axios
            .post<{ accessToken: string; avatarUrl: string | null }>(
                `${env.API_URL}/auth/refresh`,
                {},
                { withCredentials: true },
            )
            .then(({ data }) => {
                setAccessToken(data.accessToken);
                const user = parseAccessToken(data.accessToken);
                if (user) setUser({ ...user, avatarUrl: data.avatarUrl });
            })
            .catch(() => {
                // No valid refresh token — user is not logged in
            })
            .finally(() => {
                finishInitialization();
            });
    }, []);

    return <>{children}</>;
}
```

**Google OAuth — token-based flow (фактична реалізація):**
Бекенд реалізує `POST /api/auth/google` з тілом `{ idToken: string }` — не redirect handler.
Фронтенд отримує `id_token` від Google через `GoogleLogin` компонент і одразу надсилає його на бекенд.

**Чому token-based, а не redirect:**
- Бекенд валідує `id_token` через `GoogleJsonWebSignature.ValidateAsync` (Google.Apis.Auth) — не потребує server-side OAuth code exchange
- Немає callback-сторінки, простіший flow

**Чому:**
- `localStorage` для токенів = XSS вразливість. HttpOnly cookie недоступний JS
- Silent refresh на старті — юзер не бачить flash логіну після reload сторінки

**Альтернативи:**
- Access token у `localStorage` — простіше, але небезпечно
- Popup OAuth — складніше, блокується браузерами
