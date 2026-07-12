import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { User } from 'lucide-react';
import { CourseCard } from '@/components/common/course/CourseCard';
import { Seo } from '@/components/common/seo/Seo';
import { QueryError } from '@/components/common/system/QueryError';
import { BackLink } from '@/components/common/ui/BackLink';
import { TextLink } from '@/components/common/ui/TextLink';
import { useInstructorCourses } from '@/hooks/instructor/useInstructorCourses';
import { useUserProfile } from '@/hooks/user/useUserProfile';
import { APP_ROUTES } from '@/routes/paths';
import { isNotFoundError } from '@/utils/errors';

export default function InstructorProfilePage() {
    const { t } = useTranslation('instructorProfile');
    const { instructorId } = useParams<{ instructorId: string }>();

    const {
        data: profile,
        isLoading: profileLoading,
        error: profileError,
        refetch: refetchProfile,
    } = useUserProfile(instructorId!);
    const {
        data: coursesData,
        isLoading: coursesLoading,
        isError: coursesFailed,
        refetch: refetchCourses,
    } = useInstructorCourses(instructorId!);

    const isLoading = profileLoading || coursesLoading;
    const profileMissing = isNotFoundError(profileError);
    const courses = coursesData?.items ?? [];

    if (isLoading) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-12">
                <div className="animate-pulse space-y-6">
                    <div className="flex items-center gap-5">
                        <div className="size-20 rounded-full bg-muted" />
                        <div className="space-y-2">
                            <div className="h-6 w-48 rounded bg-muted" />
                            <div className="h-4 w-64 rounded bg-muted" />
                        </div>
                    </div>
                    <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                        {Array.from({ length: 3 }).map((_, i) => (
                            <div key={i} className="aspect-[4/3] rounded-xl bg-muted" />
                        ))}
                    </div>
                </div>
            </div>
        );
    }

    if (profileMissing) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-20 text-center">
                <Seo title={t('notFound')} noIndex />
                <p className="text-muted-foreground">{t('notFound')}</p>
                <TextLink to={APP_ROUTES.public.courses} className="mt-4 inline-block">
                    {t('common:actions.backToCatalog')}
                </TextLink>
            </div>
        );
    }

    if (!profile) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-12">
                <Seo title={t('error')} noIndex />
                <BackLink
                    fallbackTo={APP_ROUTES.public.courses}
                    fallbackLabel={t('common:actions.backToCatalog')}
                    className="mb-8"
                />
                <QueryError
                    message={t('error')}
                    onRetry={refetchProfile}
                    retryLabel={t('common:actions.tryAgain')}
                />
            </div>
        );
    }

    const fullName = `${profile.firstName} ${profile.lastName}`;

    return (
        <div className="mx-auto max-w-5xl px-6 py-12">
            <Seo
                title={t('seo.title', { name: fullName })}
                description={profile.bio || t('seo.description', { name: fullName })}
                image={profile.avatarUrl}
                type="profile"
                canonicalPath={APP_ROUTES.public.instructorProfile(instructorId!)}
            />
            <BackLink
                fallbackTo={APP_ROUTES.public.courses}
                fallbackLabel={t('common:actions.backToCatalog')}
                className="mb-8"
            />

            {/* Profile header */}
            <div className="mb-10 flex items-start gap-6">
                <div className="shrink-0">
                    {profile.avatarUrl ? (
                        <img
                            src={profile.avatarUrl}
                            alt={fullName}
                            className="size-20 rounded-full object-cover ring-2 ring-border"
                        />
                    ) : (
                        <div className="flex size-20 items-center justify-center rounded-full bg-muted ring-2 ring-border">
                            <User className="size-10 text-muted-foreground" />
                        </div>
                    )}
                </div>
                <div>
                    <h1 className="font-heading text-2xl font-bold text-foreground">{fullName}</h1>
                    {profile.bio && (
                        <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
                            {profile.bio}
                        </p>
                    )}
                    {!coursesFailed && (
                        <p className="mt-2 text-sm text-muted-foreground">
                            {t('coursesCount', { count: courses.length })}
                        </p>
                    )}
                </div>
            </div>

            {/* Courses */}
            <section>
                <h2 className="mb-5 font-heading text-xl font-semibold text-foreground">
                    {t('coursesHeading')}
                </h2>

                {coursesFailed ? (
                    <QueryError
                        message={t('coursesError')}
                        onRetry={refetchCourses}
                        retryLabel={t('common:actions.tryAgain')}
                    />
                ) : courses.length === 0 ? (
                    <p className="text-sm text-muted-foreground">{t('noCourses')}</p>
                ) : (
                    <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                        {courses.map((course) => (
                            <CourseCard key={course.id} course={course} />
                        ))}
                    </div>
                )}
            </section>
        </div>
    );
}
