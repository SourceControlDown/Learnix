import { Link } from 'react-router-dom';

export function Footer() {
    return (
        <footer className="border-t border-border bg-card pb-8 pt-16">
            <div className="mx-auto max-w-7xl px-6">
                <div className="flex flex-col items-start gap-4 pt-8 text-sm text-muted-foreground md:flex-row md:items-center md:justify-between">
                    <Link to="/" className="flex items-center gap-2">
                        <div className="grid h-7 w-7 place-items-center rounded-md bg-primary font-heading font-bold text-primary-foreground">
                            L
                        </div>
                        <span className="font-heading font-bold text-foreground">Learnix</span>
                    </Link>
                    <div>© 2026 Learnix. Portfolio project — not affiliated with any commercial LMS.</div>
                </div>
            </div>
        </footer>
    );
}
