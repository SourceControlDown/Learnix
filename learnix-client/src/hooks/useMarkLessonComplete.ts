import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { progressApi } from '@/api/progress.api';
import { queryKeys } from '@/api/queryKeys';

export function useMarkLessonComplete(courseId: string) {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (lessonId: string) => progressApi.markLessonComplete(courseId, lessonId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.progress.course(courseId) });
            toast.success('Lesson marked as complete');
        },
    });
}
