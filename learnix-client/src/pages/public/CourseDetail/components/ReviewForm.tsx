import { useEffect } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { reviewSchema, type ReviewFormValues } from '@/schemas/review.schema';
import { RatingStars } from '@/components/common/RatingStars';
import { COURSE_DETAIL } from '@/const/localization/courseDetail';
import { cn } from '@/utils/cn';
import { isValidationError } from '@/utils/errors';
import type { MyReviewDto } from '@/types/review.types';
import { useCreateReview, useUpdateReview, useDeleteReview } from '@/hooks/useReviewMutations';

interface ReviewFormProps {
    courseId: string;
    existing: MyReviewDto | null;
}

export function ReviewForm({ courseId, existing }: ReviewFormProps) {
    const createReview = useCreateReview(courseId);
    const updateReview = useUpdateReview(courseId, existing?.id ?? '');
    const deleteReview = useDeleteReview(courseId, existing?.id ?? '');

    const form = useForm<ReviewFormValues>({
        resolver: zodResolver(reviewSchema),
        defaultValues: { rating: existing?.rating ?? 0, comment: existing?.comment ?? '' },
    });

    useEffect(() => {
        if (existing) {
            form.reset({ rating: existing.rating, comment: existing.comment ?? '' });
        }
    }, [existing, form]);

    async function onSubmit(values: ReviewFormValues) {
        const payload = { rating: values.rating, comment: values.comment || null };
        try {
            if (existing) {
                await updateReview.mutateAsync(payload);
            } else {
                await createReview.mutateAsync(payload);
            }
            if (!existing) form.reset({ rating: 0, comment: '' });
        } catch (error) {
            if (isValidationError(error)) {
                const errors = error.response!.data.errors!;
                Object.entries(errors).forEach(([field, messages]) => {
                    form.setError(
                        (field.charAt(0).toLowerCase() + field.slice(1)) as keyof ReviewFormValues,
                        {
                            message: messages.join('. '),
                        },
                    );
                });
            }
        }
    }

    const isPending = createReview.isPending || updateReview.isPending;
    const title = existing ? COURSE_DETAIL.REVIEWS.EDIT_REVIEW : COURSE_DETAIL.REVIEWS.WRITE_REVIEW;

    return (
        <div className="rounded-xl border border-border bg-card p-5">
            <h3 className="font-heading font-semibold text-foreground">{title}</h3>

            <form onSubmit={form.handleSubmit(onSubmit)} className="mt-4 space-y-4">
                <div>
                    <label className="block text-sm font-medium text-foreground">
                        {COURSE_DETAIL.REVIEWS.RATING_LABEL}
                    </label>
                    <Controller
                        control={form.control}
                        name="rating"
                        render={({ field }) => (
                            <RatingStars
                                value={field.value}
                                onChange={field.onChange}
                                size="lg"
                                className="mt-2"
                            />
                        )}
                    />
                    {form.formState.errors.rating && (
                        <p className="mt-1 text-xs text-destructive">
                            {form.formState.errors.rating.message}
                        </p>
                    )}
                </div>

                <div>
                    <textarea
                        {...form.register('comment')}
                        rows={4}
                        placeholder={COURSE_DETAIL.REVIEWS.COMMENT_PLACEHOLDER}
                        className={cn(
                            'w-full resize-none rounded-lg border bg-background px-3 py-2 text-sm outline-none transition-colors',
                            'focus:border-primary focus:ring-1 focus:ring-primary',
                            form.formState.errors.comment ? 'border-destructive' : 'border-border',
                        )}
                    />
                    {form.formState.errors.comment && (
                        <p className="mt-1 text-xs text-destructive">
                            {form.formState.errors.comment.message}
                        </p>
                    )}
                </div>

                <div className="flex items-center gap-3">
                    <button
                        type="submit"
                        disabled={isPending}
                        className="rounded-lg bg-primary px-5 py-2 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
                    >
                        {isPending
                            ? COURSE_DETAIL.REVIEWS.SUBMITTING
                            : existing
                              ? COURSE_DETAIL.REVIEWS.UPDATE
                              : COURSE_DETAIL.REVIEWS.SUBMIT}
                    </button>

                    {existing && (
                        <button
                            type="button"
                            disabled={deleteReview.isPending}
                            onClick={() => deleteReview.mutate()}
                            className="text-sm text-destructive hover:underline disabled:opacity-50"
                        >
                            {COURSE_DETAIL.REVIEWS.DELETE}
                        </button>
                    )}
                </div>
            </form>
        </div>
    );
}
