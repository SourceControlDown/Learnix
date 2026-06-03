import { useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { useAuthStore } from '@/store/auth.store';
import { queryKeys } from '@/api/queryKeys';
import { env } from '@/utils/env';
import { ACHIEVEMENT_META } from '@/const/localization/achievements';
import { CERTIFICATES } from '@/const/localization/certificates';
import type { NewMessageNotification, UnreadCountNotification } from '@/types/message.types';
import type { CertificateReadyNotification } from '@/types/certificate.types';

interface AchievementUnlockedPayload {
    achievementId: string;
    code: string;
    unlockedAt: string;
}

export function useNotificationsHub() {
    const accessToken = useAuthStore((s) => s.accessToken);
    const queryClient = useQueryClient();
    const navigate = useNavigate();
    const navigateRef = useRef(navigate);
    navigateRef.current = navigate;
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    useEffect(() => {
        if (!accessToken) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${env.HUB_URL}/hubs/notifications`, {
                accessTokenFactory: () => accessToken,
            })
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveMessage', (notification: NewMessageNotification) => {
            queryClient.invalidateQueries({ queryKey: queryKeys.messages.conversations() });
            queryClient.invalidateQueries({
                queryKey: queryKeys.messages.messages(notification.conversationId),
            });
        });

        connection.on('UnreadCountChanged', (notification: UnreadCountNotification) => {
            queryClient.setQueryData(queryKeys.messages.unreadCount(), {
                totalUnread: notification.totalUnread,
            });
        });

        connection.on('AchievementUnlocked', (payload: AchievementUnlockedPayload) => {
            const meta = ACHIEVEMENT_META[payload.code];
            toast.success(`🏆 ${meta?.name ?? payload.code}`, {
                description: meta?.description,
                duration: 6000,
            });
            queryClient.invalidateQueries({ queryKey: queryKeys.achievements.mine() });
        });

        connection.on('CertificateReady', (payload: CertificateReadyNotification) => {
            toast.success(CERTIFICATES.NOTIFICATION.TITLE, {
                description: `${CERTIFICATES.NOTIFICATION.DESCRIPTION_PREFIX}"${payload.courseTitle}"`,
                duration: 8000,
                action: {
                    label: CERTIFICATES.NOTIFICATION.ACTION,
                    onClick: () => navigateRef.current('/certificates'),
                },
            });
            queryClient.invalidateQueries({ queryKey: queryKeys.certificates.mine() });
        });

        connection.start().catch(() => {});
        connectionRef.current = connection;

        return () => {
            connection.stop();
        };
    }, [accessToken, queryClient]);
}
