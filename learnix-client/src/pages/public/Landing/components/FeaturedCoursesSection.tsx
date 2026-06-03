import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import type { CourseSummaryDto } from '@/types/course.types';
import { CourseCard } from '@/components/common/CourseCard';

interface FeaturedCoursesSectionProps {
    courses: CourseSummaryDto[];
    isLoading?: boolean;
    totalCount?: number;
}

export function FeaturedCoursesSection({
    courses,
    isLoading,
    totalCount,
}: FeaturedCoursesSectionProps) {
    const { t } = useTranslation('landing');

    return (
        <section id="courses" className="bg-secondary/40 py-20">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-10 flex items-end justify-between">
                    <div>
                        <span className="text-sm font-semibold text-primary">
                            {t('featuredCourses.tag')}
                        </span>
                        <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                            {t('featuredCourses.heading')}
                        </h2>
                        <p className="mt-2 text-muted-foreground">
                            {t('featuredCourses.subtitle')}
                        </p>
                    </div>
                    <Link to="/courses" className="text-sm text-primary hover:underline">
                        {t('featuredCourses.viewAll')}
                    </Link>
                </div>

                {isLoading ? (
                    <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                        {Array.from({ length: 6 }).map((_, i) => (
                            <div
                                key={i}
                                className="h-72 animate-pulse rounded-xl border border-border bg-card"
                            />
                        ))}
                    </div>
                ) : (
                    <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                        {courses.map((course) => (
                            <CourseCard key={course.id} course={course} />
                        ))}
                    </div>
                )}

                <div className="mt-10 text-center">
                    <Link
                        to="/courses"
                        className="inline-flex items-center gap-2 font-medium text-primary hover:underline"
                    >
                        {totalCount !== undefined
                            ? t('featuredCourses.viewMore', { count: totalCount })
                            : t('featuredCourses.viewAll')}
                    </Link>
                </div>
            </div>
        </section>
    );
}
