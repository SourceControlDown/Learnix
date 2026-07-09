import { Link, type LinkProps } from 'react-router-dom';
import { cn } from '@/utils/cn';

export function TextLink({ className, children, ...props }: LinkProps) {
    return (
        <Link
            className={cn(
                'rounded-sm font-medium text-link transition-colors hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-link focus-visible:ring-offset-2 focus-visible:ring-offset-background',
                className,
            )}
            {...props}
        >
            {children}
        </Link>
    );
}
