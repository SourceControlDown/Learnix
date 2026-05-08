export const queryKeys = {
    courses: {
        all: ['courses'] as const,
        lists: () => [...queryKeys.courses.all, 'list'] as const,
        list: (filters: Record<string, unknown>) =>
            [...queryKeys.courses.lists(), filters] as const,
        featured: () => [...queryKeys.courses.all, 'featured'] as const,
        details: () => [...queryKeys.courses.all, 'detail'] as const,
        detail: (id: string) => [...queryKeys.courses.details(), id] as const,
    },
    categories: {
        all: ['categories'] as const,
        lists: () => [...queryKeys.categories.all, 'list'] as const,
    },
    lessons: {
        all: ['lessons'] as const,
        content: (courseId: string, lessonId: string) =>
            [...queryKeys.lessons.all, 'content', courseId, lessonId] as const,
    },
    progress: {
        all: ['progress'] as const,
        course: (courseId: string) => [...queryKeys.progress.all, 'course', courseId] as const,
    },
    tests: {
        all: ['tests'] as const,
        lesson: (courseId: string, lessonId: string) =>
            [...queryKeys.tests.all, 'lesson', courseId, lessonId] as const,
        attempts: (courseId: string, lessonId: string) =>
            [...queryKeys.tests.all, 'attempts', courseId, lessonId] as const,
    },
} as const;
