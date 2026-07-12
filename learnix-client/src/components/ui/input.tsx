import * as React from 'react';
import { FIELD_BASE, FIELD_SURFACE_CARD } from '@/components/common/form/fieldStyles';
import { cn } from '@/utils/cn';

interface InputProps extends React.ComponentProps<'input'> {
    /** Surface the input sits on: `default` (page) or `card` (inside a card). */
    variant?: 'default' | 'card';
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
    ({ className, type, variant = 'default', ...props }, ref) => {
        return (
            <input
                type={type}
                className={cn(
                    FIELD_BASE,
                    'px-3 py-2 file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground',
                    variant === 'card' && FIELD_SURFACE_CARD,
                    className,
                )}
                ref={ref}
                {...props}
            />
        );
    },
);
Input.displayName = 'Input';

export { Input };
