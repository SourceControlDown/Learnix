import { LANDING_PAGE } from '@/const/localization/landingPage';
import { useCourseCount } from '@/hooks/useCourseCount';
import { useCategories } from '@/hooks/useCategories';

const { STATS } = LANDING_PAGE;

export function StatsSection() {
    const { data: courseCount } = useCourseCount();
    const { data: categories } = useCategories();

    const stats = STATS.map((s, i) => {
        if (i !== 0) return s;
        return {
            ...s,
            value: courseCount !== undefined ? courseCount.toLocaleString('en-US') + '+' : s.value,
            label:
                categories !== undefined
                    ? `Courses across\n${categories.length} categories`
                    : s.label,
        };
    });

    return (
        <section className="border-y border-border bg-card">
            <div className="mx-auto grid max-w-7xl grid-cols-2 gap-8 px-6 py-10 text-center md:grid-cols-4">
                {stats.map((s, i) => (
                    <div key={i}>
                        <p className="font-heading text-4xl font-bold text-foreground md:text-5xl">
                            {s.highlightStar ? (
                                <>
                                    {s.value.replace('★', '')}
                                    <span className="text-warning">★</span>
                                </>
                            ) : (
                                s.value
                            )}
                        </p>
                        <p className="mt-2 whitespace-pre-line text-sm text-muted-foreground">
                            {s.label}
                        </p>
                    </div>
                ))}
            </div>
        </section>
    );
}
