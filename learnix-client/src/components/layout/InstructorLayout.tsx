import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { AiChatWidget } from '@/components/common/AiChatWidget/AiChatWidget';
import { useNotificationsHub } from '@/hooks/useNotificationsHub';
import {
    LayoutDashboard,
    BookOpen,
    PlusCircle,
    MessageSquare,
    TrendingUp,
    ArrowLeft,
    LogOut,
} from 'lucide-react';
import { Helmet } from 'react-helmet-async';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';
import { useAuthStore } from '@/store/auth.store';
import { authApi } from '@/api/auth.api';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';
import { Logo } from '@/components/common/Logo';

export function InstructorLayout() {
    const { t } = useTranslation('instructor');
    useNotificationsHub();
    const navigate = useNavigate();
    const { logout } = useAuthStore();
    const queryClient = useQueryClient();

    const { data: unreadData } = useQuery({
        queryKey: queryKeys.messages.unreadCount(),
        queryFn: messagesApi.getUnreadCount,
        staleTime: Infinity,
    });
    const unreadCount = unreadData?.totalUnread ?? 0;

    const navItems = [
        {
            to: '/instructor',
            label: t('navDashboard'),
            icon: <LayoutDashboard size={16} />,
            end: true,
        },
        {
            to: '/instructor/courses',
            label: t('navMyCourses'),
            icon: <BookOpen size={16} />,
            end: true,
        },
        {
            to: '/instructor/courses/new',
            label: t('navNewCourse'),
            icon: <PlusCircle size={16} />,
        },
        {
            to: '/instructor/messages',
            label: t('navMessages'),
            icon: <MessageSquare size={16} />,
        },
        { to: '/instructor/earnings', label: t('navEarnings'), icon: <TrendingUp size={16} /> },
    ];

    function handleSignOut() {
        authApi.logout().catch(() => {});
        logout();
        queryClient.clear();
        navigate('/login');
    }

    return (
        <>
            <Helmet>
                <meta name="robots" content="noindex,nofollow" />
            </Helmet>
            <div className="grid h-screen grid-cols-[240px_1fr] overflow-hidden bg-background">
                {/* Sidebar */}
                <aside className="flex flex-col border-r border-border bg-card">
                    <div className="flex items-center gap-2 px-4 py-5">
                        <Link
                            to="/"
                            className="flex items-center gap-2.5 font-heading font-bold text-foreground transition-opacity hover:opacity-90"
                        >
                            <div className="grid h-8 w-8 place-items-center rounded-lg bg-primary text-primary-foreground shadow-sm">
                                <Logo className="h-6 w-6" />
                            </div>
                            <span className="tracking-tight">Learnix</span>
                        </Link>
                    </div>

                    <div className="flex-1 px-3 py-2">
                        <p className="mb-2 px-2 text-xs uppercase tracking-wider text-muted-foreground">
                            Instructor
                        </p>
                        <nav className="space-y-1 text-sm">
                            {navItems.map((item) => (
                                <NavLink
                                    key={item.to}
                                    to={item.to}
                                    end={item.end}
                                    className={({ isActive }) =>
                                        cn(
                                            'flex items-center gap-2.5 rounded-lg px-3 py-2 transition-colors',
                                            isActive
                                                ? 'bg-primary/10 font-medium text-primary'
                                                : 'text-foreground hover:bg-secondary',
                                        )
                                    }
                                >
                                    {item.icon}
                                    {item.label}
                                    {item.to === '/instructor/messages' && unreadCount > 0 && (
                                        <span className="ml-auto flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-bold text-destructive-foreground">
                                            {unreadCount > 99 ? '99+' : unreadCount}
                                        </span>
                                    )}
                                </NavLink>
                            ))}
                        </nav>
                    </div>

                    <div className="border-t border-border px-3 py-4">
                        <p className="mb-2 px-2 text-xs uppercase tracking-wider text-muted-foreground">
                            Account
                        </p>
                        <nav className="space-y-1 text-sm">
                            <Link
                                to="/"
                                className="flex items-center gap-2.5 rounded-lg px-3 py-2 text-foreground transition-colors hover:bg-secondary"
                            >
                                <ArrowLeft size={16} />
                                {t('navBackToCatalog')}
                            </Link>
                            <button
                                onClick={handleSignOut}
                                className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                            >
                                <LogOut size={16} />
                                {t('navSignOut')}
                            </button>
                        </nav>
                    </div>
                </aside>

                {/* Main content */}
                <main className="h-full overflow-y-auto">
                    <Outlet />
                </main>
            </div>
            <AiChatWidget />
        </>
    );
}
