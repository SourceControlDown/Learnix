import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { videoLessonSchema, type VideoLessonFormData } from '@/schemas/lesson.schema';
import { VideoUploader } from './VideoUploader';
import { INSTRUCTOR } from '@/const/localization/instructor';
import type { CourseForEditLessonDto } from '@/types/course.types';

interface Props {
    lesson?: CourseForEditLessonDto;
    isPending: boolean;
    onSubmit: (data: VideoLessonFormData) => void;
    onCancel: () => void;
}

export function VideoLessonForm({ lesson, isPending, onSubmit, onCancel }: Props) {
    const {
        register,
        handleSubmit,
        setValue,
        watch,
        formState: { errors },
    } = useForm<VideoLessonFormData>({
        resolver: zodResolver(videoLessonSchema),
        defaultValues: {
            title: lesson?.title ?? '',
            videoUrl: lesson?.videoUrl ?? '',
            description: lesson?.description ?? '',
            durationSeconds: lesson?.durationSeconds ?? undefined,
        },
    });

    const videoUrl = watch('videoUrl');

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div>
                <label className="mb-1 block text-sm font-medium">{INSTRUCTOR.FIELD_TITLE}</label>
                <input
                    {...register('title')}
                    placeholder={INSTRUCTOR.FIELD_TITLE_PLACEHOLDER}
                    className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                {errors.title && (
                    <p className="mt-1 text-xs text-destructive">{errors.title.message}</p>
                )}
            </div>

            <VideoUploader
                value={videoUrl}
                onChange={(path) => setValue('videoUrl', path, { shouldValidate: true })}
            />
            {errors.videoUrl && (
                <p className="text-xs text-destructive">{errors.videoUrl.message}</p>
            )}

            <div>
                <label className="mb-1 block text-sm font-medium">
                    {INSTRUCTOR.FIELD_DESCRIPTION}
                </label>
                <textarea
                    {...register('description')}
                    rows={3}
                    className="w-full resize-none rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
            </div>

            <div>
                <label className="mb-1 block text-sm font-medium">
                    {INSTRUCTOR.FIELD_DURATION}
                </label>
                <input
                    {...register('durationSeconds')}
                    type="number"
                    min={1}
                    className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
            </div>

            <div className="flex justify-end gap-2 pt-2">
                <button
                    type="button"
                    onClick={onCancel}
                    className="rounded-lg border border-border px-4 py-2 text-sm hover:bg-secondary"
                >
                    {INSTRUCTOR.BTN_CANCEL}
                </button>
                <button
                    type="submit"
                    disabled={isPending}
                    className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
                >
                    {isPending ? '...' : INSTRUCTOR.BTN_SAVE_LESSON}
                </button>
            </div>
        </form>
    );
}
