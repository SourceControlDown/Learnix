import { useTranslation } from 'react-i18next';
import type { AiChatStatusDto } from '@/types/aiChat.types';

/**
 * What the assistant's status says, in the user's language. Green only when the backend confirms the
 * provider is answering — a widget that claims "Active" while the quota is spent is worse than no status
 * at all (ADR-CHAT-014).
 */
export function useAiStatusLabel(status: AiChatStatusDto | undefined) {
    const { t, i18n } = useTranslation('aiChat');

    if (!status || status.available) {
        return { label: t('status'), isAvailable: true };
    }

    const reason = status.reason ?? 'unavailable';

    if (reason === 'quota_exceeded' && status.retryAtUtc) {
        const time = new Date(status.retryAtUtc).toLocaleTimeString(i18n.language, {
            hour: '2-digit',
            minute: '2-digit',
        });

        return { label: t('unavailable.quotaUntil', { time }), isAvailable: false };
    }

    return { label: t(`unavailable.${reason}`), isAvailable: false };
}
