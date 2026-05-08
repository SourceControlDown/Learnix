import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
    instructorApplicationsApi,
    type SubmitApplicationRequest,
} from '@/api/instructorApplications.api';
import { queryKeys } from '@/api/queryKeys';
import { INSTRUCTOR } from '@/const/localization/instructor';

export function useSubmitApplication() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: SubmitApplicationRequest) => instructorApplicationsApi.submit(data),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_APPLICATION_SUBMITTED);
            qc.invalidateQueries({ queryKey: queryKeys.applications.mine() });
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}
