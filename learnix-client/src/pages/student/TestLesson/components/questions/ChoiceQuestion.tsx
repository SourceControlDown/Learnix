import { useTranslation } from 'react-i18next';
import { ChoiceIndicator } from '@/components/common/form/ChoiceIndicator';
import type { QuestionOptionDto, QuestionResultDto } from '@/types/lesson.types';
import { cn } from '@/utils/cn';

interface ChoiceQuestionProps {
    type: 'SingleChoice' | 'MultipleChoice';
    options: QuestionOptionDto[];
    selectedOptions: number[];
    onOptionToggle: (optionOrder: number) => void;
    result?: QuestionResultDto;
    readonly?: boolean;
}

export function ChoiceQuestion({
    type,
    options,
    selectedOptions,
    onOptionToggle,
    result,
    readonly = false,
}: ChoiceQuestionProps) {
    const { t } = useTranslation('testLesson');
    const correctOrders = new Set(result?.correctOptionOrders ?? []);
    const hasResult = result !== undefined;

    return (
        <ul className="ml-9 space-y-2">
            {options
                .slice()
                .sort((a, b) => a.order - b.order)
                .map((option) => {
                    const isSelected = selectedOptions.includes(option.order);
                    const isCorrectOption = correctOrders.has(option.order);
                    const inputType = type === 'SingleChoice' ? 'radio' : 'checkbox';

                    // Styling in readonly review mode:
                    // - Selected + correct → green
                    // - Selected + wrong   → red
                    // - Not selected + correct → green outline (show the right answer)
                    // - Not selected + wrong   → neutral
                    const reviewStyle = hasResult
                        ? isSelected && isCorrectOption
                            ? 'border-success bg-success/10 text-success'
                            : isSelected && !isCorrectOption
                              ? 'border-destructive bg-destructive/10 text-destructive'
                              : isCorrectOption
                                ? 'border-success/50 bg-success/5'
                                : 'border-border opacity-60'
                        : null;

                    // The control says the same thing the card does: right in green, wrong in red.
                    const tone: 'default' | 'success' | 'destructive' = !hasResult
                        ? 'default'
                        : isCorrectOption
                          ? 'success'
                          : 'destructive';

                    return (
                        <li key={option.order}>
                            <label
                                className={cn(
                                    'group flex cursor-pointer items-center gap-3 rounded-lg border px-4 py-3 text-sm transition-colors',
                                    readonly && 'cursor-default',
                                    !hasResult && isSelected
                                        ? 'border-primary bg-primary/5'
                                        : !hasResult
                                          ? 'border-border hover:border-primary/50 hover:bg-secondary'
                                          : '',
                                    reviewStyle,
                                )}
                            >
                                {/* Real input, hidden: the browser's own control is a white disc in the
                                    dark theme. Keyboard and screen readers still get a genuine radio. */}
                                <input
                                    type={inputType}
                                    disabled={readonly}
                                    checked={isSelected}
                                    onChange={() => onOptionToggle(option.order)}
                                    className="sr-only"
                                />
                                <ChoiceIndicator
                                    type={inputType}
                                    checked={isSelected}
                                    tone={tone}
                                />
                                <span>{option.text}</span>
                                {hasResult && isCorrectOption && (
                                    <span className="ml-auto text-xs font-medium text-success">
                                        {t('results.correctAnswer')}
                                    </span>
                                )}
                            </label>
                        </li>
                    );
                })}
        </ul>
    );
}
