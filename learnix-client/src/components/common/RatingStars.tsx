import { Star } from 'lucide-react';
import { cn } from '@/utils/cn';

interface RatingStarsProps {
    value: number;
    max?: number;
    onChange?: (value: number) => void;
    size?: 'sm' | 'md' | 'lg';
    className?: string;
}

const SIZE_MAP = {
    sm: 'h-3 w-3',
    md: 'h-5 w-5',
    lg: 'h-7 w-7',
};

export function RatingStars({
    value,
    max = 5,
    onChange,
    size = 'md',
    className,
}: RatingStarsProps) {
    const isInteractive = !!onChange;

    return (
        <div className={cn('flex items-center gap-0.5', className)}>
            {Array.from({ length: max }, (_, i) => {
                const starValue = i + 1;
                const filled = starValue <= value;

                return (
                    <button
                        key={i}
                        type="button"
                        disabled={!isInteractive}
                        onClick={() => onChange?.(starValue)}
                        className={cn(
                            'transition-colors',
                            isInteractive && 'cursor-pointer hover:scale-110',
                            !isInteractive && 'cursor-default',
                        )}
                        aria-label={`${starValue} star${starValue !== 1 ? 's' : ''}`}
                    >
                        <Star
                            className={cn(
                                SIZE_MAP[size],
                                filled
                                    ? 'fill-warning text-warning'
                                    : 'fill-none text-muted-foreground/40',
                            )}
                        />
                    </button>
                );
            })}
        </div>
    );
}
