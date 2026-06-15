import { useState, useEffect, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import { useInfiniteQuery, keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/api/queryKeys';
import { messagesApi } from '@/api/messages.api';
import { ConversationList } from './components/ConversationList';
import { ConversationView } from '@/components/common/ConversationView';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import { cn } from '@/utils/cn';
import { useDebounce } from '@/hooks/useDebounce';
import type { ConversationDetail, ConversationSummary } from '@/types/message.types';

export default function MessagesPage() {
    const { t } = useTranslation('messages');
    const location = useLocation();
    const initialConversation =
        (location.state as { initialConversation?: ConversationDetail } | null)
            ?.initialConversation ?? null;

    const [selected, setSelected] = useState<ConversationSummary | null>(() => {
        if (!initialConversation) return null;
        return { ...initialConversation, lastMessagePreview: null, lastMessageAt: null };
    });

    const [searchQuery, setSearchQuery] = useState('');
    const debouncedSearch = useDebounce(searchQuery, 500);

    const {
        data,
        isLoading,
        fetchNextPage,
        hasNextPage,
        isFetchingNextPage
    } = useInfiniteQuery({
        queryKey: [...queryKeys.messages.conversations(), debouncedSearch],
        queryFn: ({ pageParam = 0 }) =>
            messagesApi.getConversations(pageParam, 20, debouncedSearch || undefined),
        initialPageParam: 0,
        getNextPageParam: (lastPage) =>
            lastPage.hasNextPage ? lastPage.page * 20 + 20 : undefined,
        placeholderData: keepPreviousData,
    });

    const conversations = data?.pages.flatMap((p) => p.items) ?? [];

    const initialSyncDone = useRef(false);

    useEffect(() => {
        if (conversations.length === 0) return;

        if (initialConversation && !initialSyncDone.current) {
            // First load only: select the conversation we came from
            const match = conversations.find((c) => c.id === initialConversation.id);
            if (match) {
                setSelected(match);
                initialSyncDone.current = true;
            }
            return;
        }

        // On subsequent refetches: refresh the currently selected conversation's data
        setSelected((prev) => {
            if (!prev) return prev;
            const fresh = conversations.find((c) => c.id === prev.id);
            return fresh ?? prev;
        });
    }, [conversations]); // eslint-disable-line react-hooks/exhaustive-deps

    if (isLoading && !selected) {
        return (
            <div className="flex h-full items-center justify-center">
                <LoadingSpinner />
            </div>
        );
    }

    return (
        <div className="flex h-full overflow-hidden">
            {/* Conversation list sidebar */}
            <aside
                className={cn(
                    'w-full shrink-0 flex-col overflow-hidden border-r border-border bg-card md:flex md:w-80 lg:w-96',
                    selected ? 'hidden' : 'flex',
                )}
            >
                <div className="shrink-0 border-b border-border px-4 py-3">
                    <h1 className="font-heading text-lg font-semibold text-foreground">
                        {t('pageTitle')}
                    </h1>
                    <div className="pt-3">
                        <input
                            type="text"
                            placeholder={t('searchPlaceholder', 'Search...')}
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                        />
                    </div>
                </div>
                <div 
                    className="min-h-0 flex-1 overflow-y-auto"
                    onScroll={(e) => {
                        const target = e.target as HTMLDivElement;
                        if (target.scrollHeight - target.scrollTop <= target.clientHeight + 100) {
                            if (hasNextPage && !isFetchingNextPage) {
                                fetchNextPage();
                            }
                        }
                    }}
                >
                    <ConversationList
                        conversations={conversations}
                        selectedId={selected?.id ?? null}
                        onSelect={setSelected}
                        isFetchingNextPage={isFetchingNextPage}
                    />
                </div>
            </aside>

            {/* Chat area */}
            <main
                className={cn(
                    'min-w-0 flex-1 flex-col overflow-hidden bg-background',
                    selected ? 'flex' : 'hidden md:flex',
                )}
            >
                {selected ? (
                    <ConversationView conversation={selected} onBack={() => setSelected(null)} />
                ) : (
                    <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
                        {t('selectConversation')}
                    </div>
                )}
            </main>
        </div>
    );
}
