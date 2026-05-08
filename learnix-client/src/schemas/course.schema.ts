import { z } from 'zod';

export const courseInfoSchema = z.object({
    title: z.string().min(3, 'Title must be at least 3 characters').max(200, 'Title is too long'),
    description: z
        .string()
        .min(10, 'Description must be at least 10 characters')
        .max(5000, 'Description is too long'),
    categoryId: z.string().min(1, 'Category is required'),
    price: z.coerce
        .number({ invalid_type_error: 'Price must be a number' })
        .min(0, 'Price cannot be negative'),
    coverImageUrl: z.string().nullable().optional(),
    tags: z.array(z.string().max(50)).max(10, 'Maximum 10 tags allowed'),
});

export type CourseInfoFormData = z.infer<typeof courseInfoSchema>;
