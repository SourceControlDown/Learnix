import { z } from 'zod';

export const profileSchema = z.object({
    firstName: z.string().min(1, 'First name is required').max(50),
    lastName: z.string().min(1, 'Last name is required').max(50),
    bio: z.string().max(500, 'Bio must be 500 characters or less').optional(),
});

export type ProfileFormValues = z.infer<typeof profileSchema>;
