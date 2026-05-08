import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { sectionsApi, type ReorderItem } from '@/api/sections.api';
import { queryKeys } from '@/api/queryKeys';
import { INSTRUCTOR } from '@/const/localization/instructor';

function invalidateEdit(qc: ReturnType<typeof useQueryClient>, courseId: string) {
    qc.invalidateQueries({ queryKey: queryKeys.instructor.courseForEdit(courseId) });
}

export function useCreateSection(courseId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (title: string) => sectionsApi.create(courseId, title),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_SECTION_ADDED);
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useUpdateSectionTitle(courseId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ sectionId, title }: { sectionId: string; title: string }) =>
            sectionsApi.updateTitle(courseId, sectionId, title),
        onSuccess: () => invalidateEdit(qc, courseId),
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useDeleteSection(courseId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (sectionId: string) => sectionsApi.delete(courseId, sectionId),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_SECTION_DELETED);
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useReorderSections(courseId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (items: ReorderItem[]) => sectionsApi.reorder(courseId, items),
        onSuccess: () => invalidateEdit(qc, courseId),
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}
