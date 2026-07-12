import { useTranslation } from 'react-i18next';
import { FormSelect } from '@/components/common/form/FormSelect';

type SortBy = 'popular' | 'newest' | 'rating';

interface SortDropdownProps {
    value: SortBy;
    onChange: (value: SortBy) => void;
}

export function SortDropdown({ value, onChange }: SortDropdownProps) {
    const { t } = useTranslation('catalog');

    return (
        <FormSelect
            value={value}
            onValueChange={(v) => onChange(v as SortBy)}
            containerClassName="w-full sm:w-[200px]"
            options={[
                { value: 'popular', label: t('sort.popular') },
                { value: 'newest', label: t('sort.newest') },
                { value: 'rating', label: t('sort.rating') },
            ]}
        />
    );
}
