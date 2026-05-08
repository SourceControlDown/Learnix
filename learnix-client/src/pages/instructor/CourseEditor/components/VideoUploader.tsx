import { useRef } from 'react';
import { Video } from 'lucide-react';
import { cn } from '@/utils/cn';
import { useRequestUploadUrl } from '@/hooks/useRequestUploadUrl';
import { INSTRUCTOR } from '@/const/localization/instructor';

interface Props {
    value: string;
    onChange: (blobPath: string) => void;
}

export function VideoUploader({ value, onChange }: Props) {
    const inputRef = useRef<HTMLInputElement>(null);
    const { uploadFile, isUploading, error } = useRequestUploadUrl();

    async function handleFile(file: File) {
        if (!file.type.startsWith('video/')) return;
        try {
            const blobPath = await uploadFile('LessonVideo', file);
            onChange(blobPath);
        } catch {
            // error surfaced via hook state
        }
    }

    function onInputChange(e: React.ChangeEvent<HTMLInputElement>) {
        const file = e.target.files?.[0];
        if (file) handleFile(file);
    }

    function onDrop(e: React.DragEvent) {
        e.preventDefault();
        const file = e.dataTransfer.files?.[0];
        if (file) handleFile(file);
    }

    return (
        <div className="space-y-1.5">
            <label className="block text-sm font-medium text-foreground">
                {INSTRUCTOR.FIELD_VIDEO}
            </label>
            <div
                onClick={() => !isUploading && inputRef.current?.click()}
                onDrop={onDrop}
                onDragOver={(e) => e.preventDefault()}
                className={cn(
                    'flex cursor-pointer flex-col items-center gap-2 rounded-lg border-2 border-dashed border-border bg-muted p-8 text-muted-foreground transition-colors hover:border-primary',
                    isUploading && 'cursor-wait opacity-70',
                )}
            >
                <Video size={28} className="opacity-50" />
                {isUploading ? (
                    <span className="text-sm">{INSTRUCTOR.VIDEO_UPLOADING}</span>
                ) : value ? (
                    <span className="text-sm font-medium text-success">
                        {INSTRUCTOR.VIDEO_UPLOADED}
                    </span>
                ) : (
                    <span className="text-sm">{INSTRUCTOR.VIDEO_HINT}</span>
                )}
            </div>
            {error && <p className="text-xs text-destructive">{error}</p>}
            <input
                ref={inputRef}
                type="file"
                accept="video/mp4,video/webm"
                className="hidden"
                onChange={onInputChange}
            />
        </div>
    );
}
