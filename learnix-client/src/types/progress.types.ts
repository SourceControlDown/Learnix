export interface LessonProgressItemDto {
    lessonId: string;
    title: string;
    lessonType: string;
    displayOrder: number;
    isCompleted: boolean;
    completedAt: string | null;
    lastAccessedAt: string | null;
    /** A video's real length, or a post's estimated reading time. Null for tests. */
    durationSeconds: number | null;
    /** Only for tests — the closest thing a quiz has to a length. */
    questionCount: number | null;
}

export interface SectionProgressDto {
    sectionId: string;
    title: string;
    displayOrder: number;
    lessons: LessonProgressItemDto[];
}

export interface CourseProgressDto {
    courseId: string;
    totalLessons: number;
    completedLessons: number;
    sections: SectionProgressDto[];
}

export interface MarkLessonCompleteResponse {
    lessonProgressId: string;
    isCompleted: boolean;
    completedAt: string | null;
}
