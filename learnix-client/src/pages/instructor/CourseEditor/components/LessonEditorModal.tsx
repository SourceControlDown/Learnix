import { useEffect, useRef } from 'react';
import { X } from 'lucide-react';
import { VideoLessonForm } from './VideoLessonForm';
import { PostLessonForm } from './PostLessonForm';
import { TestLessonForm } from './TestLessonForm';
import {
    useCreateVideoLesson,
    useCreatePostLesson,
    useCreateTestLesson,
    useUpdateVideoLesson,
    useUpdatePostLesson,
    useUpdateTestLesson,
} from '@/hooks/useLessonMutations';
import { INSTRUCTOR } from '@/const/localization/instructor';
import type { CourseForEditLessonDto, LessonType } from '@/types/course.types';
import type {
    VideoLessonFormData,
    PostLessonFormData,
    TestLessonFormData,
} from '@/schemas/lesson.schema';

interface Props {
    courseId: string;
    sectionId: string;
    lessonType: LessonType;
    lesson?: CourseForEditLessonDto;
    onClose: () => void;
}

const TITLES: Record<LessonType, { create: string; edit: string }> = {
    Video: { create: INSTRUCTOR.MODAL_NEW_VIDEO, edit: INSTRUCTOR.MODAL_EDIT_VIDEO },
    Post: { create: INSTRUCTOR.MODAL_NEW_POST, edit: INSTRUCTOR.MODAL_EDIT_POST },
    Test: { create: INSTRUCTOR.MODAL_NEW_TEST, edit: INSTRUCTOR.MODAL_EDIT_TEST },
};

export function LessonEditorModal({ courseId, sectionId, lessonType, lesson, onClose }: Props) {
    const overlayRef = useRef<HTMLDivElement>(null);

    const createVideo = useCreateVideoLesson(courseId, sectionId);
    const createPost = useCreatePostLesson(courseId, sectionId);
    const createTest = useCreateTestLesson(courseId, sectionId);
    const updateVideo = useUpdateVideoLesson(courseId);
    const updatePost = useUpdatePostLesson(courseId);
    const updateTest = useUpdateTestLesson(courseId);

    const isEditing = !!lesson;
    const title = TITLES[lessonType][isEditing ? 'edit' : 'create'];

    function handleVideoSubmit(data: VideoLessonFormData) {
        if (isEditing) {
            updateVideo.mutate({ lessonId: lesson.id, data }, { onSuccess: onClose });
        } else {
            createVideo.mutate(data, { onSuccess: onClose });
        }
    }

    function handlePostSubmit(data: PostLessonFormData) {
        if (isEditing) {
            updatePost.mutate({ lessonId: lesson.id, data }, { onSuccess: onClose });
        } else {
            createPost.mutate(data, { onSuccess: onClose });
        }
    }

    function handleTestSubmit(data: TestLessonFormData) {
        if (isEditing) {
            updateTest.mutate({ lessonId: lesson.id, data }, { onSuccess: onClose });
        } else {
            createTest.mutate(data, { onSuccess: onClose });
        }
    }

    useEffect(() => {
        function onKey(e: KeyboardEvent) {
            if (e.key === 'Escape') onClose();
        }
        document.addEventListener('keydown', onKey);
        return () => document.removeEventListener('keydown', onKey);
    }, [onClose]);

    const videoIsPending = createVideo.isPending || updateVideo.isPending;
    const postIsPending = createPost.isPending || updatePost.isPending;
    const testIsPending = createTest.isPending || updateTest.isPending;

    return (
        <div
            ref={overlayRef}
            className="fixed inset-0 z-50 flex items-center justify-center bg-foreground/30 p-4"
            onClick={(e) => {
                if (e.target === overlayRef.current) onClose();
            }}
        >
            <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-xl bg-card shadow-xl">
                <div className="flex items-center justify-between border-b border-border p-5">
                    <h2 className="font-heading font-semibold text-foreground">{title}</h2>
                    <button
                        onClick={onClose}
                        className="text-muted-foreground transition-colors hover:text-foreground"
                    >
                        <X size={18} />
                    </button>
                </div>
                <div className="p-5">
                    {lessonType === 'Video' && (
                        <VideoLessonForm
                            lesson={lesson}
                            isPending={videoIsPending}
                            onSubmit={handleVideoSubmit}
                            onCancel={onClose}
                        />
                    )}
                    {lessonType === 'Post' && (
                        <PostLessonForm
                            lesson={lesson}
                            isPending={postIsPending}
                            onSubmit={handlePostSubmit}
                            onCancel={onClose}
                        />
                    )}
                    {lessonType === 'Test' && (
                        <TestLessonForm
                            lesson={lesson}
                            isPending={testIsPending}
                            onSubmit={handleTestSubmit}
                            onCancel={onClose}
                        />
                    )}
                </div>
            </div>
        </div>
    );
}
