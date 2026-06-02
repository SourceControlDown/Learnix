import { z } from 'zod';
import { PAYMENT_LIMITS } from '@/const/payment.constants';

export const paymentSchema = z.object({
    cardNumber: z
        .string()
        .min(PAYMENT_LIMITS.CARD_NUMBER_LENGTH, 'Card number must be 16 digits')
        .max(PAYMENT_LIMITS.CARD_NUMBER_LENGTH, 'Card number must be 16 digits')
        .regex(/^\d+$/, 'Card number can only contain digits'),
    expiry: z.string().regex(/^(0[1-9]|1[0-2])\/\d{2}$/, 'Expiry must be in MM/YY format'),
    cvv: z
        .string()
        .min(PAYMENT_LIMITS.CVV_MIN, 'CVV must be 3 digits')
        .max(PAYMENT_LIMITS.CVV_MAX, 'CVV must be at most 4 digits')
        .regex(/^\d+$/, 'CVV can only contain digits'),
    cardholderName: z
        .string()
        .min(PAYMENT_LIMITS.CARDHOLDER_NAME_MIN, 'Name must be at least 3 characters'),
});

export type PaymentFormValues = z.infer<typeof paymentSchema>;
