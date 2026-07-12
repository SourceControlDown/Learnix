import { Toaster } from 'sonner';
import { useThemeStore } from '@/store/theme.store';

/**
 * Sonner keeps its own light/dark state and defaults to light, so without this it would paint
 * light toasts over a dark page. The colours themselves come from the design tokens — see the
 * `[data-sonner-toaster]` block in `styles/index.css`.
 */
export function AppToaster() {
    const theme = useThemeStore((s) => s.theme);

    return <Toaster position="top-right" offset="80px" richColors theme={theme} />;
}
