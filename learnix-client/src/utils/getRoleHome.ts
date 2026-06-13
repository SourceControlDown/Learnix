import type { UserSummary } from '@/store/auth.store';

export function getRoleHome(roles: UserSummary['roles']): string {
    if (roles.includes('Admin')) return '/admin';
    if (roles.includes('Instructor')) return '/instructor';
    return '/courses';
}
