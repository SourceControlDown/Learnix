import { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { aiChatApi, streamAiMessage } from '@/api/aiChat.api';
import { queryKeys } from '@/api/queryKeys';
import type { ChatScope, LocalChatMessage } from '@/types/aiChat.types';

let msgCounter = 0;
const nextId = () => `msg-${Date.now()}-${++msgCounter}`;

export type AiChatController = ReturnType<typeof useAiChat>;

const keyOf = (scope: ChatScope) =>
    scope.kind === 'platform' ? 'platform' : `course:${scope.courseId}`;

/**
 * Owns one AI chat session. Call it from the component that outlives the chat surface,
 * so an in-flight stream and the message list survive the panel being closed.
 *
 * @param scope which conversation — the platform assistant or a course tutor.
 *   Pass a stable reference (a module constant or `useMemo`); it feeds a query key and a callback.
 * @param lessonId the lesson the student has open, sent alongside each message of a course tutor.
 */
export function useAiChat(isOpen: boolean, scope: ChatScope, lessonId?: string) {
    const { t } = useTranslation('aiChat');
    const [messages, setMessages] = useState<LocalChatMessage[]>([]);
    const [streamingContent, setStreamingContent] = useState('');
    const [isStreaming, setIsStreaming] = useState(false);
    const [activeToolName, setActiveToolName] = useState<string | null>(null);
    const [sessionLoaded, setSessionLoaded] = useState(false);
    const streamingRef = useRef('');
    const abortRef = useRef<AbortController | null>(null);
    const queryClient = useQueryClient();

    const scopeKey = keyOf(scope);
    const [prevScopeKey, setPrevScopeKey] = useState(scopeKey);

    // The player keeps this hook mounted across courses, so the scope can change underneath it.
    // Another course is another conversation: drop what belongs to the previous one.
    if (scopeKey !== prevScopeKey) {
        setPrevScopeKey(scopeKey);
        setMessages([]);
        setStreamingContent('');
        setIsStreaming(false);
        setActiveToolName(null);
        setSessionLoaded(false);
    }

    // Leaving a scope (or the page) must not leave a stream writing into the next one.
    useEffect(
        () => () => {
            abortRef.current?.abort();
            streamingRef.current = '';
        },
        [scopeKey],
    );

    const { data: session, isLoading: isSessionLoading } = useQuery({
        queryKey: queryKeys.aiChat.session(scope),
        queryFn: () => aiChatApi.getSession(scope),
        enabled: isOpen && !sessionLoaded,
        staleTime: Infinity,
    });

    if (session && !sessionLoaded) {
        setSessionLoaded(true);
        const localMsgs: LocalChatMessage[] = session.messages
            .filter((m) => m.role === 'user' || m.role === 'assistant')
            .map((m) => ({
                id: nextId(),
                role: m.role as 'user' | 'assistant',
                content: m.content,
            }));
        setMessages(localMsgs);
    }

    const { mutate: clearSession, isPending: isClearing } = useMutation({
        mutationFn: () => aiChatApi.clearSession(scope),
        onSuccess: () => {
            abortRef.current?.abort();
            streamingRef.current = '';
            setMessages([]);
            setStreamingContent('');
            setIsStreaming(false);
            setActiveToolName(null);
            setSessionLoaded(false);
            queryClient.removeQueries({ queryKey: queryKeys.aiChat.session(scope) });
        },
        onError: () => toast.error(t('error')),
    });

    const sendMessage = useCallback(
        async (text: string) => {
            if (isStreaming || !text.trim()) return;

            abortRef.current?.abort();
            const controller = new AbortController();
            abortRef.current = controller;

            setMessages((prev) => [...prev, { id: nextId(), role: 'user', content: text }]);
            setIsStreaming(true);
            streamingRef.current = '';
            setStreamingContent('');

            let settled = false;

            try {
                for await (const event of streamAiMessage(
                    scope,
                    text,
                    lessonId,
                    controller.signal,
                )) {
                    if (controller.signal.aborted) break;

                    if (event.type === 'text_delta') {
                        const delta = (event.data as { content: string }).content ?? '';
                        streamingRef.current += delta;
                        setStreamingContent(streamingRef.current);
                    } else if (event.type === 'tool_use_start') {
                        const { toolName } = event.data as { toolName: string; callId: string };
                        setActiveToolName(toolName);
                    } else if (event.type === 'tool_use_end') {
                        setActiveToolName(null);
                    } else if (event.type === 'message_end') {
                        settled = true;
                        const finalContent = streamingRef.current;
                        streamingRef.current = '';
                        setStreamingContent('');
                        if (finalContent) {
                            setMessages((prev) => [
                                ...prev,
                                { id: nextId(), role: 'assistant', content: finalContent },
                            ]);
                        }
                        setIsStreaming(false);
                        break;
                    } else if (event.type === 'error') {
                        settled = true;
                        toast.error(t('error'));
                        streamingRef.current = '';
                        setStreamingContent('');
                        setIsStreaming(false);
                        setActiveToolName(null);
                        break;
                    }
                }

                // A provider that dies mid-stream closes the connection after the SSE headers are out, so
                // neither message_end nor error ever arrives. Without this the composer stays disabled.
                if (!settled && !controller.signal.aborted) {
                    toast.error(t('error'));
                    streamingRef.current = '';
                    setStreamingContent('');
                    setIsStreaming(false);
                    setActiveToolName(null);
                }
            } catch {
                if (!controller.signal.aborted) {
                    toast.error(t('error'));
                }
                streamingRef.current = '';
                setStreamingContent('');
                setIsStreaming(false);
                setActiveToolName(null);
            }
        },
        [isStreaming, t, scope, lessonId],
    );

    return {
        messages,
        streamingContent,
        isStreaming,
        activeToolName,
        isSessionLoading,
        sendMessage,
        clearSession,
        isClearing,
    };
}
