# Learnix — Frontend Architecture Decision Records (Auth)

## ADR-FRONT-AUTH-001: Access Token Storage & Silent Refresh

**Decision:**
- **Access Token:** Stored exclusively in-memory (via Zustand). It is *never* saved to `localStorage` (XSS vulnerable).
- **Refresh Token:** Handled by the backend via an `HttpOnly` cookie (set by the backend, inaccessible to JS).
- **Silent Refresh on Load:** An `AuthInitializer` component runs on app startup. It sends a `POST /auth/refresh` request, parses the received token via `parseAccessToken`, and if successful, the user is logged in.
- **On other tabs:** Nothing special. They find out via a 401 error → refresh → continue.
- **Google OAuth:** Token-based flow via `@react-oauth/google` (not a server-side redirect).

**Silent Refresh (AuthInitializer):**
The logic is encapsulated in the `AuthInitializer` component, which is called when the application starts.
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

**Google OAuth — Token-Based Flow (Actual Implementation):**
The backend implements `POST /api/auth/google` with the body `{ idToken: string }` — it is not a redirect handler.
The frontend receives the `id_token` from Google via the `GoogleLogin` component and immediately sends it to the backend.

**Why token-based and not redirect:**
- The backend validates the `id_token` via `GoogleJsonWebSignature.ValidateAsync` (Google.Apis.Auth) — it doesn't require a server-side OAuth code exchange.
- There is no callback page, making the flow simpler.

**Why:**
- `localStorage` for tokens = XSS vulnerability. HttpOnly cookies are inaccessible to JS.
- **Preventing the Login Flash:** The `isInitializing` state defaults to `true`. In the `RequireRole` guard, we return `null` while `isInitializing` is `true`. The `AuthInitializer` calls `finishInitialization()` in its `finally` block. This guarantees that protected routes will wait for the silent refresh to finish before deciding whether to redirect the user to `/login`, eliminating the "login flash" effect upon reloading the page.

**Alternatives:**
- Access token in `localStorage` — simpler, but insecure.
- Popup OAuth — more complex, often blocked by browsers.
