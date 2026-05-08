import { useQuery } from '@tanstack/react-query';
import { usersApi } from '@/api/users.api';
import { queryKeys } from '@/api/queryKeys';

export function useMyProfile() {
    return useQuery({
        queryKey: queryKeys.users.myProfile(),
        queryFn: usersApi.getMyProfile,
    });
}
