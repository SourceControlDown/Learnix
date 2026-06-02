import { z } from 'zod';
import { COURSE_LIMITS } from '@/const/course.constants';

export const courseInfoSchema = z.object({
    title: z
        .string()
        .min(COURSE_LIMITS.TITLE_MIN, 'Title must be at least 3 characters')
        .max(COURSE_LIMITS.TITLE_MAX, 'Title is too long'),
    description: z
        .string()
        .min(COURSE_LIMITS.DESCRIPTION_MIN, 'Description must be at least 10 characters')
        .max(COURSE_LIMITS.DESCRIPTION_MAX, 'Description is too long'),
    categoryId: z.string().min(1, 'Category is required'),
    price: z.coerce
        .number({ invalid_type_error: 'Price must be a number' })
        .min(COURSE_LIMITS.PRICE_MIN, 'Price cannot be negative'),
    coverImageUrl: z.string().nullable().optional(),
    tags: z
        .array(z.string().max(COURSE_LIMITS.TAG_MAX_LENGTH))
        .max(COURSE_LIMITS.TAGS_MAX_COUNT, 'Maximum 10 tags allowed'),
});

export type CourseInfoFormData = z.infer<typeof courseInfoSchema>;
