import { DollarSign, ShoppingCart } from 'lucide-react';
import { useInstructorEarningsQuery } from '@/hooks/useInstructorEarningsQuery';
import { INSTRUCTOR } from '@/const/localization/instructor';

export default function InstructorEarningsPage() {
    const { data, isLoading } = useInstructorEarningsQuery();

    return (
        <div className="p-8">
            {/* Header */}
            <div className="mb-8 flex items-end justify-between">
                <div>
                    <h1 className="font-heading text-3xl font-bold text-foreground">
                        {INSTRUCTOR.EARNINGS_TITLE}
                    </h1>
                    <p className="mt-1 text-muted-foreground">{INSTRUCTOR.EARNINGS_SUBTITLE}</p>
                </div>
                <span className="rounded-full border border-warning/30 bg-warning/10 px-3 py-1 text-xs font-medium text-warning">
                    {INSTRUCTOR.EARNINGS_FREE_BADGE}
                </span>
            </div>

            {/* Stats */}
            <div className="mb-8 grid gap-4 md:grid-cols-2">
                <div className="flex items-center gap-4 rounded-xl border border-border bg-card p-5">
                    <div className="grid h-10 w-10 place-items-center rounded-lg bg-primary/10">
                        <DollarSign size={20} className="text-primary" />
                    </div>
                    <div>
                        <p className="text-sm text-muted-foreground">{INSTRUCTOR.EARNINGS_TOTAL}</p>
                        <p className="font-heading text-2xl font-bold text-foreground">
                            {isLoading ? '—' : `$${(data?.totalEarnings ?? 0).toFixed(2)}`}
                        </p>
                    </div>
                </div>
                <div className="flex items-center gap-4 rounded-xl border border-border bg-card p-5">
                    <div className="grid h-10 w-10 place-items-center rounded-lg bg-primary/10">
                        <ShoppingCart size={20} className="text-primary" />
                    </div>
                    <div>
                        <p className="text-sm text-muted-foreground">
                            {INSTRUCTOR.EARNINGS_PAYMENTS}
                        </p>
                        <p className="font-heading text-2xl font-bold text-foreground">
                            {isLoading ? '—' : (data?.totalPayments ?? 0).toLocaleString()}
                        </p>
                    </div>
                </div>
            </div>

            {/* Revenue by course */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                <div className="border-b border-border px-5 py-4">
                    <h3 className="font-heading font-semibold text-foreground">
                        {INSTRUCTOR.EARNINGS_COURSES_TABLE_TITLE}
                    </h3>
                </div>

                {isLoading ? (
                    <div className="py-12 text-center text-sm text-muted-foreground">
                        Loading...
                    </div>
                ) : !data || data.courses.length === 0 ? (
                    <div className="py-12 text-center text-sm text-muted-foreground">
                        {INSTRUCTOR.EARNINGS_EMPTY}
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-secondary/50 text-xs uppercase tracking-wider text-muted-foreground">
                            <tr>
                                <th className="px-5 py-3 text-left font-medium">
                                    {INSTRUCTOR.EARNINGS_COL_COURSE}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {INSTRUCTOR.EARNINGS_COL_PAYMENTS}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {INSTRUCTOR.EARNINGS_COL_REVENUE}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {INSTRUCTOR.EARNINGS_COL_LAST}
                                </th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-border">
                            {data.courses.map((course) => (
                                <tr key={course.courseId} className="hover:bg-secondary/30">
                                    <td className="px-5 py-3 font-medium text-foreground">
                                        {course.courseTitle}
                                    </td>
                                    <td className="px-5 py-3 text-muted-foreground">
                                        {course.paymentsCount}
                                    </td>
                                    <td className="px-5 py-3 font-semibold text-foreground">
                                        ${course.totalAmount.toFixed(2)}
                                    </td>
                                    <td className="px-5 py-3 text-muted-foreground">
                                        {new Date(course.lastPaymentAt).toLocaleDateString()}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                        <tfoot>
                            <tr className="border-t border-border">
                                <td
                                    colSpan={2}
                                    className="px-5 py-3 text-right text-sm text-muted-foreground"
                                >
                                    Total
                                </td>
                                <td className="px-5 py-3 font-bold text-foreground">
                                    ${data.totalEarnings.toFixed(2)}
                                </td>
                                <td />
                            </tr>
                        </tfoot>
                    </table>
                )}
            </div>
        </div>
    );
}
