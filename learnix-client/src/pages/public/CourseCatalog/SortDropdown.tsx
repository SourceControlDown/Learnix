import { useTranslation } from 'react-i18next';

type SortBy = 'popular' | 'newest' | 'rating';

interface SortDropdownProps {
    value: SortBy;
    onChange: (value: SortBy) => void;
}

export function SortDropdown({ value, onChange }: SortDropdownProps) {
    const { t } = useTranslation('catalog');

    const OPTIONS: { value: SortBy; label: string }[] = [
        { value: 'popular', label: t('sort.popular') },
        { value: 'newest', label: t('sort.newest') },
        { value: 'rating', label: t('sort.rating') },
    ];

    return (
        <select
            value={value}
            onChange={(e) => onChange(e.target.value as SortBy)}
            className="rounded-lg border border-input bg-card px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            aria-label={t('sort.label')}
        >
            {OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                    {opt.label}
                </option>
            ))}
        </select>
    );
}
