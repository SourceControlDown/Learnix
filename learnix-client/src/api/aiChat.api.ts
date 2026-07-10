import type { ChatScope, ChatSessionDto } from '@/types/aiChat.types';
import { api } from './axios.instance';

/** The scope is the address of the session — the user comes from the token. */
function scopePath(scope: ChatScope): string {
    return scope.kind === 'platform' ? '/ai-chat/platform' : `/ai-chat/courses/${scope.courseId}`;
}

export const aiChatApi = {
    getSession: (scope: ChatScope) =>
        api.get<ChatSessionDto>(`${scopePath(scope)}/session`).then((r) => r.data),

    clearSession: (scope: ChatScope) =>
        api.delete(`${scopePath(scope)}/session`).then((r) => r.data),
};

export async function* streamAiMessage(
    scope: ChatScope,
    message: string,
    lessonId?: string,
    signal?: AbortSignal,
): AsyncGenerator<{ type: string; data: unknown }> {
    // lessonId travels beside the message, never inside it: the stored text is what the user sees again.
    const body = scope.kind === 'course' ? { message, lessonId } : { message };

    const response = await api.post(`${scopePath(scope)}/messages`, body, {
        responseType: 'stream',
        adapter: 'fetch', // Use fetch adapter to support streaming in the browser
        signal,
    });

    const stream = response.data as unknown as ReadableStream<Uint8Array>;
    if (!stream) {
        throw new Error('No stream in response');
    }

    const reader = stream.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    try {
        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            buffer += decoder.decode(value, { stream: true });

            // SSE events are separated by \n\n
            const blocks = buffer.split('\n\n');
            buffer = blocks.pop() ?? '';

            for (const block of blocks) {
                if (!block.trim()) continue;

                const lines = block.split('\n');
                let eventType = 'message';
                let data = '';

                for (const line of lines) {
                    if (line.startsWith('event: ')) eventType = line.slice(7).trim();
                    else if (line.startsWith('data: ')) data = line.slice(6);
                }

                if (data) {
                    try {
                        yield { type: eventType, data: JSON.parse(data) };
                    } catch {
                        yield { type: eventType, data };
                    }
                }
            }
        }
    } finally {
        reader.releaseLock();
    }
}
