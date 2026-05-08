import { cn } from '@/utils/cn';
import type { QuestionDto, SubmitAttemptResponse } from '@/types/lesson.types';
import { TEST_LESSON } from '@/const/localization/testLesson';

interface QuestionCardProps {
    question: QuestionDto;
    index: number;
    total: number;
    selectedOptions: number[];
    textValue: string;
    onOptionToggle: (optionOrder: number) => void;
    onTextChange: (value: string) => void;
    result?: SubmitAttemptResponse['questionResults'][number];
    readonly?: boolean;
}

export function QuestionCard({
    question,
    index,
    total,
    selectedOptions,
    textValue,
    onOptionToggle,
    onTextChange,
    result,
    readonly = false,
}: QuestionCardProps) {
    const isCorrect = result?.isCorrect;

    return (
        <div
            className={cn(
                'rounded-xl border p-6 transition-colors',
                result !== undefined
                    ? isCorrect
                        ? 'border-success/40 bg-success/5'
                        : 'border-destructive/40 bg-destructive/5'
                    : 'border-border bg-card',
            )}
        >
            <div className="mb-4 flex items-start justify-between gap-4">
                <div className="flex items-start gap-3">
                    <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-secondary text-xs font-semibold text-muted-foreground">
                        {index + 1}
                    </span>
                    <p className="font-medium leading-relaxed">{question.text}</p>
                </div>
                {result !== undefined && (
                    <span
                        className={cn(
                            'shrink-0 rounded-full px-2.5 py-0.5 text-xs font-medium',
                            isCorrect
                                ? 'bg-success/15 text-success'
                                : 'bg-destructive/15 text-destructive',
                        )}
                    >
                        {isCorrect ? TEST_LESSON.RESULTS.correct : TEST_LESSON.RESULTS.incorrect}
                    </span>
                )}
            </div>

            <p className="mb-3 ml-9 text-xs text-muted-foreground">
                {TEST_LESSON.FORM.questionOf(index + 1, total)}
                {question.type === 'MultipleChoice' && ' · Select all that apply'}
                {question.type === 'SingleChoice' && ' · Select one'}
            </p>

            {(question.type === 'SingleChoice' || question.type === 'MultipleChoice') &&
                question.options && (
                    <ul className="ml-9 space-y-2">
                        {question.options
                            .slice()
                            .sort((a, b) => a.order - b.order)
                            .map((option) => {
                                const isSelected = selectedOptions.includes(option.order);
                                const inputType =
                                    question.type === 'SingleChoice' ? 'radio' : 'checkbox';

                                return (
                                    <li key={option.order}>
                                        <label
                                            className={cn(
                                                'flex cursor-pointer items-center gap-3 rounded-lg border px-4 py-3 text-sm transition-colors',
                                                readonly && 'cursor-default',
                                                isSelected
                                                    ? 'border-primary bg-primary/5'
                                                    : 'border-border hover:border-primary/50 hover:bg-secondary',
                                            )}
                                        >
                                            <input
                                                type={inputType}
                                                disabled={readonly}
                                                checked={isSelected}
                                                onChange={() => onOptionToggle(option.order)}
                                                className="accent-primary"
                                            />
                                            <span>{option.text}</span>
                                        </label>
                                    </li>
                                );
                            })}
                    </ul>
                )}

            {question.type === 'TextInput' && (
                <div className="ml-9">
                    <textarea
                        value={textValue}
                        onChange={(e) => onTextChange(e.target.value)}
                        disabled={readonly}
                        placeholder={TEST_LESSON.FORM.textPlaceholder}
                        rows={3}
                        className="w-full resize-none rounded-lg border border-border bg-background px-4 py-3 text-sm placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:cursor-default disabled:opacity-70"
                    />
                </div>
            )}
        </div>
    );
}
