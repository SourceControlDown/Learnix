import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';

export function useFormatDuration() {
    const { t } = useTranslation('common');

    const formatDuration = useCallback(
        (seconds: number) => {
            if (seconds < 60) {
                return t('lessonMeta.durationSeconds', { n: Math.max(1, Math.round(seconds)) });
            }

            const totalMinutes = Math.round(seconds / 60);
            const hours = Math.floor(totalMinutes / 60);

            return hours > 0
                ? t('lessonMeta.durationHours', { h: hours, m: totalMinutes % 60 })
                : t('lessonMeta.durationMinutes', { n: totalMinutes });
        },
        [t],
    );

    return formatDuration;
}
