import { api } from './axios.instance';
import type { GetMyAchievementsResponse } from '@/types/achievement.types';

export const achievementsApi = {
    getMyAchievements: () =>
        api.get<GetMyAchievementsResponse>('/achievements/me').then((r) => r.data),

    markSeen: (achievementId: string) =>
        api.post<void>(`/achievements/${achievementId}/seen`).then((r) => r.data),
};
