import React, { forwardRef, useState } from 'react';
import type { FieldError } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { type VariantProps, cva } from 'class-variance-authority';
import { AlertCircle, Eye, EyeOff } from 'lucide-react';
import { FIELD_BASE, FIELD_ERROR, FIELD_SURFACE_CARD } from '@/components/common/form/fieldStyles';
import { cn } from '@/utils/cn';
import { getFieldErrors } from '@/utils/errors';

// Border/focus/fill come from the shared field tokens; the only choice is the surface.
// pr-10 leaves room for the reveal button.
const passwordInputVariants = cva(`${FIELD_BASE} py-2.5 pl-3 pr-10`, {
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

interface PasswordInputProps
    extends
        Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'>,
        VariantProps<typeof passwordInputVariants> {
    label?: React.ReactNode;
    labelRightAction?: React.ReactNode;
    error?: string | FieldError;
    containerClassName?: string;
    hideErrorText?: boolean;
    /** Reserve a fixed-height slot for the error text so the form does not shift. Opt-in. */
    reserveErrorSpace?: boolean;
}

export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
    (
        {
            label,
            labelRightAction,
            error,
            hideErrorText,
            variant,
            className,
            containerClassName,
            reserveErrorSpace,
            id,
            ...props
        },
        ref,
    ) => {
        const { t } = useTranslation('zod');
        const [showPassword, setShowPassword] = useState(false);
        const errorMessages = getFieldErrors(error);
        const hasError = errorMessages.length > 0;

        return (
            <div className={cn('space-y-1.5', containerClassName)}>
                {(label || labelRightAction) && (
                    <div className="flex items-center justify-between">
                        {label ? (
                            <label htmlFor={id} className="text-sm font-medium text-foreground">
                                {label}
                            </label>
                        ) : (
                            <span />
                        )}
                        {labelRightAction}
                    </div>
                )}
                <div className="relative">
                    <input
                        id={id}
                        ref={ref}
                        type={showPassword ? 'text' : 'password'}
                        className={cn(
                            passwordInputVariants({ variant, hasError, className }),
                            hasError ? 'pr-20' : 'pr-10',
                        )}
                        {...props}
                    />
                    {hasError && (
                        <div className="pointer-events-none absolute right-12 top-1/2 -translate-y-1/2 text-destructive">
                            <AlertCircle size={18} />
                        </div>
                    )}
                    <button
                        type="button"
                        onClick={() => setShowPassword(!showPassword)}
                        className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-field-focus"
                    >
                        {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                    </button>
                </div>
                {!hideErrorText && (errorMessages.length > 0 || reserveErrorSpace) && (
                    <div className={cn('mt-1 space-y-1', reserveErrorSpace && 'min-h-[20px]')}>
                        {errorMessages.map((msg, idx) => (
                            <p key={idx} className="text-[13px] leading-tight text-destructive">
                                {t(msg)}
                            </p>
                        ))}
                    </div>
                )}
            </div>
        );
    },
);

PasswordInput.displayName = 'PasswordInput';
