import { AxiosError } from 'axios';
import type { ProblemDetails } from '@/types/api.types';

export function isValidationError(error: unknown): error is AxiosError<ProblemDetails> {
    return (
        error instanceof AxiosError &&
        error.response?.status === 400 &&
        !!error.response.data?.errors
    );
}

export function getErrorMessage(error: unknown, fallback = 'Something went wrong'): string {
    if (error instanceof AxiosError) {
        const problem = error.response?.data as ProblemDetails | undefined;
        return problem?.detail ?? problem?.title ?? error.message ?? fallback;
    }
    return fallback;
}
