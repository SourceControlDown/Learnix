import { GraduationCap, Download, Link as LinkIcon, Clock } from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { useMyCertificates } from '@/hooks/useMyCertificates';
import { cn } from '@/utils/cn';

export default function CertificatesPage() {
    const { t } = useTranslation('certificates');
    const { data: certificates, isLoading } = useMyCertificates();

    function copyLink(url: string) {
        navigator.clipboard.writeText(url);
        toast.success(t('messages.linkCopied'));
    }

    if (isLoading) {
        return (
            <div className="mx-auto max-w-4xl px-6 py-12">
                <div className="animate-pulse space-y-4">
                    <div className="h-8 w-56 rounded bg-muted" />
                    {Array.from({ length: 3 }).map((_, i) => (
                        <div key={i} className="h-24 rounded-xl bg-muted" />
                    ))}
                </div>
            </div>
        );
    }

    return (
        <div className="mx-auto max-w-4xl px-6 py-12">
            <div>
                <h1 className="font-heading text-3xl font-bold text-foreground">
                    {t('pageTitle')}
                </h1>
                <p className="mt-1 text-muted-foreground">{t('subtitle')}</p>
            </div>

            {!certificates || certificates.length === 0 ? (
                <div className="mt-16 flex flex-col items-center gap-4 text-center">
                    <div className="flex h-20 w-20 items-center justify-center rounded-full bg-muted">
                        <GraduationCap className="h-10 w-10 text-muted-foreground" />
                    </div>
                    <h2 className="font-heading text-xl font-semibold">{t('emptyTitle')}</h2>
                    <p className="max-w-sm text-muted-foreground">{t('emptyDescription')}</p>
                </div>
            ) : (
                <div className="mt-8 space-y-4">
                    {certificates.map((cert) => (
                        <div
                            key={cert.certificateId}
                            className="flex items-center gap-5 rounded-xl border border-border bg-card p-5 transition-shadow hover:shadow-sm"
                        >
                            {/* Cover / icon */}
                            <div
                                className={cn(
                                    'flex h-16 w-16 shrink-0 items-center justify-center rounded-lg',
                                    cert.courseCoverBlobPath ? 'overflow-hidden' : 'bg-accent/10',
                                )}
                            >
                                {cert.courseCoverBlobPath ? (
                                    <img
                                        src={cert.courseCoverBlobPath}
                                        alt={cert.courseTitle}
                                        className="h-full w-full object-cover"
                                    />
                                ) : (
                                    <GraduationCap className="h-8 w-8 text-accent" />
                                )}
                            </div>

                            {/* Info */}
                            <div className="min-w-0 flex-1">
                                <p className="line-clamp-1 font-heading font-semibold text-foreground">
                                    {cert.courseTitle}
                                </p>
                                <p className="mt-0.5 text-sm text-muted-foreground">
                                    {t('issuedOn')}{' '}
                                    {new Date(cert.issuedAt).toLocaleDateString('en-US', {
                                        month: 'long',
                                        day: 'numeric',
                                        year: 'numeric',
                                    })}
                                </p>
                                {!cert.isReady && (
                                    <p className="mt-1 flex items-center gap-1 text-xs text-warning">
                                        <Clock className="h-3 w-3" />
                                        {t('status.generatingHint')}
                                    </p>
                                )}
                            </div>

                            {/* Actions */}
                            <div className="flex shrink-0 items-center gap-2">
                                <button
                                    type="button"
                                    onClick={() => copyLink(cert.verificationUrl)}
                                    title={t('actions.share')}
                                    className="flex h-9 w-9 items-center justify-center rounded-lg border border-border text-muted-foreground transition-colors hover:border-primary hover:text-primary"
                                >
                                    <LinkIcon className="h-4 w-4" />
                                </button>

                                {cert.isReady && cert.downloadUrl ? (
                                    <a
                                        href={cert.downloadUrl}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90"
                                    >
                                        <Download className="h-4 w-4" />
                                        {t('actions.download')}
                                    </a>
                                ) : (
                                    <span className="inline-flex items-center gap-1 rounded-lg bg-muted px-4 py-2 text-sm text-muted-foreground">
                                        <Clock className="h-4 w-4" />
                                        {t('status.generating')}
                                    </span>
                                )}
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
