import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { MockPaymentDto } from '@/types/admin.types';
import { cn } from '@/utils/cn';
import { MOCK_PAYMENTS } from '@/utils/mocks/payments.mock';

type StatusFilter = 'All' | 'Completed' | 'Pending' | 'Failed';

const STATUS_STYLES: Record<MockPaymentDto['status'], string> = {
    Completed: 'bg-success/20 text-success',
    Pending: 'bg-warning/20 text-warning',
    Failed: 'bg-destructive/10 text-destructive',
};

export default function PaymentHistoryPage() {
    const { t } = useTranslation('admin');
    const [filter, setFilter] = useState<StatusFilter>('All');

    const STATUS_LABELS: Record<MockPaymentDto['status'], string> = {
        Completed: t('payStatusCompleted'),
        Pending: t('payStatusPending'),
        Failed: t('payStatusFailed'),
    };

    const FILTERS: { value: StatusFilter; label: string }[] = [
        { value: 'All', label: t('payFilterAll') },
        { value: 'Completed', label: t('payFilterCompleted') },
        { value: 'Pending', label: t('payFilterPending') },
        { value: 'Failed', label: t('payFilterFailed') },
    ];

    const displayed =
        filter === 'All' ? MOCK_PAYMENTS : MOCK_PAYMENTS.filter((p) => p.status === filter);

    const total = displayed.reduce(
        (sum, p) => (p.status === 'Completed' ? sum + p.amount : sum),
        0,
    );

    return (
        <div className="flex h-full flex-col p-8">
            {/* Header */}
            <div className="mb-2 flex flex-wrap items-start justify-between gap-4">
                <div>
                    <h1 className="font-heading text-3xl font-bold text-foreground">
                        {t('paymentsTitle')}
                    </h1>
                    <p className="mt-1 text-muted-foreground">{t('paymentsSubtitle')}</p>
                </div>
                <span className="rounded-full border border-warning/30 bg-warning/10 px-3 py-1 text-xs font-medium text-warning">
                    {t('paymentsMockBadge')}
                </span>
            </div>

            {/* Filters */}
            <div className="mb-4 mt-6 flex gap-2">
                {FILTERS.map((f) => (
                    <button
                        key={f.value}
                        onClick={() => setFilter(f.value)}
                        className={cn(
                            'rounded-lg px-3 py-1.5 text-sm transition-colors',
                            filter === f.value
                                ? 'bg-primary text-primary-foreground'
                                : 'bg-secondary text-foreground hover:bg-secondary/80',
                        )}
                    >
                        {f.label}
                    </button>
                ))}
            </div>

            {/* Table */}
            <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-xl border border-border bg-card">
                {displayed.length === 0 ? (
                    <div className="py-16 text-center text-sm text-muted-foreground">
                        {t('emptyPayments')}
                    </div>
                ) : (
                    <div className="min-h-0 flex-1 overflow-y-auto">
                        <table className="w-full text-sm">
                            <thead className="sticky top-0 bg-secondary/50 text-xs uppercase tracking-wider text-muted-foreground">
                                <tr>
                                    <th className="px-5 py-3 text-left font-medium">
                                        {t('colPayer')}
                                    </th>
                                    <th className="px-5 py-3 text-left font-medium">
                                        {t('colCourseTitle')}
                                    </th>
                                    <th className="px-5 py-3 text-left font-medium">
                                        {t('colAmount')}
                                    </th>
                                    <th className="px-5 py-3 text-left font-medium">
                                        {t('colPayStatus')}
                                    </th>
                                    <th className="px-5 py-3 text-left font-medium">
                                        {t('colDate')}
                                    </th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-border">
                                {displayed.map((p) => (
                                    <tr key={p.id} className="hover:bg-secondary/30">
                                        <td className="px-5 py-3">
                                            <p className="font-medium text-foreground">
                                                {p.userName}
                                            </p>
                                            <p className="text-xs text-muted-foreground">
                                                {p.userEmail}
                                            </p>
                                        </td>
                                        <td className="px-5 py-3 text-foreground">
                                            {p.courseTitle}
                                        </td>
                                        <td className="px-5 py-3 font-medium text-foreground">
                                            ${p.amount.toFixed(2)}
                                        </td>
                                        <td className="px-5 py-3">
                                            <span
                                                className={cn(
                                                    'rounded px-2 py-0.5 text-xs font-medium',
                                                    STATUS_STYLES[p.status],
                                                )}
                                            >
                                                {STATUS_LABELS[p.status]}
                                            </span>
                                        </td>
                                        <td className="px-5 py-3 text-muted-foreground">
                                            {new Date(p.createdAt).toLocaleDateString()}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}

                {/* Summary */}
                {displayed.length > 0 && filter !== 'Failed' && (
                    <div className="flex justify-end border-t border-border px-5 py-3">
                        <p className="text-sm text-muted-foreground">
                            Completed total:{' '}
                            <span className="font-semibold text-foreground">
                                ${total.toFixed(2)}
                            </span>
                        </p>
                    </div>
                )}
            </div>
        </div>
    );
}
