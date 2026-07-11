import { useAiStatusLabel } from '@/hooks/shared/useAiStatusLabel';
import type { AiChatStatusDto } from '@/types/aiChat.types';
import { cn } from '@/utils/cn';

interface AiChatStatusLineProps {
    status: AiChatStatusDto | undefined;
    className?: string;
}

/** The dot and the line under the assistant's name: green when it can answer, amber when it cannot. */
export function AiChatStatusLine({ status, className }: AiChatStatusLineProps) {
    const { label, isAvailable } = useAiStatusLabel(status);

    return (
        <p
            className={cn(
                'mt-1.5 flex items-center gap-1.5 text-xs text-muted-foreground',
                className,
            )}
        >
            <span
                className={cn(
                    'size-2 shrink-0 rounded-full',
                    isAvailable
                        ? 'bg-success shadow-[0_0_5px_rgba(var(--success),0.8)]'
                        : 'bg-warning',
                )}
            />
            <span className="truncate">{label}</span>
        </p>
    );
}
