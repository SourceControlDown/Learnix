import { Link } from 'react-router-dom';
import { CheckCircle2, Circle, PlayCircle, FileText, ClipboardList } from 'lucide-react';
import { cn } from '@/utils/cn';
import type { SectionProgressDto } from '@/types/progress.types';
import { LESSON_PLAYER } from '@/const/localization/lessonPlayer';

interface CourseSidebarProps {
    sections: SectionProgressDto[];
    currentLessonId: string;
    courseId: string;
    totalLessons: number;
    completedLessons: number;
}

const lessonTypeIcon = {
    Video: PlayCircle,
    Post: FileText,
    Test: ClipboardList,
};

export function CourseSidebar({
    sections,
    currentLessonId,
    courseId,
    totalLessons,
    completedLessons,
}: CourseSidebarProps) {
    const progressPct = totalLessons > 0 ? Math.round((completedLessons / totalLessons) * 100) : 0;

    return (
        <aside className="flex w-72 shrink-0 flex-col overflow-hidden border-r border-border bg-card">
            <div className="border-b border-border p-4">
                <p className="mb-2 text-xs font-medium text-muted-foreground">
                    {LESSON_PLAYER.SIDEBAR.progressLabel(completedLessons, totalLessons)}
                </p>
                <div className="h-1.5 w-full overflow-hidden rounded-full bg-secondary">
                    <div
                        className="h-full rounded-full bg-primary transition-all duration-300"
                        style={{ width: `${progressPct}%` }}
                    />
                </div>
            </div>

            <nav className="flex-1 overflow-y-auto py-2">
                {sections.map((section, sIdx) => (
                    <div key={section.sectionId} className="mb-1">
                        <button
                            className="flex w-full items-center gap-2 px-4 py-2 text-left"
                            type="button"
                        >
                            <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                                {LESSON_PLAYER.SIDEBAR.sectionPrefix} {sIdx + 1} · {section.title}
                            </span>
                        </button>

                        <ul>
                            {section.lessons
                                .slice()
                                .sort((a, b) => a.displayOrder - b.displayOrder)
                                .map((lesson) => {
                                    const isActive = lesson.lessonId === currentLessonId;
                                    const Icon =
                                        lessonTypeIcon[
                                            lesson.lessonType as keyof typeof lessonTypeIcon
                                        ] ?? FileText;

                                    return (
                                        <li key={lesson.lessonId}>
                                            <Link
                                                to={`/courses/${courseId}/learn/${lesson.lessonId}`}
                                                className={cn(
                                                    'flex items-center gap-3 px-4 py-2.5 text-sm transition-colors hover:bg-secondary',
                                                    isActive
                                                        ? 'border-l-2 border-primary bg-secondary font-medium text-foreground'
                                                        : 'text-muted-foreground',
                                                )}
                                            >
                                                <span className="shrink-0">
                                                    {lesson.isCompleted ? (
                                                        <CheckCircle2 className="h-4 w-4 text-success" />
                                                    ) : (
                                                        <Circle className="h-4 w-4" />
                                                    )}
                                                </span>
                                                <Icon className="h-4 w-4 shrink-0 opacity-60" />
                                                <span className="line-clamp-2 leading-snug">
                                                    {lesson.title}
                                                </span>
                                            </Link>
                                        </li>
                                    );
                                })}
                        </ul>
                    </div>
                ))}
            </nav>
        </aside>
    );
}
