import { useState, useMemo } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Users, BookOpen, FileText, Pencil, Trash2, Globe, EyeOff, Archive } from 'lucide-react';
import { cn } from '@/utils/cn';
import { useMyCoursesQuery } from '@/hooks/useMyCoursesQuery';
import {
    useDeleteCourse,
    usePublishCourse,
    useUnpublishCourse,
    useArchiveCourse,
} from '@/hooks/useCourseMutations';
import { INSTRUCTOR } from '@/const/localization/instructor';
import type { ManageCourseCardDto, CourseStatus } from '@/types/course.types';

const STATUS_STYLES: Record<CourseStatus, string> = {
    Published: 'bg-success/20 text-success',
    Draft: 'bg-muted text-muted-foreground',
    Archived: 'bg-warning/20 text-warning',
};

const STATUS_LABELS: Record<CourseStatus, string> = {
    Published: INSTRUCTOR.STATUS_PUBLISHED,
    Draft: INSTRUCTOR.STATUS_DRAFT,
    Archived: INSTRUCTOR.STATUS_ARCHIVED,
};

function StatCard({ label, value, sub }: { label: string; value: string | number; sub?: string }) {
    return (
        <div className="rounded-xl border border-border bg-card p-5">
            <p className="text-sm text-muted-foreground">{label}</p>
            <p className="mt-1 font-heading text-3xl font-bold text-foreground">{value}</p>
            {sub && <p className="mt-2 text-xs text-muted-foreground">{sub}</p>}
        </div>
    );
}

export default function InstructorDashboardPage() {
    const navigate = useNavigate();
    const [search, setSearch] = useState('');

    const { data, isLoading } = useMyCoursesQuery({ take: 100 });
    const publishMutation = usePublishCourse();
    const unpublishMutation = useUnpublishCourse();
    const archiveMutation = useArchiveCourse();
    const deleteMutation = useDeleteCourse();

    const courses = data?.items ?? [];

    const totalStudents = useMemo(
        () => courses.reduce((sum, c) => sum + c.enrollmentsCount, 0),
        [courses],
    );
    const publishedCount = useMemo(
        () => courses.filter((c) => c.status === 'Published').length,
        [courses],
    );
    const draftCount = useMemo(
        () => courses.filter((c) => c.status !== 'Published').length,
        [courses],
    );

    const filtered = useMemo(() => {
        const q = search.trim().toLowerCase();
        if (!q) return courses;
        return courses.filter((c) => c.title.toLowerCase().includes(q));
    }, [courses, search]);

    function handleDelete(course: ManageCourseCardDto) {
        if (!confirm(`Delete "${course.title}"? This cannot be undone.`)) return;
        deleteMutation.mutate(course.id);
    }

    return (
        <div className="p-8">
            {/* Header */}
            <div className="mb-8 flex items-end justify-between">
                <div>
                    <h1 className="font-heading text-3xl font-bold text-foreground">
                        {INSTRUCTOR.DASHBOARD_TITLE}
                    </h1>
                    <p className="mt-1 text-muted-foreground">{INSTRUCTOR.DASHBOARD_SUBTITLE}</p>
                </div>
                <Link
                    to="/instructor/courses/new"
                    className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                >
                    {INSTRUCTOR.BTN_NEW_COURSE}
                </Link>
            </div>

            {/* Stats */}
            <div className="mb-8 grid gap-4 md:grid-cols-3">
                <StatCard
                    label={INSTRUCTOR.STAT_TOTAL_STUDENTS}
                    value={totalStudents.toLocaleString()}
                />
                <StatCard
                    label={INSTRUCTOR.STAT_PUBLISHED}
                    value={publishedCount}
                    sub={`${draftCount} draft / archived`}
                />
                <StatCard label={INSTRUCTOR.STAT_DRAFT} value={draftCount} />
            </div>

            {/* Courses table */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                <div className="flex items-center justify-between border-b border-border p-5">
                    <h3 className="font-heading font-semibold text-foreground">
                        {INSTRUCTOR.COURSES_TABLE_TITLE}
                    </h3>
                    <input
                        type="text"
                        placeholder={INSTRUCTOR.COURSES_TABLE_SEARCH}
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        className="rounded-lg border border-input bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                </div>

                {isLoading ? (
                    <div className="py-16 text-center text-sm text-muted-foreground">
                        Loading courses...
                    </div>
                ) : filtered.length === 0 ? (
                    <div className="py-16 text-center">
                        <p className="text-sm text-muted-foreground">{INSTRUCTOR.EMPTY_COURSES}</p>
                        <Link
                            to="/instructor/courses/new"
                            className="mt-3 inline-block text-sm text-primary hover:underline"
                        >
                            {INSTRUCTOR.EMPTY_COURSES_CTA}
                        </Link>
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-secondary/50 text-xs uppercase tracking-wider text-muted-foreground">
                            <tr>
                                <th className="px-5 py-3 text-left font-medium">
                                    {INSTRUCTOR.COL_COURSE}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {INSTRUCTOR.COL_STATUS}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {INSTRUCTOR.COL_STUDENTS}
                                </th>
                                <th className="px-5 py-3 text-right font-medium">
                                    {INSTRUCTOR.COL_ACTIONS}
                                </th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-border">
                            {filtered.map((course) => (
                                <tr key={course.id} className="hover:bg-secondary/30">
                                    <td className="px-5 py-3">
                                        <div className="flex items-center gap-3">
                                            <div className="h-10 w-10 shrink-0 overflow-hidden rounded bg-gradient-to-br from-primary/30 to-accent/30">
                                                {course.coverImageUrl && (
                                                    <img
                                                        src={course.coverImageUrl}
                                                        alt=""
                                                        className="h-full w-full object-cover"
                                                    />
                                                )}
                                            </div>
                                            <span className="font-medium text-foreground">
                                                {course.title}
                                            </span>
                                        </div>
                                    </td>
                                    <td className="px-5 py-3">
                                        <span
                                            className={cn(
                                                'rounded px-2 py-0.5 text-xs',
                                                STATUS_STYLES[course.status],
                                            )}
                                        >
                                            {STATUS_LABELS[course.status]}
                                        </span>
                                    </td>
                                    <td className="px-5 py-3 text-muted-foreground">
                                        {course.enrollmentsCount}
                                    </td>
                                    <td className="px-5 py-3">
                                        <div className="flex items-center justify-end gap-2">
                                            <button
                                                onClick={() =>
                                                    navigate(
                                                        `/instructor/courses/${course.id}/edit`,
                                                    )
                                                }
                                                className="rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-primary"
                                                title={INSTRUCTOR.BTN_EDIT}
                                            >
                                                <Pencil size={14} />
                                            </button>
                                            {course.status === 'Draft' && (
                                                <button
                                                    onClick={() =>
                                                        publishMutation.mutate(course.id)
                                                    }
                                                    className="rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                                    title={INSTRUCTOR.BTN_PUBLISH}
                                                >
                                                    <Globe size={14} />
                                                </button>
                                            )}
                                            {course.status === 'Published' && (
                                                <button
                                                    onClick={() =>
                                                        unpublishMutation.mutate(course.id)
                                                    }
                                                    className="rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-warning"
                                                    title={INSTRUCTOR.BTN_UNPUBLISH}
                                                >
                                                    <EyeOff size={14} />
                                                </button>
                                            )}
                                            {course.status !== 'Archived' && (
                                                <button
                                                    onClick={() =>
                                                        archiveMutation.mutate(course.id)
                                                    }
                                                    className="rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-warning"
                                                    title={INSTRUCTOR.BTN_ARCHIVE}
                                                >
                                                    <Archive size={14} />
                                                </button>
                                            )}
                                            <button
                                                onClick={() => handleDelete(course)}
                                                className="rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-destructive"
                                                title={INSTRUCTOR.BTN_DELETE}
                                            >
                                                <Trash2 size={14} />
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}
            </div>
        </div>
    );
}
