import { useQuery } from '@tanstack/react-query';
import { reviewsApi } from '@/api/reviews.api';
import { queryKeys } from '@/api/queryKeys';

export function useCourseReviews(courseId: string, skip = 0, take = 20) {
    return useQuery({
        queryKey: queryKeys.reviews.byCourse(courseId),
        queryFn: () => reviewsApi.getCourseReviews(courseId, skip, take),
        enabled: !!courseId,
    });
}
