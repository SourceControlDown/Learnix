import { useRef, useEffect } from 'react';
import { useLessonContent } from '@/hooks/useLessonContent';
import type { LessonProgressItemDto } from '@/types/progress.types';

interface VideoLessonViewProps {
    lesson: LessonProgressItemDto;
    courseId: string;
    /** Called when the video crosses the auto-complete threshold (mark as done, no redirect). */
    onVideoNearEnd?: () => void;
    /** Called when the video plays all the way to the end (redirect to next lesson). */
    onVideoFullyEnded?: () => void;
}

export function VideoLessonView({ lesson, courseId, onVideoNearEnd, onVideoFullyEnded }: VideoLessonViewProps) {
    const { data, isLoading } = useLessonContent(courseId, lesson.lessonId);
    const hasAutoCompleted = useRef(false);

    useEffect(() => {
        hasAutoCompleted.current = false;
    }, [lesson.lessonId]);

    const handleTimeUpdate = (e: React.SyntheticEvent<HTMLVideoElement>) => {
        if (!onVideoNearEnd || hasAutoCompleted.current) return;

        const video = e.currentTarget;
        if (!video.duration) return;

        const timeRemaining = video.duration - video.currentTime;

        // Threshold: the lesser of 120 s and 20 % of the video duration.
        // Example: 10 s video → threshold = min(120, 2) = 2 s → fires at ~8 s.
        // Example: 30 min video → threshold = min(120, 360) = 120 s → fires in last 2 min.
        const threshold = Math.min(120, video.duration * 0.2);

        if (timeRemaining <= threshold) {
            hasAutoCompleted.current = true;
            onVideoNearEnd(); // mark complete – no navigation
        }
    };

    return (
        <div className="mx-auto max-w-5xl">
            {isLoading && (
                <div className="flex aspect-video w-full animate-pulse items-center justify-center rounded-xl bg-secondary" />
            )}

            {!isLoading && data?.videoUrl && (
                <video
                    key={data.videoUrl}
                    controls
                    className="aspect-video w-full rounded-xl bg-black shadow-lg"
                    preload="metadata"
                    onEnded={() => {
                        // Ensure completion is marked even if timeupdate missed it.
                        if (!hasAutoCompleted.current && onVideoNearEnd) {
                            hasAutoCompleted.current = true;
                            onVideoNearEnd();
                        }
                        // Navigate to the next lesson only when the video truly ends.
                        onVideoFullyEnded?.();
                    }}
                    onTimeUpdate={handleTimeUpdate}
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
