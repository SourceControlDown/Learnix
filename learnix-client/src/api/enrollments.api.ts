import { api } from './axios.instance';
import type { EnrolledCourseDto } from '@/types/enrollment.types';
import type { PaginatedResult } from '@/types/api.types';

export const enrollmentsApi = {
    getMyEnrollments: (skip = 0, take = 100) =>
        api
            .get<PaginatedResult<EnrolledCourseDto>>('/enrollments/mine', {
                params: { skip, take },
            })
            .then((r) => r.data),

    enroll: (courseId: string) =>
        api.post<{ enrollmentId: string }>('/enrollments', { courseId }).then((r) => r.data),
};
