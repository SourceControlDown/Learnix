import { format } from 'date-fns';
import { Lock } from 'lucide-react';
import { cn } from '@/utils/cn';
import { ACHIEVEMENT_META } from '@/const/localization/achievements';

interface AchievementBadgeProps {
    code: string;
    unlockedAt?: string;
    isNew?: boolean;
    onClick?: () => void;
    className?: string;
}

export function AchievementBadge({
    code,
    unlockedAt,
    isNew,
    onClick,
    className,
}: AchievementBadgeProps) {
    const meta = ACHIEVEMENT_META[code];
    const isUnlocked = !!unlockedAt;
    const Icon = meta?.icon ?? Lock;

    return (
        <button
            type="button"
            onClick={onClick}
            className={cn(
                'group relative flex flex-col items-center gap-3 rounded-xl border p-5 text-center transition-all',
                isUnlocked
                    ? 'border-accent/30 bg-accent/5 hover:border-accent/60 hover:bg-accent/10'
                    : 'border-border bg-muted/30 opacity-60',
                onClick && 'cursor-pointer',
                !onClick && 'cursor-default',
                className,
            )}
        >
            {isNew && (
                <span className="absolute right-2 top-2 rounded-full bg-accent px-2 py-0.5 text-xs font-semibold text-accent-foreground">
                    New
                </span>
            )}

            <div
                className={cn(
                    'flex h-14 w-14 items-center justify-center rounded-full',
                    isUnlocked ? 'bg-accent/20 text-accent' : 'bg-muted text-muted-foreground',
                )}
            >
                {isUnlocked ? <Icon className="h-7 w-7" /> : <Lock className="h-6 w-6" />}
            </div>

            <div>
                <p className="font-heading text-sm font-semibold leading-tight text-foreground">
                    {meta?.name ?? code}
                </p>
                <p className="mt-1 text-xs text-muted-foreground">{meta?.description}</p>
                {isUnlocked && unlockedAt && (
                    <p className="mt-2 text-xs text-accent">
                        Earned {format(new Date(unlockedAt), 'MMM d, yyyy')}
                    </p>
                )}
            </div>
        </button>
    );
}
