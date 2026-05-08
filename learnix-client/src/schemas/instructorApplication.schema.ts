import { z } from 'zod';

export const instructorApplicationSchema = z.object({
    motivationText: z
        .string()
        .min(50, 'Please write at least 50 characters about your motivation')
        .max(3000, 'Motivation text is too long'),
    portfolioUrl: z.string().url('Please enter a valid URL').max(500).optional().or(z.literal('')),
});

export type InstructorApplicationFormData = z.infer<typeof instructorApplicationSchema>;
