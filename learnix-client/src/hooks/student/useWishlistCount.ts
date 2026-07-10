import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { wishlistApi } from '@/api/wishlist.api';
import { useAuthStore } from '@/store/auth.store';

export function useWishlistCount() {
    const user = useAuthStore((s) => s.user);

    return useQuery({
        queryKey: queryKeys.wishlist.count(),
        queryFn: wishlistApi.getCount,
        enabled: !!user,
    });
}
