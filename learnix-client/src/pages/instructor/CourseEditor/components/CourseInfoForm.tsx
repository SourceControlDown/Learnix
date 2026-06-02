import { useState, KeyboardEvent } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { X } from 'lucide-react';
import { courseInfoSchema, type CourseInfoFormData } from '@/schemas/course.schema';
import { COURSE_LIMITS } from '@/const/course.constants';
import { CoverImageUploader } from './CoverImageUploader';
import { useCategories } from '@/hooks/useCategories';
import { INSTRUCTOR } from '@/const/localization/instructor';
import type { CourseForEditDto } from '@/types/course.types';

interface Props {
    course?: CourseForEditDto;
    isPending: boolean;
    onSubmit: (data: CourseInfoFormData) => void;
}

export function CourseInfoForm({ course, isPending, onSubmit }: Props) {
    const { data: categories = [] } = useCategories();

    const {
        register,
        handleSubmit,
        control,
        setValue,
        watch,
        formState: { errors },
    } = useForm<CourseInfoFormData>({
        resolver: zodResolver(courseInfoSchema),
        defaultValues: {
            title: course?.title ?? '',
            description: course?.description ?? '',
            categoryId: course?.categoryId ?? '',
            price: course?.price ?? 0,
            coverImageUrl: course?.coverImageUrl ?? null,
            tags: course?.tags ?? [],
        },
    });

    const tags = watch('tags');
    const [tagInput, setTagInput] = useState('');

    function addTag(e: KeyboardEvent<HTMLInputElement>) {
        if (e.key !== 'Enter' && e.key !== ',') return;
        e.preventDefault();
        const tag = tagInput.trim().toLowerCase();
        if (tag && !tags.includes(tag) && tags.length < COURSE_LIMITS.TAGS_MAX_COUNT) {
            setValue('tags', [...tags, tag]);
        }
        setTagInput('');
    }

    function removeTag(tag: string) {
        setValue(
            'tags',
            tags.filter((t) => t !== tag),
        );
    }

    return (
        <form id="course-info-form" onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {/* Title */}
            <div>
                <label className="mb-1 block text-sm font-medium text-foreground">
                    {INSTRUCTOR.FIELD_TITLE}
                </label>
                <input
                    {...register('title')}
                    placeholder={INSTRUCTOR.FIELD_TITLE_PLACEHOLDER}
                    className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                {errors.title && (
                    <p className="mt-1 text-xs text-destructive">{errors.title.message}</p>
                )}
            </div>

            {/* Description */}
            <div>
                <label className="mb-1 block text-sm font-medium text-foreground">
                    {INSTRUCTOR.FIELD_DESCRIPTION}
                </label>
                <textarea
                    {...register('description')}
                    rows={4}
                    placeholder={INSTRUCTOR.FIELD_DESCRIPTION_PLACEHOLDER}
                    className="w-full resize-none rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                {errors.description && (
                    <p className="mt-1 text-xs text-destructive">{errors.description.message}</p>
                )}
            </div>

            {/* Category + Price */}
            <div className="grid grid-cols-2 gap-4">
                <div>
                    <label className="mb-1 block text-sm font-medium text-foreground">
                        {INSTRUCTOR.FIELD_CATEGORY}
                    </label>
                    <select
                        {...register('categoryId')}
                        className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    >
                        <option value="">{INSTRUCTOR.FIELD_CATEGORY_PLACEHOLDER}</option>
                        {categories.map((cat) => (
                            <option key={cat.id} value={cat.id}>
                                {cat.name}
                            </option>
                        ))}
                    </select>
                    {errors.categoryId && (
                        <p className="mt-1 text-xs text-destructive">{errors.categoryId.message}</p>
                    )}
                </div>
                <div>
                    <label className="mb-1 block text-sm font-medium text-foreground">
                        {INSTRUCTOR.FIELD_PRICE}
                    </label>
                    <input
                        {...register('price')}
                        type="number"
                        min={COURSE_LIMITS.PRICE_MIN}
                        step={0.01}
                        placeholder={INSTRUCTOR.FIELD_PRICE_PLACEHOLDER}
                        className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                    {errors.price && (
                        <p className="mt-1 text-xs text-destructive">{errors.price.message}</p>
                    )}
                </div>
            </div>

            {/* Tags */}
            <div>
                <label className="mb-1 block text-sm font-medium text-foreground">
                    {INSTRUCTOR.FIELD_TAGS}
                </label>
                <div className="flex min-h-[42px] flex-wrap gap-2 rounded-lg border border-input bg-background px-3 py-2">
                    {tags.map((tag) => (
                        <span
                            key={tag}
                            className="flex items-center gap-1 rounded bg-primary/10 px-2 py-0.5 text-sm text-primary"
                        >
                            {tag}
                            <button
                                type="button"
                                onClick={() => removeTag(tag)}
                                className="leading-none"
                            >
                                <X size={12} />
                            </button>
                        </span>
                    ))}
                    <input
                        value={tagInput}
                        onChange={(e) => setTagInput(e.target.value)}
                        onKeyDown={addTag}
                        placeholder={tags.length === 0 ? INSTRUCTOR.FIELD_TAGS_PLACEHOLDER : ''}
                        className="flex-1 bg-transparent text-sm outline-none"
                    />
                </div>
                {errors.tags && (
                    <p className="mt-1 text-xs text-destructive">{errors.tags.message as string}</p>
                )}
            </div>

            {/* Cover image */}
            <Controller
                control={control}
                name="coverImageUrl"
                render={({ field }) => (
                    <CoverImageUploader
                        value={field.value ?? null}
                        onChange={(path) => field.onChange(path)}
                    />
                )}
            />

            {/* Submit is triggered by the editor page header button via form id */}
        </form>
    );
}
