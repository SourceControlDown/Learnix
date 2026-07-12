import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { SlidersHorizontal, X } from 'lucide-react';
import { CourseCard } from '@/components/common/course/CourseCard';
import { Seo } from '@/components/common/seo/Seo';
import { QueryError } from '@/components/common/system/QueryError';
import { PageSizeSelect } from '@/components/common/ui/PageSizeSelect';
import { Pagination } from '@/components/common/ui/Pagination';
import { SearchInput } from '@/components/common/ui/SearchInput';
import { CATALOG_PAGE_SIZES } from '@/const/ui.constants';
import { useCatalogCourses } from '@/hooks/course/useCatalogCourses';
import { useCategories } from '@/hooks/course/useCategories';
import { useMediaQuery } from '@/hooks/shared/useMediaQuery';
import { APP_ROUTES } from '@/routes/paths';
import { cn } from '@/utils/cn';
import { breadcrumbJsonLd } from '@/utils/seo';
import { FilterSidebar } from './FilterSidebar';
import { SortDropdown } from './SortDropdown';
import { useCatalogFilters } from './hooks/useCatalogFilters';

export default function CourseCatalogPage() {
    const { t } = useTranslation('catalog');

    const {
        categoryId,
        sortBy,
        isFree,
        minRating,
        page,
        pageSize,
        searchInput,
        debouncedSearch,
        setSearchInput,
        setPage,
        setPageSize,
        setCategoryId,
        setSortBy,
        setIsFree,
        setMinRating,
        clearAllFilters,
    } = useCatalogFilters();

    const [isFiltersOpen, setIsFiltersOpen] = useState(false);

    // Mobile never offers 48+ cards per page. If the URL carries a desktop-only size and the
    // viewport is small, clamp it down to the largest mobile option.
    const isDesktop = useMediaQuery('(min-width: 640px)');
    const pageSizeOptions = isDesktop ? CATALOG_PAGE_SIZES.desktop : CATALOG_PAGE_SIZES.mobile;
    useEffect(() => {
        const max = pageSizeOptions.at(-1);
        if (max !== undefined && pageSize > max) setPageSize(max);
    }, [pageSizeOptions, pageSize, setPageSize]);

    const { data: categoriesData } = useCategories();
    const categories = categoriesData ?? [];

    const { data, isFetching, isLoading, isError, refetch } = useCatalogCourses({
        search: debouncedSearch,
        categoryId,
        sortBy,
        isFree,
        minRating,
        page,
        pageSize,
    });

    const courses = data?.items ?? [];
    const totalCount = data?.totalCount ?? 0;
    const totalPages = data?.totalPages ?? 1;

    // Active chips
    const chips: { label: string; onRemove: () => void }[] = [];
    if (categoryId) {
        const cat = categories.find((c) => c.id === categoryId);
        if (cat) chips.push({ label: cat.name, onRemove: () => setCategoryId('') });
    }
    if (isFree === true)
        chips.push({
            label: t('common:general.free'),
            onRemove: () => setIsFree(undefined),
        });
    if (isFree === false)
        chips.push({
            label: t('activeChips.paid'),
            onRemove: () => setIsFree(undefined),
        });
    if (minRating !== undefined)
        chips.push({
            label: t('activeChips.rating', { n: minRating }),
            onRemove: () => setMinRating(undefined),
        });

    return (
        <>
            {/* Filters and paging live in the query string; canonical points at the clean URL so
                every filtered view consolidates onto one indexable page. */}
            <Seo
                title={t('seo.title')}
                description={t('seo.description')}
                canonicalPath={APP_ROUTES.public.courses}
                jsonLd={breadcrumbJsonLd([
                    { name: t('common:navigation.home'), path: APP_ROUTES.public.home },
                    { name: t('common:navigation.courses'), path: APP_ROUTES.public.courses },
                ])}
            />
            <div className="min-h-screen bg-background">
                <div className="mx-auto max-w-7xl px-4 pb-8 pt-4 sm:px-6 md:pt-6">
                    {/* Page title */}
                    <div className="mb-6 flex flex-col items-center justify-between gap-2 text-center md:flex-row md:items-end md:justify-start md:gap-4 md:text-left">
                        <h1 className="font-heading text-3xl font-bold md:text-4xl">
                            {t('common:navigation.allCourses')}
                        </h1>
                        <p className="text-muted-foreground md:pb-1">
                            {debouncedSearch
                                ? t('resultsCountQuery', {
                                      count: totalCount,
                                      query: debouncedSearch,
                                  })
                                : t('resultsCount', { count: totalCount })}
                        </p>
                    </div>

                    {/* Body: sidebar + courses */}
                    <div className="grid gap-6 md:grid-cols-[260px_1fr] md:gap-8">
                        {/* Mobile Filters Toggle */}
                        <div className="md:hidden">
                            <button
                                type="button"
                                onClick={() => setIsFiltersOpen((prev) => !prev)}
                                className="flex w-full items-center justify-center gap-2 rounded-xl border border-border bg-card px-4 py-3.5 font-semibold shadow-sm transition-all hover:bg-secondary active:scale-[0.98]"
                            >
                                <SlidersHorizontal className="size-5" />
                                {t('common:general.filters')}
                            </button>
                        </div>

                        {/* Filters */}
                        <div className={cn('md:block', isFiltersOpen ? 'block' : 'hidden')}>
                            <FilterSidebar
                                categories={categories}
                                selectedCategoryId={categoryId}
                                isFree={isFree}
                                minRating={minRating}
                                onCategoryChange={setCategoryId}
                                onPriceChange={setIsFree}
                                onRatingChange={setMinRating}
                                onClear={clearAllFilters}
                            />
                        </div>

                        {/* Main */}
                        <div>
                            {/* Search + Sort row */}
                            <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center">
                                <SearchInput
                                    containerClassName="flex-1"
                                    value={searchInput}
                                    onChange={(e) => setSearchInput(e.target.value)}
                                    onClear={() => setSearchInput('')}
                                    placeholder={t('searchPlaceholder')}
                                />
                                <SortDropdown value={sortBy} onChange={setSortBy} />
                            </div>

                            {/* Active filter chips */}
                            {chips.length > 0 && (
                                <div className="mb-4 flex flex-wrap gap-2">
                                    {chips.map((chip) => (
                                        <span
                                            key={chip.label}
                                            className="flex items-center gap-1 rounded-full bg-primary/10 px-3 py-1 text-xs text-primary"
                                        >
                                            {chip.label}
                                            <button
                                                type="button"
                                                onClick={chip.onRemove}
                                                className="ml-0.5 rounded-full hover:text-primary/70"
                                                aria-label={`Remove ${chip.label} filter`}
                                            >
                                                <X className="size-3" />
                                            </button>
                                        </span>
                                    ))}
                                </div>
                            )}

                            {/* Grid */}
                            {isLoading && courses.length === 0 ? (
                                <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                                    {Array.from({ length: 6 }).map((_, i) => (
                                        <div
                                            key={i}
                                            className="h-72 animate-pulse rounded-xl border border-border bg-card"
                                        />
                                    ))}
                                </div>
                            ) : isError ? (
                                <QueryError
                                    message={t('error.title')}
                                    onRetry={refetch}
                                    retryLabel={t('common:actions.tryAgain')}
                                />
                            ) : (
                                <div
                                    className={cn(
                                        'grid gap-5 transition-opacity sm:grid-cols-2 lg:grid-cols-3',
                                        isFetching && 'opacity-60',
                                    )}
                                >
                                    {courses.length > 0
                                        ? courses.map((course) => (
                                              <CourseCard key={course.id} course={course} />
                                          ))
                                        : !isFetching && (
                                              <div className="col-span-full py-20 text-center">
                                                  <p className="font-heading text-lg font-semibold text-foreground">
                                                      {t('noResultsTitle')}
                                                  </p>
                                                  <p className="mt-2 text-sm text-muted-foreground">
                                                      {t('noResultsDesc')}
                                                  </p>
                                                  <button
                                                      type="button"
                                                      onClick={clearAllFilters}
                                                      className="mt-4 text-sm text-primary underline hover:text-primary/80"
                                                  >
                                                      {t('clearFilters')}
                                                  </button>
                                              </div>
                                          )}
                                </div>
                            )}

                            {/* Footer: page size (left) + pagination (right) */}
                            {courses.length > 0 && (
                                <div className="flex flex-col items-center gap-4 pt-10 sm:flex-row sm:justify-between">
                                    <PageSizeSelect
                                        value={pageSize}
                                        onChange={setPageSize}
                                        options={pageSizeOptions}
                                        label={t('pagination.perPage')}
                                    />
                                    <Pagination
                                        page={page}
                                        totalPages={totalPages}
                                        onChange={setPage}
                                        showGoToPage={totalPages > 10}
                                        goToLabel={t('pagination.goToPage')}
                                    />
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
}
