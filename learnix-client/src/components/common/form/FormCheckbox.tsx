import React, { forwardRef } from 'react';
import type { FieldError } from 'react-hook-form';
import { ChoiceIndicator } from '@/components/common/form/ChoiceIndicator';
import { cn } from '@/utils/cn';
import { getFieldErrors } from '@/utils/errors';

interface FormCheckboxProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
    label: React.ReactNode;
    error?: string | FieldError;
    containerClassName?: string;
    labelClassName?: string;
}

/**
 * The input is real but hidden, and the box beside it is drawn (`ChoiceIndicator`): a native checkbox styled
 * with `accent-*` still paints its own unchecked box, which is white on a dark background.
 *
 * The indicator runs in `peer` mode rather than off a `checked` prop, because this component is used both
 * controlled and through `register()` — and a registered input is uncontrolled, so nothing here re-renders
 * when it is ticked. CSS sees `:checked` either way.
 */
export const FormCheckbox = forwardRef<HTMLInputElement, FormCheckboxProps>(
    ({ label, error, className, containerClassName, labelClassName, id, ...props }, ref) => {
        const errorMessages = getFieldErrors(error);

        return (
            <div className={cn('space-y-1', containerClassName)}>
                <label
                    htmlFor={id}
                    className={cn(
                        'group flex cursor-pointer items-center gap-2 text-sm text-foreground',
                        props.disabled && 'cursor-not-allowed opacity-50',
                        labelClassName,
                    )}
                >
                    <input
                        type="checkbox"
                        id={id}
                        ref={ref}
                        className={cn('peer sr-only', className)}
                        {...props}
                    />
                    <ChoiceIndicator type="checkbox" peer hasError={errorMessages.length > 0} />
                    {label}
                </label>
                {errorMessages.length > 0 && (
                    <div className="mt-1 space-y-1">
                        {errorMessages.map((msg, idx) => (
                            <p key={idx} className="text-sm text-destructive">
                                {msg}
                            </p>
                        ))}
                    </div>
                )}
            </div>
        );
    },
);

FormCheckbox.displayName = 'FormCheckbox';
