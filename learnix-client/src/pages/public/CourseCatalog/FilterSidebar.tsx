import { cn } from '@/utils/cn';
import { COURSE_CATALOG } from '@/const/localization/courseCatalog';

interface CategoryOption {
    id: string;
    name: string;
    coursesCount: number;
}

interface FilterSidebarProps {
    categories: CategoryOption[];
    selectedCategoryId: string;
    isFree: boolean | undefined;
    minRating: number | undefined;
    onCategoryChange: (id: string) => void;
    onPriceChange: (val: boolean | undefined) => void;
    onRatingChange: (val: number | undefined) => void;
    onClear: () => void;
}

const PRICE_OPTIONS: { label: string; value: boolean | undefined }[] = [
    { label: COURSE_CATALOG.FILTERS.PRICE_ALL, value: undefined },
    { label: COURSE_CATALOG.FILTERS.PRICE_FREE, value: true },
    { label: COURSE_CATALOG.FILTERS.PRICE_PAID, value: false },
];

const RATING_OPTIONS: { label: string; value: number }[] = [
    { label: COURSE_CATALOG.FILTERS.RATING_45, value: 4.5 },
    { label: COURSE_CATALOG.FILTERS.RATING_40, value: 4.0 },
    { label: COURSE_CATALOG.FILTERS.RATING_35, value: 3.5 },
];

const STAR = '★';

export function FilterSidebar({
    categories,
    selectedCategoryId,
    isFree,
    minRating,
    onCategoryChange,
    onPriceChange,
    onRatingChange,
    onClear,
}: FilterSidebarProps) {
    const hasActiveFilters =
        !!selectedCategoryId || isFree !== undefined || minRating !== undefined;

    return (
        <aside className="space-y-4">
            {/* Category */}
            <div className="rounded-xl border border-border bg-card p-5">
                <h3 className="mb-3 font-heading font-semibold">
                    {COURSE_CATALOG.FILTERS.CATEGORY}
                </h3>
                <div className="max-h-64 space-y-1 overflow-y-auto text-sm">
                    {categories.map((cat) => {
                        const selected = cat.id === selectedCategoryId;
                        return (
                            <button
                                key={cat.id}
                                type="button"
                                onClick={() => onCategoryChange(selected ? '' : cat.id)}
                                className={cn(
                                    'flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left transition-colors',
                                    selected
                                        ? 'bg-primary/10 text-primary'
                                        : 'hover:bg-muted hover:text-foreground',
                                )}
                            >
                                <span className={cn(selected && 'font-medium')}>{cat.name}</span>
                                <span className="text-muted-foreground">{cat.coursesCount}</span>
                            </button>
                        );
                    })}
                </div>
            </div>

            {/* Price */}
            <div className="rounded-xl border border-border bg-card p-5">
                <h3 className="mb-3 font-heading font-semibold">{COURSE_CATALOG.FILTERS.PRICE}</h3>
                <div className="space-y-2 text-sm">
                    {PRICE_OPTIONS.map((opt) => {
                        const checked = isFree === opt.value;
                        return (
                            <label
                                key={String(opt.value)}
                                className="flex cursor-pointer items-center gap-2 hover:text-primary"
                            >
                                <input
                                    type="radio"
                                    name="price"
                                    checked={checked}
                                    onChange={() => onPriceChange(opt.value)}
                                    className="accent-primary"
                                />
                                {opt.label}
                            </label>
                        );
                    })}
                </div>
            </div>

            {/* Rating */}
            <div className="rounded-xl border border-border bg-card p-5">
                <h3 className="mb-3 font-heading font-semibold">{COURSE_CATALOG.FILTERS.RATING}</h3>
                <div className="space-y-2 text-sm">
                    {RATING_OPTIONS.map((opt) => {
                        const checked = minRating === opt.value;
                        const stars = Math.floor(opt.value);
                        return (
                            <label
                                key={opt.value}
                                className="flex cursor-pointer items-center gap-2 hover:text-primary"
                            >
                                <input
                                    type="radio"
                                    name="rating"
                                    checked={checked}
                                    onChange={() => onRatingChange(checked ? undefined : opt.value)}
                                    className="accent-primary"
                                />
                                <span className="text-warning">{STAR.repeat(stars)}</span>
                                <span className="text-muted-foreground">
                                    {STAR.repeat(5 - stars).replace(/./g, '☆')}
                                </span>
                                <span>{opt.value}+</span>
                            </label>
                        );
                    })}
                </div>
            </div>

            {hasActiveFilters && (
                <button
                    type="button"
                    onClick={onClear}
                    className="w-full text-sm text-muted-foreground transition-colors hover:text-destructive"
                >
                    {COURSE_CATALOG.CLEAR_FILTERS}
                </button>
            )}
        </aside>
    );
}
