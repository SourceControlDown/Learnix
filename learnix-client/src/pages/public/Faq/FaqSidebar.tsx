import { FAQ_PAGE } from '@/const/localization/faqPage';

export function FaqSidebar() {
    return (
        <aside className="md:sticky md:top-20 md:self-start">
            <p className="mb-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                {FAQ_PAGE.TOPICS.heading}
            </p>
            <nav className="flex gap-1 overflow-x-auto md:flex-col md:overflow-visible">
                {FAQ_PAGE.TOPICS.categories.map((category, index) => (
                    <a
                        key={category.id}
                        href={`#${category.id}`}
                        className={`whitespace-nowrap rounded-lg px-3 py-2 text-sm ${
                            index === 0
                                ? 'bg-primary/10 font-medium text-primary'
                                : 'text-muted-foreground hover:bg-secondary hover:text-foreground'
                        }`}
                    >
                        {category.label}
                    </a>
                ))}
            </nav>

            <div className="mt-8 hidden rounded-xl border border-border bg-card p-4 md:block">
                <p className="text-sm font-medium">{FAQ_PAGE.TOPICS.supportTitle}</p>
                <p className="mt-1 text-xs text-muted-foreground">
                    {FAQ_PAGE.TOPICS.supportSubtitle}
                </p>
                <a href="#" className="mt-3 inline-block text-sm text-primary hover:underline">
                    {FAQ_PAGE.TOPICS.supportCta}
                </a>
            </div>
        </aside>
    );
}
