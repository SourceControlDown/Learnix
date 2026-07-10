import type { ReactNode } from 'react';
import type { FieldError } from 'react-hook-form';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { cn } from '@/utils/cn';
import { getFieldErrors } from '@/utils/errors';

export interface FormSelectOption {
    value: string;
    label: ReactNode;
    disabled?: boolean;
}

interface FormSelectProps {
    options: FormSelectOption[];
    value?: string;
    onValueChange?: (value: string) => void;
    label?: ReactNode;
    error?: string | FieldError;
    placeholder?: string;
    /** Surface the control sits on: `default` (page) or `card` (inside a card). */
    variant?: 'default' | 'card';
    disabled?: boolean;
    id?: string;
    name?: string;
    containerClassName?: string;
    triggerClassName?: string;
}

/**
 * The single dropdown/select for the whole app, built on the shared shadcn/radix Select so its
 * trigger AND popup are identical everywhere (unlike a native <select>, whose list is OS-drawn).
 * Form-bound callers wire it through a react-hook-form <Controller>.
 */
export function FormSelect({
    options,
    value,
    onValueChange,
    label,
    error,
    placeholder,
    variant = 'default',
    disabled,
    id,
    name,
    containerClassName,
    triggerClassName,
}: FormSelectProps) {
    const errorMessages = getFieldErrors(error);
    const hasError = errorMessages.length > 0;

    return (
        <div className={cn('space-y-1', containerClassName)}>
            {label && (
                <label htmlFor={id} className="block text-sm font-medium text-foreground">
                    {label}
                </label>
            )}
            <Select
                value={value || undefined}
                onValueChange={onValueChange}
                disabled={disabled}
                name={name}
            >
                <SelectTrigger
                    id={id}
                    variant={variant}
                    className={cn(
                        hasError &&
                            'border-field-error focus:border-field-error focus:ring-field-error/20',
                        triggerClassName,
                    )}
                >
                    <SelectValue placeholder={placeholder} />
                </SelectTrigger>
                <SelectContent>
                    {options.map((option) => (
                        <SelectItem
                            key={option.value}
                            value={option.value}
                            disabled={option.disabled}
                        >
                            {option.label}
                        </SelectItem>
                    ))}
                </SelectContent>
            </Select>
            {hasError && <p className="text-xs text-destructive">{errorMessages[0]}</p>}
        </div>
    );
}
