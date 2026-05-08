import { api } from './axios.instance';
import type { LessonContentDto } from '@/types/lesson.types';

export const lessonsApi = {
    getContent: (courseId: string, lessonId: string) =>
        api.get<LessonContentDto>(`/courses/${courseId}/lessons/${lessonId}`).then((r) => r.data),
};
