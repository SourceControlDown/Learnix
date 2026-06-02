import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { coursesApi, type CreateCourseRequest, type UpdateCourseRequest } from '@/api/courses.api';
import { queryKeys } from '@/api/queryKeys';
import { INSTRUCTOR } from '@/const/localization/instructor';

function invalidateMyCourses(qc: ReturnType<typeof useQueryClient>) {
    qc.invalidateQueries({ queryKey: ['courses', 'mine'] });
}

export function useCreateCourse() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: CreateCourseRequest) => coursesApi.create(data),
        onSuccess: () => invalidateMyCourses(qc),
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useUpdateCourse(courseId: string) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: UpdateCourseRequest) => coursesApi.update(courseId, data),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_COURSE_SAVED);
            qc.invalidateQueries({ queryKey: queryKeys.instructor.courseForEdit(courseId) });
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function usePublishCourse() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => coursesApi.publish(courseId),
        onSuccess: (_data, courseId) => {
            toast.success(INSTRUCTOR.TOAST_COURSE_PUBLISHED);
            qc.invalidateQueries({ queryKey: queryKeys.instructor.courseForEdit(courseId) });
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useUnpublishCourse() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => coursesApi.unpublish(courseId),
        onSuccess: (_data, courseId) => {
            toast.success(INSTRUCTOR.TOAST_COURSE_UNPUBLISHED);
            qc.invalidateQueries({ queryKey: queryKeys.instructor.courseForEdit(courseId) });
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useArchiveCourse() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => coursesApi.archive(courseId),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_COURSE_ARCHIVED);
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useUnarchiveCourse() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => coursesApi.unarchive(courseId),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_COURSE_UNARCHIVED);
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}

export function useDeleteCourse() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => coursesApi.deleteCourse(courseId),
        onSuccess: () => {
            toast.success(INSTRUCTOR.TOAST_COURSE_DELETED);
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(INSTRUCTOR.TOAST_ERROR),
    });
}
