import type { PaginatedResult } from '@/types/api.types';
import type { ContinueLearningDto, EnrolledCourseDto } from '@/types/enrollment.types';
import { api } from './axios.instance';

export const enrollmentsApi = {
    getMyEnrollments: (skip = 0, take = 100) =>
        api
            .get<PaginatedResult<EnrolledCourseDto>>('/enrollments/mine', {
                params: { skip, take },
            })
            .then((r) => r.data),

    enroll: (courseId: string) =>
        api.post<{ enrollmentId: string }>('/enrollments', { courseId }).then((r) => r.data),

    /** The backend answers 204 when there is no course in progress. */
    getContinueLearning: () =>
        api
            .get<ContinueLearningDto>('/enrollments/continue')
            .then((r) => (r.status === 204 ? null : r.data)),
};
