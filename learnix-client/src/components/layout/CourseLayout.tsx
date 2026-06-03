import { Outlet } from 'react-router-dom';
import { useNotificationsHub } from '@/hooks/useNotificationsHub';

export function CourseLayout() {
    useNotificationsHub();
    return <Outlet />;
}
