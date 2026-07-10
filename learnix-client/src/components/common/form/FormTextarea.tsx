import React, { forwardRef } from 'react';
import { type VariantProps, cva } from 'class-variance-authority';
import { FIELD_BASE, FIELD_ERROR, FIELD_SURFACE_CARD } from '@/components/common/form/fieldStyles';
import { cn } from '@/utils/cn';

// Border/focus/fill come from the shared field tokens; the only choice is the surface.
const formTextareaVariants = cva(`${FIELD_BASE} resize-none px-3 py-2.5`, {
    variants: {
        variant: {
            default: '',
            card: FIELD_SURFACE_CARD,
        },
        hasError: {
            true: FIELD_ERROR,
            false: '',
        },
    },
    defaultVariants: {
        variant: 'default',
        hasError: false,
    },
});

interface FormTextareaProps
    extends
        React.TextareaHTMLAttributes<HTMLTextAreaElement>,
        VariantProps<typeof formTextareaVariants> {
    label?: React.ReactNode;
    error?: string;
    containerClassName?: string;
}

export const FormTextarea = forwardRef<HTMLTextAreaElement, FormTextareaProps>(
    ({ label, error, variant, className, containerClassName, id, ...props }, ref) => {
        const hasError = !!error;
        return (
            <div className={cn('space-y-1', containerClassName)}>
                {label && (
                    <label htmlFor={id} className="text-sm font-medium text-foreground">
                        {label}
                    </label>
                )}
                <textarea
                    id={id}
                    ref={ref}
                    className={cn(formTextareaVariants({ variant, hasError, className }))}
                    {...props}
                />
                {error && <p className="text-sm text-destructive">{error}</p>}
            </div>
        );
    },
);

FormTextarea.displayName = 'FormTextarea';
