import { useEffect } from 'react';
import { Maximize2, Minimize2, RotateCcw, X } from 'lucide-react';
import { cn } from '@/utils/cn';
import { AI_CHAT } from '@/const/localization/aiChat';
import { AiChatMessages } from './AiChatMessages';
import { AiChatInput } from './AiChatInput';
import { useAiChat } from '@/hooks/useAiChat';

interface AiChatPanelProps {
    isOpen: boolean;
    onClose: () => void;
    isExpanded: boolean;
    onToggleExpand: () => void;
}

export function AiChatPanel({ isOpen, onClose, isExpanded, onToggleExpand }: AiChatPanelProps) {
    const {
        messages,
        streamingContent,
        isStreaming,
        activeToolName,
        isSessionLoading,
        sendMessage,
        clearSession,
        isClearing,
    } = useAiChat(isOpen);

    useEffect(() => {
        document.body.style.overflow = isExpanded ? 'hidden' : '';
        return () => { document.body.style.overflow = ''; };
    }, [isExpanded]);

    const visible = isOpen || isExpanded;

    return (
        <>
            {/* Backdrop — fade in/out незалежно від панелі */}
            <div
                className={cn(
                    'fixed inset-0 z-[59] bg-black/50 transition-opacity duration-300',
                    isExpanded ? 'opacity-100' : 'pointer-events-none opacity-0',
                )}
            />

            {/* Один і той самий div — анімує position/size між станами */}
            <div
                className={cn(
                    'fixed z-[60] flex flex-col overflow-hidden rounded-xl border border-border bg-card shadow-xl',
                    'transition-all duration-300 ease-in-out',
                    isExpanded
                        ? 'bottom-4 right-4 h-[calc(100vh-2rem)] w-[calc(100vw-2rem)]'
                        : 'bottom-[88px] right-6 h-[520px] w-80',
                    !isExpanded && (visible
                        ? 'opacity-100'
                        : 'pointer-events-none opacity-0'),
                )}
            >
                {/* Header */}
                <div className="flex shrink-0 items-center justify-between border-b border-border px-3 py-2.5">
                    <p className="font-heading text-sm font-semibold text-foreground">
                        {AI_CHAT.TITLE}
                    </p>
                    <div className="flex items-center gap-1">
                        <button
                            onClick={() => clearSession()}
                            disabled={isClearing || isStreaming || messages.length === 0}
                            title={AI_CHAT.ARIA_CLEAR}
                            aria-label={AI_CHAT.ARIA_CLEAR}
                            className={cn(
                                'rounded-md p-1 text-muted-foreground transition-colors',
                                'hover:bg-secondary hover:text-foreground',
                                'disabled:pointer-events-none disabled:opacity-40',
                            )}
                        >
                            <RotateCcw size={14} />
                        </button>
                        <button
                            onClick={onToggleExpand}
                            aria-label={isExpanded ? AI_CHAT.ARIA_COLLAPSE : AI_CHAT.ARIA_EXPAND}
                            className="rounded-md p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                        >
                            {isExpanded ? <Minimize2 size={14} /> : <Maximize2 size={14} />}
                        </button>
                        <button
                            onClick={onClose}
                            aria-label={AI_CHAT.ARIA_CLOSE}
                            className="rounded-md p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                        >
                            <X size={14} />
                        </button>
                    </div>
                </div>

                <AiChatMessages
                    messages={messages}
                    streamingContent={streamingContent}
                    isStreaming={isStreaming}
                    activeToolName={activeToolName}
                    isSessionLoading={isSessionLoading}
                />

                <AiChatInput onSend={sendMessage} disabled={isStreaming || isClearing} />
            </div>
        </>
    );
}
