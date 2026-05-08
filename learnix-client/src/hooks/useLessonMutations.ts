import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
    lessonsApi,
    type CreateVideoLessonRequest,
    type CreatePostLessonRequest,
    type CreateTestLessonRequest,
} from '@/api/lessons.api';
import { type ReorderItem } from '@/api/sections.api';
import { queryKeys } from '@/api/queryKeys';
import { INSTRUCTOR } from '@/const/localization/instructor';

function invalidateEdit(qc: ReturnType<typeof useQueryClient>, courseId: string) {
    qc.invalidateQueries({ queryKey: queryKeys.instructor.courseForEdit(courseId) });
}

export function useCreateVideoLesson(courseId: string, sectionId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: CreateVideoLessonRequest) =>
            lessonsApi.createVideo(courseId, sectionId, data),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_LESSON_SAVED);
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useCreatePostLesson(courseId: string, sectionId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: CreatePostLessonRequest) =>
            lessonsApi.createPost(courseId, sectionId, data),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_LESSON_SAVED);
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useCreateTestLesson(courseId: string, sectionId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: CreateTestLessonRequest) =>
            lessonsApi.createTest(courseId, sectionId, data),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_LESSON_SAVED);
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useUpdateVideoLesson(courseId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ lessonId, data }: { lessonId: string; data: CreateVideoLessonRequest }) =>
            lessonsApi.updateVideo(courseId, lessonId, data),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_LESSON_SAVED);
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useUpdatePostLesson(courseId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ lessonId, data }: { lessonId: string; data: CreatePostLessonRequest }) =>
            lessonsApi.updatePost(courseId, lessonId, data),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_LESSON_SAVED);
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useUpdateTestLesson(courseId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ lessonId, data }: { lessonId: string; data: CreateTestLessonRequest }) =>
            lessonsApi.updateTest(courseId, lessonId, data),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_LESSON_SAVED);
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useDeleteLesson(courseId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (lessonId: string) => lessonsApi.delete(courseId, lessonId),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_LESSON_DELETED);
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useReorderLessons(courseId: string, sectionId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (items: ReorderItem[]) => lessonsApi.reorder(courseId, sectionId, items),
        onSuccess: () => invalidateEdit(qc, courseId),
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}
