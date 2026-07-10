import type {
    Control,
    FieldErrors,
    UseFormRegister,
    UseFormSetValue,
    UseFormWatch,
} from 'react-hook-form';
import { Controller } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Trash2 } from 'lucide-react';
import { FormSelect } from '@/components/common/form/FormSelect';
import { Input } from '@/components/ui/input';
import type { TestLessonFormData } from '@/schemas/lesson.schema';
import { ChoiceEditor } from './editors/ChoiceEditor';
import { TextInputEditor } from './editors/TextInputEditor';

export type QuestionEditorProps = {
    qIdx: number;
    register: UseFormRegister<TestLessonFormData>;
    control: Control<TestLessonFormData>;
    watch: UseFormWatch<TestLessonFormData>;
    setValue: UseFormSetValue<TestLessonFormData>;
    errors: FieldErrors<TestLessonFormData>;
    onRemove: () => void;
};

export function QuestionEditor({
    qIdx,
    register,
    control,
    watch,
    setValue,
    errors,
    onRemove,
}: QuestionEditorProps) {
    const { t } = useTranslation('instructor');

    const qType = watch(`questions.${qIdx}.type`);

    type QuestionErrorExt = {
        text?: { message?: string };
        options?: { root?: { message?: string }; message?: string };
        textAnswer?: { correctAnswer?: { message?: string } };
    };
    const qError = (errors.questions || [])[qIdx] as unknown as QuestionErrorExt;

    return (
        <div className="space-y-3 rounded-lg border border-border p-4">
            <div className="space-y-2">
                <div className="flex items-center gap-1.5">
                    <Input
                        variant="card"
                        {...register(`questions.${qIdx}.text` as const)}
                        placeholder={t('fieldQuestionText')}
                        className="flex-1"
                    />
                    <Controller
                        name={`questions.${qIdx}.type` as const}
                        control={control}
                        render={({ field }) => (
                            <FormSelect
                                variant="card"
                                containerClassName="shrink-0"
                                triggerClassName="w-[160px]"
                                value={field.value}
                                onValueChange={field.onChange}
                                options={[
                                    { value: 'SingleChoice', label: t('questionTypeSingle') },
                                    { value: 'MultipleChoice', label: t('questionTypeMultiple') },
                                    { value: 'TextInput', label: t('questionTypeTextInput') },
                                ]}
                            />
                        )}
                    />
                    <button
                        type="button"
                        onClick={onRemove}
                        className="flex size-9 shrink-0 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive"
                        title={t('btnDeleteQuestion')}
                    >
                        <Trash2 size={16} />
                    </button>
                </div>
                {qError?.text && (
                    <p className="pl-1 text-xs text-destructive">{qError.text.message}</p>
                )}
            </div>

            <div className="space-y-2 pl-2">
                {qType === 'TextInput' ? (
                    <TextInputEditor qIdx={qIdx} register={register} qError={qError} />
                ) : (
                    <ChoiceEditor
                        qIdx={qIdx}
                        qType={qType}
                        register={register}
                        control={control}
                        watch={watch}
                        setValue={setValue}
                        qError={qError}
                    />
                )}
            </div>
        </div>
    );
}
