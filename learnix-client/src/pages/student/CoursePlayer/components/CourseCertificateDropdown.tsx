import { useState, useRef, useEffect } from 'react';
import { Trophy, ChevronDown } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { CourseCertificateButton } from '@/components/common/CourseCertificateButton';

interface CourseCertificateDropdownProps {
    courseId: string;
    completedLessons: number;
    totalLessons: number;
}

export function CourseCertificateDropdown({
    courseId,
    completedLessons,
    totalLessons,
}: CourseCertificateDropdownProps) {
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef<HTMLDivElement>(null);
    const { t } = useTranslation('lessonPlayer');

    const isCompleted = completedLessons === totalLessons && totalLessons > 0;

    useEffect(() => {
        function handleClickOutside(event: MouseEvent) {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
                setIsOpen(false);
            }
        }

        if (isOpen) {
            document.addEventListener('mousedown', handleClickOutside);
        }
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [isOpen]);

    return (
        <div className="relative" ref={dropdownRef}>
            <button
                type="button"
                onClick={() => setIsOpen(!isOpen)}
                className="flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
            >
                <Trophy className="h-4 w-4" />
                <span className="hidden sm:inline-block">Get course certificate</span>
                <ChevronDown className={`h-3 w-3 transition-transform ${isOpen ? 'rotate-180' : ''}`} />
            </button>

            {isOpen && (
                <div className="absolute right-0 top-full mt-2 w-64 rounded-xl border border-border bg-card p-4 shadow-lg z-50 animate-in fade-in zoom-in-95 duration-200">
                    <p className="mb-4 text-sm font-medium text-foreground">
                        {completedLessons} of {totalLessons} complete.
                    </p>
                    {isCompleted ? (
                        <CourseCertificateButton courseId={courseId} variant="primary" />
                    ) : (
                        <button
                            type="button"
                            disabled
                            className="w-full rounded-lg bg-primary/50 px-4 py-2 text-sm font-medium text-primary-foreground opacity-50 cursor-not-allowed"
                        >
                            Get certificate
                        </button>
                    )}
                </div>
            )}
        </div>
    );
}
