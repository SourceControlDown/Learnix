import { useMutation } from '@tanstack/react-query';
import { useNavigate, useLocation } from 'react-router-dom';
import { authApi } from '@/api/auth.api';
import { useAuthStore } from '@/store/auth.store';

export function useGoogleAuth() {
    const navigate = useNavigate();
    const location = useLocation();
    const setAccessToken = useAuthStore((s) => s.setAccessToken);

    const from =
        (location.state as { from?: { pathname: string } } | null)?.from?.pathname ?? '/dashboard';

    const { mutate, isPending } = useMutation({
        mutationFn: authApi.googleLogin,
        onSuccess: (data) => {
            setAccessToken(data.accessToken);
            navigate(from, { replace: true });
        },
    });

    return {
        onGoogleCredential: (credential: string) => mutate({ idToken: credential }),
        isPending,
    };
}
