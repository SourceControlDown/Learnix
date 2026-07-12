import { useFieldArray } from 'react-hook-form';
import type { Control, UseFormRegister, UseFormSetValue, UseFormWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Trash2 } from 'lucide-react';
import { ChoiceIndicator } from '@/components/common/form/ChoiceIndicator';
import { Input } from '@/components/ui/input';
import type { TestLessonFormData } from '@/schemas/lesson.schema';
import { cn } from '@/utils/cn';

interface ChoiceEditorProps {
    qIdx: number;
    qType: 'SingleChoice' | 'MultipleChoice';
    register: UseFormRegister<TestLessonFormData>;
    control: Control<TestLessonFormData>;
    watch: UseFormWatch<TestLessonFormData>;
    setValue: UseFormSetValue<TestLessonFormData>;
    qError?: {
        options?: { root?: { message?: string }; message?: string };
    };
}

export function ChoiceEditor({
    qIdx,
    qType,
    register,
    control,
    watch,
    setValue,
    qError,
}: ChoiceEditorProps) {
    const { t } = useTranslation('instructor');
    const {
        fields: optionFields,
        append: addOption,
        remove: removeOption,
    } = useFieldArray({
        control,
        name: `questions.${qIdx}.options` as const,
    });

    return (
        <>
            {optionFields.map((optField, oIdx) => {
                const isCorrectField = `questions.${qIdx}.options.${oIdx}.isCorrect` as const;
                return (
                    <div key={optField.id} className="flex items-center gap-2">
                        {/* The input is real but hidden — the browser's own control is a white disc in the
                            dark theme. The circle beside it is ours (ChoiceIndicator). */}
                        <label className="group flex cursor-pointer items-center">
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
                                    className="sr-only"
                                />
                            ) : (
                                <input
                                    type="checkbox"
                                    {...register(isCorrectField)}
                                    className="sr-only"
                                />
                            )}
                            <ChoiceIndicator
                                type={qType === 'SingleChoice' ? 'radio' : 'checkbox'}
                                checked={!!watch(isCorrectField)}
                            />
                        </label>
                        <Input
                            variant="card"
                            {...register(`questions.${qIdx}.options.${oIdx}.text` as const)}
                            placeholder={t('fieldOptionText')}
                            className="h-8 flex-1 py-1"
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
            {qError?.options && (
                <p className="text-xs text-destructive">
                    {qError.options.root?.message ?? qError.options.message}
                </p>
            )}
        </>
    );
}
