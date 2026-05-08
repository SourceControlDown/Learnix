import { api } from './axios.instance';
import type {
    GetTestLessonDto,
    SubmitAttemptRequest,
    SubmitAttemptResponse,
    TestAttemptSummaryDto,
} from '@/types/lesson.types';

export const testsApi = {
    getTestLesson: (courseId: string, lessonId: string) =>
        api
            .get<GetTestLessonDto>(`/courses/${courseId}/lessons/${lessonId}/test`)
            .then((r) => r.data),

    submitAttempt: (courseId: string, lessonId: string, data: SubmitAttemptRequest) =>
        api
            .post<SubmitAttemptResponse>(
                `/courses/${courseId}/lessons/${lessonId}/test/attempts`,
                data,
            )
            .then((r) => r.data),

    getMyAttempts: (courseId: string, lessonId: string) =>
        api
            .get<TestAttemptSummaryDto[]>(`/courses/${courseId}/lessons/${lessonId}/test/attempts`)
            .then((r) => r.data),
};
