import { z } from 'zod';

export const paymentSchema = z.object({
    cardNumber: z
        .string()
        .min(16, 'Card number must be 16 digits')
        .max(16, 'Card number must be 16 digits')
        .regex(/^\d+$/, 'Card number can only contain digits'),
    expiry: z.string().regex(/^(0[1-9]|1[0-2])\/\d{2}$/, 'Expiry must be in MM/YY format'),
    cvv: z
        .string()
        .min(3, 'CVV must be 3 digits')
        .max(4, 'CVV must be at most 4 digits')
        .regex(/^\d+$/, 'CVV can only contain digits'),
    cardholderName: z.string().min(3, 'Name must be at least 3 characters'),
});

export type PaymentFormValues = z.infer<typeof paymentSchema>;
