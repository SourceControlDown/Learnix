import { z } from 'zod';

export const reviewSchema = z.object({
    rating: z.number().min(1, 'Rating is required').max(5),
    comment: z.string().max(1000, 'Comment must be 1000 characters or less').optional(),
});

export type ReviewFormValues = z.infer<typeof reviewSchema>;
