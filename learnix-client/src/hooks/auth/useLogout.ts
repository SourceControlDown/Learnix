import { useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { authApi } from '@/api/auth.api';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';

/**
 * The one way to sign a user out — see ADR-FRONT-AUTH-004.
 *
 * Every step matters. Without `queryClient.clear()` the previous user's cached server state
 * survives into the next session on a shared device. Clearing the store even when the API
 * call fails means a backend outage can never strand someone signed in.
 *
 * The redirect is a full page load, not `navigate()`. Signing out clears the store
 * synchronously, so a router transition still has the guarded route mounted when its guard
 * sees a null user — it renders its own `<Navigate to="/login">` and wins the race.
 * Leaving the document also drops anything the two clears above could have missed.
 */
export function useLogout() {
    const queryClient = useQueryClient();
    const clearAuth = useAuthStore((s) => s.logout);

    return useCallback(async () => {
        // Awaited, unlike the rest: a hard navigation would abort a request still in flight,
        // leaving the refresh cookie alive on the server.
        await authApi.logout().catch(() => {});
        clearAuth();
        queryClient.clear();
        window.location.assign(APP_ROUTES.public.home);
    }, [clearAuth, queryClient]);
}
