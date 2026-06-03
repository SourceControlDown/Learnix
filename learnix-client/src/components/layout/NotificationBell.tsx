import { Bell } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/api/queryKeys';
import { notificationsApi } from '@/api/notifications.api';
import { messagesApi } from '@/api/messages.api';
import { useAuthStore } from '@/store/auth.store';
import { cn } from '@/utils/cn';

export function NotificationBell() {
    const { t } = useTranslation('header');
    const user = useAuthStore((s) => s.user);

    const { data: notifData } = useQuery({
        queryKey: queryKeys.notifications.unreadCount(),
        queryFn: notificationsApi.getUnreadCount,
        enabled: !!user,
        staleTime: Infinity,
    });

    const { data: messagesData } = useQuery({
        queryKey: queryKeys.messages.unreadCount(),
        queryFn: messagesApi.getUnreadCount,
        enabled: !!user,
        staleTime: Infinity,
    });

    const totalUnread = (notifData?.count ?? 0) + (messagesData?.totalUnread ?? 0);

    return (
        <Link
            to="/notifications"
            className={cn(
                'relative inline-flex items-center justify-center rounded-md p-2',
                'text-muted-foreground transition-colors hover:bg-muted hover:text-foreground',
            )}
            aria-label={t('notificationsAriaLabel')}
        >
            <Bell className="h-5 w-5" />
            {totalUnread > 0 && (
                <span className="absolute right-1 top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-bold text-destructive-foreground">
                    {totalUnread > 99 ? '99+' : totalUnread}
                </span>
            )}
        </Link>
    );
}
