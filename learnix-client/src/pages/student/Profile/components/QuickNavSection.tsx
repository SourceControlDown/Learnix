import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Award, ChevronRight, GraduationCap } from 'lucide-react';
import { APP_ROUTES } from '@/routes/paths';

interface QuickNavSectionProps {
    /** False for instructors and admins — neither can submit an application. */
    canBecomeInstructor: boolean;
}

export function QuickNavSection({ canBecomeInstructor }: QuickNavSectionProps) {
    const { t } = useTranslation('profile');

    return (
        <div className="space-y-4">
            <Link
                to={APP_ROUTES.student.certificates}
                className="group flex items-center gap-4 rounded-xl border border-warning/20 bg-warning/10 p-4 transition-all hover:bg-warning/20 sm:p-5"
            >
                <div className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-warning/20 transition-transform group-hover:scale-110">
                    <Award className="size-5 text-warning" />
                </div>
                <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium text-foreground">
                        {t('common:navigation.certificates')}
                    </p>
                    <p className="text-xs text-muted-foreground">{t('certificatesNav.desc')}</p>
                </div>
                <ChevronRight className="size-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-1" />
            </Link>

            {canBecomeInstructor && (
                <Link
                    to={APP_ROUTES.public.becomeInstructor}
                    className="group flex items-center gap-4 rounded-xl border border-brand/20 bg-brand/10 p-4 transition-all hover:bg-brand/20 sm:p-5"
                >
                    <div className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-brand/20 transition-transform group-hover:scale-110">
                        <GraduationCap className="size-5 text-brand" />
                    </div>
                    <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium text-foreground">
                            {t('becomeInstructorNav.title')}
                        </p>
                        <p className="text-xs text-muted-foreground">
                            {t('becomeInstructorNav.desc')}
                        </p>
                    </div>
                    <ChevronRight className="size-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-1" />
                </Link>
            )}
        </div>
    );
}
