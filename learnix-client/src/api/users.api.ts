import { api } from './axios.instance';
import type { MyProfileDto, UpdateProfileRequest } from '@/types/user.types';

export const usersApi = {
    getMyProfile: () => api.get<MyProfileDto>('/users/me').then((r) => r.data),

    updateProfile: (data: UpdateProfileRequest) =>
        api.put<void>('/users/me', data).then((r) => r.data),
};
