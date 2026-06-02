import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { messagesApi } from '@/api/messages.api';
import { InstructorConversationList } from './components/InstructorConversationList';
import { ConversationView } from '@/components/common/ConversationView';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import { MESSAGES } from '@/const/localization/messages';
import type { ConversationSummary } from '@/types/message.types';

export default function InstructorMessagesPage() {
    const [selected, setSelected] = useState<ConversationSummary | null>(null);

    const { data: conversations = [], isLoading } = useQuery({
        queryKey: queryKeys.messages.conversations(),
        queryFn: messagesApi.getConversations,
    });

    useEffect(() => {
        setSelected((prev) => {
            if (!prev) return prev;
            const fresh = conversations.find((c) => c.id === prev.id);
            return fresh ?? prev;
        });
    }, [conversations]);

    const totalUnread = conversations.reduce((sum, c) => sum + c.unreadCount, 0);

    if (isLoading) {
        return (
            <div className="flex h-full items-center justify-center">
                <LoadingSpinner />
            </div>
        );
    }

    return (
        <div className="flex h-full overflow-hidden">
            {/* Conversation list sidebar */}
            <aside className="flex w-80 shrink-0 flex-col overflow-hidden border-r border-border bg-card">
                <div className="shrink-0 border-b border-border px-4 py-3">
                    <div className="flex items-center justify-between">
                        <h1 className="font-heading text-lg font-semibold text-foreground">
                            {MESSAGES.INSTRUCTOR_PAGE_TITLE}
                        </h1>
                        {totalUnread > 0 && (
                            <span className="flex h-6 min-w-6 items-center justify-center rounded-full bg-primary px-1.5 text-xs font-bold text-primary-foreground">
                                {totalUnread}
                            </span>
                        )}
                    </div>
                </div>
                <div className="min-h-0 flex-1 overflow-y-auto">
                    <InstructorConversationList
                        conversations={conversations}
                        selectedId={selected?.id ?? null}
                        onSelect={setSelected}
                    />
                </div>
            </aside>

            {/* Chat area */}
            <main className="flex min-w-0 flex-1 flex-col overflow-hidden bg-background">
                {selected ? (
                    <ConversationView conversation={selected} />
                ) : (
                    <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
                        {MESSAGES.SELECT_CONVERSATION}
                    </div>
                )}
            </main>
        </div>
    );
}
