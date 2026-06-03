import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { testLessonSchema, type TestLessonFormData } from '@/schemas/lesson.schema';
import { LESSON_LIMITS } from '@/const/lesson.constants';
import { cn } from '@/utils/cn';
import type { CourseForEditLessonDto } from '@/types/course.types';

interface Props {
    lesson?: CourseForEditLessonDto;
    isPending: boolean;
    onSubmit: (data: TestLessonFormData) => void;
    onCancel: () => void;
}

export function TestLessonForm({ lesson, isPending, onSubmit, onCancel }: Props) {
    const { t } = useTranslation('instructor');
    const {
        register,
        handleSubmit,
        control,
        watch,
        setValue,
        formState: { errors },
    } = useForm<TestLessonFormData>({
        resolver: zodResolver(testLessonSchema),
        defaultValues: {
            title: lesson?.title ?? '',
            description: lesson?.description ?? '',
            passingThreshold: lesson?.passingThreshold ?? 70,
            attemptLimit: lesson?.attemptLimit ?? undefined,
            cooldownMinutes: lesson?.cooldownMinutes ?? undefined,
            questions: lesson?.questions
                ?.filter((q) => q.type !== 'TextInput')
                .map((q) => ({
                    text: q.text,
                    type: q.type as 'SingleChoice' | 'MultipleChoice',
                    options: q.options.map((o) => ({ text: o.text, isCorrect: o.isCorrect })),
                })) ?? [
                {
                    text: '',
                    type: 'SingleChoice',
                    options: [
                        { text: '', isCorrect: false },
                        { text: '', isCorrect: false },
                    ],
                },
            ],
        },
    });

    const {
        fields: questionFields,
        append: addQuestion,
        remove: removeQuestion,
    } = useFieldArray({
        control,
        name: 'questions',
    });

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            <div>
                <label className="mb-1 block text-sm font-medium">{t('fieldTitle')}</label>
                <input
                    {...register('title')}
                    className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                {errors.title && (
                    <p className="mt-1 text-xs text-destructive">{errors.title.message}</p>
                )}
            </div>

            <div>
                <label className="mb-1 block text-sm font-medium">{t('fieldDescription')}</label>
                <textarea
                    {...register('description')}
                    rows={2}
                    className="w-full resize-none rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
            </div>

            <div className="grid grid-cols-3 items-end gap-4">
                <div>
                    <label className="mb-1 block text-sm font-medium">
                        {t('fieldPassingThreshold')}
                    </label>
                    <input
                        {...register('passingThreshold')}
                        type="number"
                        min={LESSON_LIMITS.PASSING_THRESHOLD_MIN}
                        max={LESSON_LIMITS.PASSING_THRESHOLD_MAX}
                        className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                    {errors.passingThreshold && (
                        <p className="mt-1 text-xs text-destructive">
                            {errors.passingThreshold.message}
                        </p>
                    )}
                </div>
                <div>
                    <label className="mb-1 block text-sm font-medium">
                        {t('fieldAttemptLimit')}
                    </label>
                    <input
                        {...register('attemptLimit')}
                        type="number"
                        min={LESSON_LIMITS.ATTEMPT_LIMIT_MIN}
                        placeholder="∞"
                        className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                </div>
                <div>
                    <label className="mb-1 block text-sm font-medium">{t('fieldCooldown')}</label>
                    <input
                        {...register('cooldownMinutes')}
                        type="number"
                        min={LESSON_LIMITS.COOLDOWN_MINUTES_MIN}
                        className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                </div>
            </div>

            {/* Questions */}
            <div className="space-y-4">
                <div className="flex items-center justify-between">
                    <h4 className="font-medium text-foreground">Questions</h4>
                    <button
                        type="button"
                        onClick={() =>
                            addQuestion({
                                text: '',
                                type: 'SingleChoice',
                                options: [
                                    { text: '', isCorrect: false },
                                    { text: '', isCorrect: false },
                                ],
                            })
                        }
                        className="text-sm text-primary hover:underline"
                    >
                        {t('btnAddQuestion')}
                    </button>
                </div>
                {errors.questions?.root && (
                    <p className="text-xs text-destructive">{errors.questions.root.message}</p>
                )}

                {questionFields.map((qField, qIdx) => (
                    <QuestionEditor
                        key={qField.id}
                        qIdx={qIdx}
                        register={register}
                        control={control}
                        watch={watch}
                        setValue={setValue}
                        errors={errors}
                        onRemove={() => removeQuestion(qIdx)}
                    />
                ))}
            </div>

            <div className="flex justify-end gap-2 pt-2">
                <button
                    type="button"
                    onClick={onCancel}
                    className="rounded-lg border border-border px-4 py-2 text-sm hover:bg-secondary"
                >
                    {t('btnCancel')}
                </button>
                <button
                    type="submit"
                    disabled={isPending}
                    className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
                >
                    {isPending ? '...' : t('btnSaveLesson')}
                </button>
            </div>
        </form>
    );
}

// Extracted to keep the parent manageable
function QuestionEditor({
    qIdx,
    register,
    control,
    watch,
    setValue,
    errors,
    onRemove,
}: {
    qIdx: number;
    register: ReturnType<typeof useForm<TestLessonFormData>>['register'];
    control: ReturnType<typeof useForm<TestLessonFormData>>['control'];
    watch: ReturnType<typeof useForm<TestLessonFormData>>['watch'];
    setValue: ReturnType<typeof useForm<TestLessonFormData>>['setValue'];
    errors: ReturnType<typeof useForm<TestLessonFormData>>['formState']['errors'];
    onRemove: () => void;
}) {
    const { t } = useTranslation('instructor');
    const {
        fields: optionFields,
        append: addOption,
        remove: removeOption,
    } = useFieldArray({
        control,
        name: `questions.${qIdx}.options`,
    });

    const qType = watch(`questions.${qIdx}.type`);

    return (
        <div className="space-y-3 rounded-lg border border-border p-4">
            <div className="flex items-start justify-between gap-3">
                <div className="flex-1 space-y-2">
                    <input
                        {...register(`questions.${qIdx}.text`)}
                        placeholder={t('fieldQuestionText')}
                        className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                    {errors.questions?.[qIdx]?.text && (
                        <p className="text-xs text-destructive">
                            {errors.questions[qIdx].text?.message}
                        </p>
                    )}
                </div>
                <select
                    {...register(`questions.${qIdx}.type`)}
                    className="rounded-lg border border-input bg-background px-3 py-2 text-sm"
                >
                    <option value="SingleChoice">{t('questionTypeSingle')}</option>
                    <option value="MultipleChoice">{t('questionTypeMultiple')}</option>
                </select>
                <button
                    type="button"
                    onClick={onRemove}
                    className="mt-1 text-muted-foreground hover:text-destructive"
                >
                    <Trash2 size={14} />
                </button>
            </div>

            <div className="space-y-2 pl-2">
                {optionFields.map((optField, oIdx) => {
                    const isCorrectField = `questions.${qIdx}.options.${oIdx}.isCorrect` as const;
                    return (
                        <div key={optField.id} className="flex items-center gap-2">
                            {qType === 'SingleChoice' ? (
                                <input
                                    type="radio"
                                    name={`questions.${qIdx}.singleCorrect`}
                                    checked={watch(isCorrectField)}
                                    onChange={() =>
                                        optionFields.forEach((_, i) =>
                                            setValue(
                                                `questions.${qIdx}.options.${i}.isCorrect`,
                                                i === oIdx,
                                                { shouldValidate: true },
                                            ),
                                        )
                                    }
                                    className="accent-primary"
                                />
                            ) : (
                                <input
                                    type="checkbox"
                                    {...register(isCorrectField)}
                                    className="accent-primary"
                                />
                            )}
                            <input
                                {...register(`questions.${qIdx}.options.${oIdx}.text`)}
                                placeholder={t('fieldOptionText')}
                                className="flex-1 rounded border border-input bg-background px-2 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
                            />
                            {optionFields.length > 2 && (
                                <button
                                    type="button"
                                    onClick={() => removeOption(oIdx)}
                                    className="text-muted-foreground hover:text-destructive"
                                >
                                    <Trash2 size={12} />
                                </button>
                            )}
                        </div>
                    );
                })}
                {optionFields.length < 6 && (
                    <button
                        type="button"
                        onClick={() => addOption({ text: '', isCorrect: false })}
                        className={cn('text-xs text-primary hover:underline')}
                    >
                        {t('btnAddOption')}
                    </button>
                )}
                {errors.questions?.[qIdx]?.options && (
                    <p className="text-xs text-destructive">
                        {errors.questions[qIdx].options?.root?.message ??
                            errors.questions[qIdx].options?.message}
                    </p>
                )}
            </div>
        </div>
    );
}
