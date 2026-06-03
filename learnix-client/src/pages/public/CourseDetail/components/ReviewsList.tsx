import { User } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { RatingStars } from '@/components/common/RatingStars';
import type { CourseReviewDto } from '@/types/review.types';

interface ReviewsListProps {
    reviews: CourseReviewDto[];
    averageRating: number;
    totalCount: number;
}

export function ReviewsList({ reviews, averageRating, totalCount }: ReviewsListProps) {
    const { t } = useTranslation('courseDetail');

    return (
        <section className="space-y-4">
            <div className="flex items-baseline justify-between">
                <h2 className="font-heading text-xl font-semibold text-foreground">
                    {t('reviews.title')}
                </h2>
                {totalCount > 0 && (
                    <div className="flex items-center gap-2">
                        <RatingStars value={Math.round(averageRating)} size="sm" />
                        <span className="text-sm font-medium">{averageRating.toFixed(1)}</span>
                        <span className="text-sm text-muted-foreground">
                            ({t('reviews.reviewCount', { count: totalCount })})
                        </span>
                    </div>
                )}
            </div>

            {reviews.length === 0 ? (
                <p className="text-sm text-muted-foreground">{t('reviews.noReviews')}</p>
            ) : (
                <div className="space-y-4">
                    {reviews.map((review) => (
                        <div
                            key={review.id}
                            className="rounded-xl border border-border bg-card p-5"
                        >
                            <div className="flex items-start gap-4">
                                <div className="flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-full bg-muted">
                                    {review.studentAvatarBlobPath ? (
                                        <img
                                            src={review.studentAvatarBlobPath}
                                            alt={review.studentFirstName}
                                            className="h-full w-full object-cover"
                                        />
                                    ) : (
                                        <User className="h-5 w-5 text-muted-foreground" />
                                    )}
                                </div>

                                <div className="flex-1">
                                    <div className="flex items-center justify-between">
                                        <p className="font-medium text-foreground">
                                            {review.studentFirstName} {review.studentLastName}
                                        </p>
                                        <span className="text-xs text-muted-foreground">
                                            {new Date(review.createdAt).toLocaleDateString(
                                                'en-US',
                                                { month: 'short', day: 'numeric', year: 'numeric' },
                                            )}
                                        </span>
                                    </div>

                                    <RatingStars value={review.rating} size="sm" className="mt-1" />

                                    {review.comment && (
                                        <p className="mt-2 text-sm text-foreground">
                                            {review.comment}
                                        </p>
                                    )}
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </section>
    );
}
