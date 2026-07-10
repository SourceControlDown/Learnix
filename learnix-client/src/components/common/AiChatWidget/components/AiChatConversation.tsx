import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { ChatComposer } from '@/components/common/chat/ChatComposer';
import { CHAT_LIMITS } from '@/const/ui.constants';
import type { AiChatController } from '@/hooks/realtime/useAiChat';
import { cn } from '@/utils/cn';
import { AiChatClearButton } from './AiChatClearButton';
import { AiChatMessages } from './AiChatMessages';

interface AiChatConversationProps {
    chat: AiChatController;
    /** Centre messages in a max-width column — for full-screen and expanded surfaces. */
    isWide?: boolean;
    /** Left side of the toolbar: a title block, a tab strip, ... */
    header?: ReactNode;
    /** Right side of the toolbar, after the "new conversation" button. */
    actions?: ReactNode;
    toolbarClassName?: string;
}

/**
 * The AI chat itself — messages and composer, with an optional toolbar. Renders as a
 * fragment, so the host decides how it is positioned (floating box, docked panel, sheet).
 * Omit both `header` and `actions` when the host already provides its own toolbar.
 */
export function AiChatConversation({
    chat,
    isWide = false,
    header,
    actions,
    toolbarClassName,
}: AiChatConversationProps) {
    const { t } = useTranslation('aiChat');
    const {
        messages,
        streamingContent,
        isStreaming,
        activeToolName,
        isSessionLoading,
        sendMessage,
        isClearing,
    } = chat;

    const hasToolbar = header !== undefined || actions !== undefined;

    return (
        <>
            {hasToolbar && (
                <div
                    className={cn(
                        'flex shrink-0 items-center justify-between gap-2 border-b border-border px-4 py-3',
                        toolbarClassName,
                    )}
                >
                    {header}
                    <div className="flex shrink-0 items-center gap-1.5">
                        <AiChatClearButton chat={chat} />
                        {actions}
                    </div>
                </div>
            )}

            <AiChatMessages
                messages={messages}
                streamingContent={streamingContent}
                isStreaming={isStreaming}
                activeToolName={activeToolName}
                isSessionLoading={isSessionLoading}
                isExpanded={isWide}
            />

            <div
                className={cn(
                    'shrink-0 border-t border-border p-3',
                    isWide && 'mx-auto w-full max-w-3xl border-t-0 p-4 pb-6',
                )}
            >
                <ChatComposer
                    onSend={sendMessage}
                    placeholder={t('placeholder')}
                    disabled={isStreaming || isClearing}
                    maxLength={CHAT_LIMITS.AI_MESSAGE_MAX}
                />
            </div>
        </>
    );
}
