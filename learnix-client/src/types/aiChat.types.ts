export interface ChatMessageDto {
    role: string;
    content: string;
    sentAt: string;
}

export interface ChatSessionDto {
    sessionId: string;
    messages: ChatMessageDto[];
}

export interface LocalChatMessage {
    id: string;
    role: 'user' | 'assistant';
    content: string;
}

/**
 * Which conversation this is. The platform assistant and each course tutor keep separate
 * histories, budgets and tools — clearing one leaves the others untouched.
 */
export type ChatScope = { kind: 'platform' } | { kind: 'course'; courseId: string };
