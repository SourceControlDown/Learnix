import { useLessonContent } from '@/hooks/useLessonContent';
import type { LessonProgressItemDto } from '@/types/progress.types';

interface VideoLessonViewProps {
    lesson: LessonProgressItemDto;
    courseId: string;
}

export function VideoLessonView({ lesson, courseId }: VideoLessonViewProps) {
    const { data, isLoading } = useLessonContent(courseId, lesson.lessonId);

    return (
        <div className="mx-auto max-w-5xl">
            {isLoading && (
                <div className="flex aspect-video w-full animate-pulse items-center justify-center rounded-xl bg-secondary" />
            )}

            {!isLoading && data?.videoUrl && (
                <video
                    key={data.videoUrl}
                    controls
                    className="w-full aspect-video rounded-xl bg-black shadow-lg"
                    preload="metadata"
                >
                    <source src={data.videoUrl} />
                    Your browser does not support the video element.
                </video>
            )}

            {!isLoading && !data?.videoUrl && (
                <div className="flex aspect-video w-full items-center justify-center rounded-xl border border-dashed border-border bg-secondary/40 text-sm text-muted-foreground">
                    No video attached to this lesson yet.
                </div>
            )}

            <div className="mt-8">
                <h1 className="font-heading text-2xl font-bold text-foreground">{lesson.title}</h1>
                {!isLoading && data?.description && (
                    <p className="mt-3 text-base leading-relaxed text-muted-foreground">
                        {data.description}
                    </p>
                )}
            </div>
        </div>
    );
}
