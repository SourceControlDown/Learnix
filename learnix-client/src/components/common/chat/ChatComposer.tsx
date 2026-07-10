import {
    type KeyboardEvent,
    useCallback,
    useEffect,
    useLayoutEffect,
    useRef,
    useState,
} from 'react';
import { useTranslation } from 'react-i18next';
import { Send } from 'lucide-react';
import { cn } from '@/utils/cn';

/** Matches the textarea's `text-sm` line box; used to cap autogrow at `maxRows`. */
const LINE_HEIGHT = 24;
const VERTICAL_PADDING = 20;

/** Remaining characters at which the counter appears. */
const COUNTER_THRESHOLD = 200;

interface ChatComposerProps {
    onSend: (value: string) => void;
    /** Already translated — the composer stays unaware of i18n namespaces. */
    placeholder: string;
    disabled?: boolean;
    /** Caps the input and reveals a counter near the limit. Omit for no limit. */
    maxLength?: number;
    /** Overrides the send button's aria-label. Defaults to `common:actions.send`. */
    sendLabel?: string;
    maxRows?: number;
    className?: string;
}

/**
 * The single chat input across the app: AI tutor, student ↔ instructor conversation,
 * and the landing page mock. Renders only the composer row and its counter — the border,
 * padding and max-width belong to the host, so it drops into a floating widget, a docked
 * panel and a full-width page without any of them fighting its styles.
 */
export function ChatComposer({
    onSend,
    placeholder,
    disabled = false,
    maxLength,
    sendLabel,
    maxRows = 6,
    className,
}: ChatComposerProps) {
    const { t } = useTranslation('common');
    const [value, setValue] = useState('');
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    const maxHeight = LINE_HEIGHT * maxRows + VERTICAL_PADDING;
    const lastWidth = useRef(0);

    /**
     * Skips zero-width elements: a `scrollHeight` read on a collapsed or `display: none`
     * textarea reports 0, or — because the wrapped placeholder counts towards it — the
     * full multi-line height. Either would be frozen in as an inline height, since the
     * only other trigger is a change of `value`.
     */
    const autogrow = useCallback(() => {
        const el = textareaRef.current;
        if (!el || el.clientWidth === 0) return;
        el.style.height = 'auto';
        el.style.height = `${Math.min(el.scrollHeight, maxHeight)}px`;
        el.style.overflowY = el.scrollHeight > maxHeight ? 'auto' : 'hidden';
    }, [maxHeight]);

    // Reacts to `value` rather than `onInput`, so a programmatic reset resizes too.
    useLayoutEffect(autogrow, [value, autogrow]);

    // Re-measures once the host lays the composer out: a tab is revealed, a docked panel
    // gets its width, the user drags a resize handle.
    useEffect(() => {
        const el = textareaRef.current;
        if (!el) return;

        const observer = new ResizeObserver(([entry]) => {
            const width = entry.contentRect.width;
            if (width === lastWidth.current) return;
            lastWidth.current = width;
            autogrow();
        });
        observer.observe(el);

        return () => observer.disconnect();
    }, [autogrow]);

    function handleSend() {
        const trimmed = value.trim();
        if (!trimmed || disabled) return;
        onSend(trimmed);
        setValue('');
    }

    function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            handleSend();
        }
    }

    const remaining = maxLength === undefined ? null : maxLength - value.length;
    const isNearLimit = remaining !== null && remaining <= COUNTER_THRESHOLD;

    return (
        <div className={cn('flex flex-col gap-1', className)}>
            <div
                className={cn(
                    'flex items-end gap-2 rounded-lg border border-border bg-background px-2 py-1.5',
                    'focus-within:border-primary focus-within:ring-1 focus-within:ring-primary',
                )}
            >
                <textarea
                    ref={textareaRef}
                    rows={1}
                    value={value}
                    maxLength={maxLength}
                    placeholder={placeholder}
                    disabled={disabled}
                    onChange={(event) => setValue(event.target.value)}
                    onKeyDown={handleKeyDown}
                    style={{ overflowY: 'hidden' }}
                    className={cn(
                        'flex-1 resize-none bg-transparent px-1 py-1.5 text-sm text-foreground',
                        'placeholder:text-muted-foreground focus:outline-none disabled:opacity-50',
                    )}
                />
                <button
                    type="button"
                    onClick={handleSend}
                    disabled={disabled || !value.trim()}
                    aria-label={sendLabel ?? t('actions.send')}
                    className={cn(
                        'mb-0.5 flex size-8 shrink-0 items-center justify-center rounded-md',
                        'bg-primary text-primary-foreground transition-opacity',
                        'hover:opacity-90 disabled:pointer-events-none disabled:opacity-40',
                    )}
                >
                    <Send className="size-4" />
                </button>
            </div>

            {isNearLimit && (
                <p
                    className={cn(
                        'text-right text-xs',
                        remaining <= 0 ? 'text-destructive' : 'text-muted-foreground',
                    )}
                >
                    {remaining}
                </p>
            )}
        </div>
    );
}
