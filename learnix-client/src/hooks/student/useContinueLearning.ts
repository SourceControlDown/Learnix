import { useQuery } from '@tanstack/react-query';
import { enrollmentsApi } from '@/api/enrollments.api';
import { queryKeys } from '@/api/queryKeys';
import { useAuthStore } from '@/store/auth.store';

/** Null when the student has no course in progress. */
export function useContinueLearning() {
    const user = useAuthStore((s) => s.user);

    return useQuery({
        queryKey: queryKeys.enrollments.continueLearning(),
        queryFn: enrollmentsApi.getContinueLearning,
        enabled: !!user,
    });
}
