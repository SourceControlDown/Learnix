import { z } from 'zod';
import { INSTRUCTOR_APP_LIMITS } from '@/const/instructor.constants';

export const instructorApplicationSchema = z.object({
    motivationText: z
        .string()
        .min(
            INSTRUCTOR_APP_LIMITS.MOTIVATION_MIN,
            'Please write at least 50 characters about your motivation',
        )
        .max(INSTRUCTOR_APP_LIMITS.MOTIVATION_MAX, 'Motivation text is too long'),
    portfolioUrl: z
        .string()
        .url('Please enter a valid URL')
        .max(INSTRUCTOR_APP_LIMITS.PORTFOLIO_URL_MAX)
        .optional()
        .or(z.literal('')),
});

export type InstructorApplicationFormData = z.infer<typeof instructorApplicationSchema>;
