import { z } from 'zod';
import { PROFILE_LIMITS } from '@/const/profile.constants';

export const profileSchema = z.object({
    firstName: z
        .string()
        .min(1, 'First name is required')
        .max(PROFILE_LIMITS.FIRST_NAME_MAX),
    lastName: z
        .string()
        .min(1, 'Last name is required')
        .max(PROFILE_LIMITS.LAST_NAME_MAX),
    bio: z
        .string()
        .max(PROFILE_LIMITS.BIO_MAX, 'Bio must be 500 characters or less')
        .optional(),
});

export type ProfileFormValues = z.infer<typeof profileSchema>;
