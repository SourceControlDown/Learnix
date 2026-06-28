import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { enrollmentsApi } from '@/api/enrollments.api';
import { queryKeys } from '@/api/queryKeys';

export function useEnroll() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (courseId: string) => enrollmentsApi.enroll(courseId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.enrollments.mine() });
            toast.success('Successfully enrolled!');
        },
    });
}
