import { useMutation, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '@/api/users.api';
import { queryKeys } from '@/api/queryKeys';

export function useUpdateProfile() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: usersApi.updateProfile,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.users.myProfile() });
            queryClient.invalidateQueries({ queryKey: queryKeys.achievements.mine() });
        },
    });
}
