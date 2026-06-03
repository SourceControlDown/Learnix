import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';
import type { LandingCategory } from '@/mocks/landing.mock';

interface CategoriesSectionProps {
    categories: LandingCategory[];
    isLoading?: boolean;
}

export function CategoriesSection({ categories, isLoading }: CategoriesSectionProps) {
    const { t } = useTranslation('landing');

    return (
        <section id="categories" className="py-20">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-10 flex items-end justify-between">
                    <div>
                        <span className="text-sm font-semibold text-primary">
                            {t('categories.tag')}
                        </span>
                        <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                            {t('categories.heading')}
                        </h2>
                    </div>
                    <Link
                        to="/courses"
                        className="hidden text-sm text-primary hover:underline md:inline"
                    >
                        {t('categories.viewAll')}
                    </Link>
                </div>

                {isLoading ? (
                    <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-6">
                        {Array.from({ length: 12 }).map((_, i) => (
                            <div
                                key={i}
                                className="h-28 animate-pulse rounded-xl border border-border bg-card"
                            />
                        ))}
                    </div>
                ) : (
                    <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-6">
                        {categories.map((cat) => (
                            <Link
                                key={cat.id}
                                to={`/courses?categoryId=${cat.id}`}
                                className="group rounded-xl border border-border bg-card p-5 transition-all hover:border-primary hover:shadow-md"
                            >
                                <div
                                    className={cn(
                                        'grid h-12 w-12 place-items-center rounded-lg text-2xl transition-transform group-hover:scale-110',
                                        cat.iconBgClass,
                                        cat.iconTextClass,
                                    )}
                                >
                                    {cat.emoji}
                                </div>
                                <h3 className="mt-4 font-heading font-semibold">{cat.name}</h3>
                                <p className="mt-1 text-xs text-muted-foreground">
                                    {cat.coursesCount} {t('categories.coursesLabel')}
                                </p>
                            </Link>
                        ))}
                    </div>
                )}
            </div>
        </section>
    );
}
