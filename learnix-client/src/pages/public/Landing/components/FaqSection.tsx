import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

export function FaqSection() {
    const { t } = useTranslation('landing');
    const items = t('faq.items', { returnObjects: true }) as Array<{
        q: string;
        a: string;
        defaultOpen: boolean;
    }>;

    return (
        <section id="faq" className="py-20">
            <div className="mx-auto max-w-3xl px-6">
                <div className="mb-12 text-center">
                    <span className="text-sm font-semibold text-primary">{t('faq.tag')}</span>
                    <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                        {t('faq.heading')}
                    </h2>
                    <p className="mt-3 text-muted-foreground">
                        {t('faq.subtitle')}{' '}
                        <a href="#" className="text-primary hover:underline">
                            {t('faq.contactLabel')}
                        </a>
                    </p>
                </div>

                <div className="space-y-3">
                    {items.map((item, i) => (
                        <details
                            key={i}
                            open={item.defaultOpen}
                            className="group rounded-xl border border-border bg-card"
                        >
                            <summary className="flex cursor-pointer items-center justify-between rounded-xl p-5 hover:bg-secondary/50">
                                <span className="font-heading font-semibold">{item.q}</span>
                                <span className="faq-icon text-2xl font-light text-primary">+</span>
                            </summary>
                            <div className="px-5 pb-5 text-sm leading-relaxed text-muted-foreground">
                                {item.a}
                            </div>
                        </details>
                    ))}
                </div>

                <div className="mt-8 text-center">
                    <Link to="/faq" className="font-medium text-primary hover:underline">
                        {t('faq.viewAll')}
                    </Link>
                </div>
            </div>
        </section>
    );
}
