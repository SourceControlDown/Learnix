import { useQuery } from '@tanstack/react-query';
import { testsApi } from '@/api/tests.api';
import { queryKeys } from '@/api/queryKeys';

export function useMyTestAttempts(courseId: string, lessonId: string) {
    return useQuery({
        queryKey: queryKeys.tests.attempts(courseId, lessonId),
        queryFn: () => testsApi.getMyAttempts(courseId, lessonId),
        enabled: !!courseId && !!lessonId,
    });
}
