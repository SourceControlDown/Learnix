import { useState, useMemo } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ChevronLeft, ClipboardList, Clock, AlertCircle } from 'lucide-react';
import { cn } from '@/utils/cn';
import { useTestLesson } from '@/hooks/useTestLesson';
import { useSubmitTestAttempt } from '@/hooks/useSubmitTestAttempt';
import { useMyTestAttempts } from '@/hooks/useMyTestAttempts';
import type { SubmitAttemptResponse, SubmittedAnswerDto } from '@/types/lesson.types';
import { QuestionCard } from './components/QuestionCard';
import { TestResults } from './components/TestResults';
import { TestAttemptHistory } from './components/TestAttemptHistory';
import { TEST_LESSON } from '@/const/localization/testLesson';

type PageState = 'idle' | 'submitted';

interface AnswerState {
    selectedOptions: number[];
    textValue: string;
}

export default function TestLessonPage() {
    const { courseId, lessonId } = useParams<{ courseId: string; lessonId: string }>();

    const { data: test, isLoading, isError } = useTestLesson(courseId!, lessonId!);
    const { data: attempts = [] } = useMyTestAttempts(courseId!, lessonId!);
    const submit = useSubmitTestAttempt(courseId!, lessonId!);

    const [pageState, setPageState] = useState<PageState>('idle');
    const [submitResult, setSubmitResult] = useState<SubmitAttemptResponse | null>(null);

    const initialAnswers = useMemo<Record<number, AnswerState>>(() => {
        if (!test) return {};
        return Object.fromEntries(
            test.questions.map((q) => [q.order, { selectedOptions: [], textValue: '' }]),
        );
    }, [test]);

    const [answers, setAnswers] = useState<Record<number, AnswerState>>(initialAnswers);

    const handleOptionToggle = (questionOrder: number, optionOrder: number, type: string) => {
        setAnswers((prev) => {
            const current = prev[questionOrder] ?? { selectedOptions: [], textValue: '' };
            if (type === 'SingleChoice') {
                return {
                    ...prev,
                    [questionOrder]: { ...current, selectedOptions: [optionOrder] },
                };
            }
            const already = current.selectedOptions.includes(optionOrder);
            return {
                ...prev,
                [questionOrder]: {
                    ...current,
                    selectedOptions: already
                        ? current.selectedOptions.filter((o) => o !== optionOrder)
                        : [...current.selectedOptions, optionOrder],
                },
            };
        });
    };

    const handleTextChange = (questionOrder: number, value: string) => {
        setAnswers((prev) => ({
            ...prev,
            [questionOrder]: {
                ...(prev[questionOrder] ?? { selectedOptions: [] }),
                textValue: value,
            },
        }));
    };

    const handleSubmit = () => {
        if (!test) return;

        const submittedAnswers: SubmittedAnswerDto[] = test.questions.map((q) => {
            const ans = answers[q.order] ?? { selectedOptions: [], textValue: '' };
            return {
                questionOrder: q.order,
                selectedOptionOrders: ans.selectedOptions,
                textValue: q.type === 'TextInput' ? ans.textValue || null : null,
            };
        });

        submit.mutate(
            { answers: submittedAnswers },
            {
                onSuccess: (result) => {
                    setSubmitResult(result);
                    setPageState('submitted');
                },
            },
        );
    };

    const handleRetake = () => {
        setPageState('idle');
        setSubmitResult(null);
        if (test) {
            setAnswers(
                Object.fromEntries(
                    test.questions.map((q) => [q.order, { selectedOptions: [], textValue: '' }]),
                ),
            );
        }
    };

    const status = test?.studentStatus;
    const canAttempt = status?.canAttempt ?? true;

    return (
        <div className="min-h-screen bg-background">
            {/* Header */}
            <header className="sticky top-0 z-10 border-b border-border bg-card">
                <div className="mx-auto flex h-14 max-w-4xl items-center justify-between px-6">
                    <Link
                        to={`/courses/${courseId}/learn/${lessonId}`}
                        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
                    >
                        <ChevronLeft className="h-4 w-4" />
                        {TEST_LESSON.HEADER.backToLesson}
                    </Link>
                    <div className="flex items-center gap-2 text-sm font-medium">
                        <ClipboardList className="h-4 w-4 text-primary" />
                        {TEST_LESSON.HEADER.testLabel}
                    </div>
                    <div className="w-24" />
                </div>
            </header>

            <div className="mx-auto max-w-4xl px-6 py-10">
                {/* Loading */}
                {isLoading && (
                    <div className="flex items-center justify-center py-20">
                        <div className="space-y-3 text-center">
                            <div className="mx-auto h-10 w-10 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                            <p className="text-sm text-muted-foreground">{TEST_LESSON.LOADING}</p>
                        </div>
                    </div>
                )}

                {/* Error */}
                {isError && (
                    <div className="flex items-center gap-3 rounded-xl border border-destructive/30 bg-destructive/10 p-6 text-destructive">
                        <AlertCircle className="h-5 w-5 shrink-0" />
                        <p>{TEST_LESSON.ERROR}</p>
                    </div>
                )}

                {/* Content */}
                {!isLoading && !isError && test && (
                    <div className="space-y-8">
                        {/* Test header */}
                        <div>
                            <h1 className="mb-2 font-heading text-2xl font-bold">{test.title}</h1>
                            {test.description && (
                                <p className="text-muted-foreground">{test.description}</p>
                            )}
                            <div className="mt-3 flex flex-wrap gap-4 text-sm text-muted-foreground">
                                <span>
                                    {TEST_LESSON.STATUS.attemptsUsed(
                                        status?.attemptsUsed ?? 0,
                                        test.attemptLimit,
                                    )}
                                </span>
                                <span>
                                    {TEST_LESSON.STATUS.passingScore(test.passingThreshold)}
                                </span>
                                {test.cooldownMinutes && (
                                    <span>
                                        Cooldown: {test.cooldownMinutes} min between attempts
                                    </span>
                                )}
                            </div>
                        </div>

                        {/* Cooldown notice */}
                        {status?.cooldownRemainingMinutes != null && !canAttempt && (
                            <div className="flex items-center gap-3 rounded-xl border border-warning/30 bg-warning/10 px-5 py-4 text-sm">
                                <Clock className="h-5 w-5 shrink-0 text-warning" />
                                <span>
                                    {TEST_LESSON.STATUS.cooldown(status.cooldownRemainingMinutes)}
                                </span>
                            </div>
                        )}

                        {/* No attempts left */}
                        {!canAttempt && status?.cooldownRemainingMinutes == null && (
                            <div className="flex items-center gap-3 rounded-xl border border-destructive/30 bg-destructive/10 px-5 py-4 text-sm text-destructive">
                                <AlertCircle className="h-5 w-5 shrink-0" />
                                {TEST_LESSON.STATUS.noAttemptsLeft}
                            </div>
                        )}

                        {/* Results view */}
                        {pageState === 'submitted' && submitResult && (
                            <TestResults
                                result={submitResult}
                                test={test}
                                courseId={courseId!}
                                lessonId={lessonId!}
                                onRetake={handleRetake}
                                canRetake={
                                    (test.studentStatus?.canAttempt ?? true) === false
                                        ? false
                                        : true
                                }
                            />
                        )}

                        {/* Quiz form */}
                        {pageState === 'idle' && canAttempt && (
                            <div className="space-y-4">
                                {test.questions
                                    .slice()
                                    .sort((a, b) => a.order - b.order)
                                    .map((question, idx) => (
                                        <QuestionCard
                                            key={question.order}
                                            question={question}
                                            index={idx}
                                            total={test.questions.length}
                                            selectedOptions={
                                                answers[question.order]?.selectedOptions ?? []
                                            }
                                            textValue={answers[question.order]?.textValue ?? ''}
                                            onOptionToggle={(optOrder) =>
                                                handleOptionToggle(
                                                    question.order,
                                                    optOrder,
                                                    question.type,
                                                )
                                            }
                                            onTextChange={(val) =>
                                                handleTextChange(question.order, val)
                                            }
                                        />
                                    ))}

                                <div className="flex justify-end pt-4">
                                    <button
                                        type="button"
                                        onClick={handleSubmit}
                                        disabled={submit.isPending}
                                        className={cn(
                                            'rounded-lg px-8 py-3 text-sm font-medium text-primary-foreground transition-colors',
                                            submit.isPending
                                                ? 'cursor-not-allowed bg-primary/60'
                                                : 'bg-primary hover:bg-primary/90',
                                        )}
                                    >
                                        {submit.isPending
                                            ? TEST_LESSON.FORM.submitting
                                            : TEST_LESSON.FORM.submitButton}
                                    </button>
                                </div>
                            </div>
                        )}

                        {/* Attempt history — always shown */}
                        <TestAttemptHistory attempts={attempts} />
                    </div>
                )}
            </div>
        </div>
    );
}
