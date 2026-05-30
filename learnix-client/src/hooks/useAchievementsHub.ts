import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { useAuthStore } from '@/store/auth.store';
import { queryKeys } from '@/api/queryKeys';
import { env } from '@/utils/env';
import { ACHIEVEMENT_META } from '@/const/localization/achievements';

interface AchievementUnlockedPayload {
    achievementId: string;
    code: string;
    unlockedAt: string;
}

export function useAchievementsHub() {
    const accessToken = useAuthStore((s) => s.accessToken);
    const queryClient = useQueryClient();
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    useEffect(() => {
        if (!accessToken) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${env.HUB_URL}/hubs/achievements`, {
                accessTokenFactory: () => accessToken,
            })
            .withAutomaticReconnect()
            .build();

        connection.on('AchievementUnlocked', (payload: AchievementUnlockedPayload) => {
            const meta = ACHIEVEMENT_META[payload.code];

            toast.success(`🏆 ${meta?.name ?? payload.code}`, {
                description: meta?.description,
                duration: 6000,
            });

            queryClient.invalidateQueries({ queryKey: queryKeys.achievements.mine() });
        });

        connection.start().catch(() => {});

        connectionRef.current = connection;

        return () => {
            connection.stop();
        };
    }, [accessToken, queryClient]);
}
