import { COURSE_CATALOG } from '@/const/localization/courseCatalog';

type SortBy = 'popular' | 'newest' | 'rating';

interface SortDropdownProps {
    value: SortBy;
    onChange: (value: SortBy) => void;
}

const OPTIONS: { value: SortBy; label: string }[] = [
    { value: 'popular', label: COURSE_CATALOG.SORT.POPULAR },
    { value: 'newest', label: COURSE_CATALOG.SORT.NEWEST },
    { value: 'rating', label: COURSE_CATALOG.SORT.RATING },
];

export function SortDropdown({ value, onChange }: SortDropdownProps) {
    return (
        <select
            value={value}
            onChange={(e) => onChange(e.target.value as SortBy)}
            className="rounded-lg border border-input bg-card px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            aria-label={COURSE_CATALOG.SORT.LABEL}
        >
            {OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                    {opt.label}
                </option>
            ))}
        </select>
    );
}
