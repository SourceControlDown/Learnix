import { useCallback, useSyncExternalStore } from 'react';

/**
 * Subscribes to a CSS media query.
 *
 * Use it when a breakpoint must drive *which components mount*, not just how they look.
 * For purely visual changes prefer Tailwind's responsive variants — they cost no re-render.
 */
export function useMediaQuery(query: string): boolean {
    const subscribe = useCallback(
        (onStoreChange: () => void) => {
            const mediaQueryList = window.matchMedia(query);
            mediaQueryList.addEventListener('change', onStoreChange);
            return () => mediaQueryList.removeEventListener('change', onStoreChange);
        },
        [query],
    );

    return useSyncExternalStore(
        subscribe,
        () => window.matchMedia(query).matches,
        () => false,
    );
}
