import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Heart } from 'lucide-react';
import { CountBadge } from '@/components/common/ui/CountBadge';
import { useWishlistCount } from '@/hooks/student/useWishlistCount';
import { APP_ROUTES } from '@/routes/paths';
import { cn } from '@/utils/cn';

export function WishlistButton() {
    const { t } = useTranslation('header');
    const { data } = useWishlistCount();

    const count = data?.count ?? 0;

    return (
        <Link
            to={APP_ROUTES.student.wishlist}
            aria-label={t('common:navigation.wishlist')}
            className={cn(
                'relative inline-flex items-center justify-center rounded-md p-2',
                'text-muted-foreground transition-colors hover:bg-muted hover:text-foreground',
            )}
        >
            <Heart className="size-5" />
            <CountBadge count={count} />
        </Link>
    );
}
