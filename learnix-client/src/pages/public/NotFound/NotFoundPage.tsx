import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Seo } from '@/components/common/seo/Seo';
import { APP_ROUTES } from '@/routes/paths';

export default function NotFoundPage() {
    const { t } = useTranslation('common');

    return (
        <div className="mx-auto flex min-h-[60vh] max-w-md flex-col items-center justify-center px-6 text-center">
            {/* Static hosting can only answer 200 for unknown SPA routes, so `noindex` is what
                actually keeps these URLs out of the index. */}
            <Seo title={t('notFound.title')} noIndex />
            <p className="font-heading text-7xl font-bold text-primary">404</p>
            <h1 className="mt-4 font-heading text-2xl font-semibold">{t('notFound.title')}</h1>
            <p className="mt-3 text-muted-foreground">{t('notFound.subtitle')}</p>
            <Link
                to={APP_ROUTES.public.home}
                className="mt-8 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90"
            >
                {t('notFound.backHome')}
            </Link>
        </div>
    );
}
