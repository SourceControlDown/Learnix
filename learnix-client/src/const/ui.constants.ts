export const PAGINATION = {
    DEFAULT: 20,
    CATALOG: 12,
    APPLICATIONS: 10,
    DASHBOARD_RECENT: 5,
} as const;

export const CHAT_LIMITS = {
    /** Student ↔ instructor direct message. Mirrors ConversationConstants.MessageMaxLength. */
    MESSAGE_MAX: 2000,
    /** AI tutor prompt. Mirrors AiChatConstants.MessageMaxLength. */
    AI_MESSAGE_MAX: 4000,
} as const;
