import { useTranslation } from 'react-i18next';
import { RotateCcw } from 'lucide-react';
import type { AiChatController } from '@/hooks/realtime/useAiChat';
import { cn } from '@/utils/cn';

interface AiChatClearButtonProps {
    chat: AiChatController;
}

export function AiChatClearButton({ chat }: AiChatClearButtonProps) {
    const { t } = useTranslation('aiChat');
    const { clearSession, isClearing, isStreaming, messages } = chat;

    return (
        <button
            type="button"
            onClick={() => clearSession()}
            disabled={isClearing || isStreaming || messages.length === 0}
            title={t('ariaClear')}
            aria-label={t('ariaClear')}
            className={cn(
                'rounded-md p-1.5 text-muted-foreground transition-colors',
                'hover:bg-secondary hover:text-foreground',
                'disabled:pointer-events-none disabled:opacity-40',
            )}
        >
            <RotateCcw size={18} />
        </button>
    );
}
