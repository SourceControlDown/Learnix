import React, { useEffect, useRef } from 'react';
import { Loader2, Trash2, Upload } from 'lucide-react';
import { toast } from 'sonner';
import categoryFallback from '@/assets/categories/fallback.webp';
import { ImageCropperDialog } from '@/components/common/upload/ImageCropperDialog';
import { acceptAttr } from '@/const/upload.constants';
import { useImageCropUpload } from '@/hooks/shared/useImageCropUpload';

type ThumbnailCellProps = {
    imageUrl: string | null;
    isEditing: boolean;
    previewUrl?: string;
    onUpload?: (blobPath: string, previewUrl: string) => void;
    onRemoveImage?: () => void;
};

export function ThumbnailCell({
    imageUrl,
    isEditing,
    previewUrl,
    onUpload,
    onRemoveImage,
}: ThumbnailCellProps) {
    const fileRef = useRef<HTMLInputElement>(null);
    const { selectFile, isUploading, error, cropper } = useImageCropUpload(
        'CategoryImage',
        (blobPath, preview) => onUpload?.(blobPath, preview),
    );

    // The cell has no room for an inline message, so validation failures go to a toast.
    useEffect(() => {
        if (error) toast.error(error);
    }, [error]);

    const handleFile = (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        // Reset the input so picking the same file twice still fires a change event.
        e.target.value = '';
        if (file && onUpload) selectFile(file);
    };

    // The placeholder is the same picture the public site falls back to, so an admin looking at a
    // category with no image of its own sees exactly what a visitor sees.
    const displayUrl = previewUrl || imageUrl;

    if (!isEditing) {
        return (
            <img
                src={displayUrl ?? categoryFallback}
                alt=""
                className="size-10 rounded object-cover"
            />
        );
    }

    return (
        <div className="group relative flex size-10 items-center justify-center overflow-hidden rounded bg-muted hover:bg-muted/80">
            {isUploading ? (
                <Loader2 size={16} className="animate-spin text-muted-foreground" />
            ) : displayUrl ? (
                <>
                    <img src={displayUrl} alt="" className="size-full object-cover" />
                    <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/50 opacity-0 transition-opacity group-hover:opacity-100">
                        <button
                            type="button"
                            className="flex h-1/2 w-full items-center justify-center text-white hover:bg-white/20"
                            onClick={() => fileRef.current?.click()}
                            title="Upload new image"
                        >
                            <Upload size={14} />
                        </button>
                        <button
                            type="button"
                            className="flex h-1/2 w-full items-center justify-center text-destructive hover:bg-white/20"
                            onClick={(e) => {
                                e.stopPropagation();
                                onRemoveImage?.();
                            }}
                            title="Remove image"
                        >
                            <Trash2 size={14} />
                        </button>
                    </div>
                </>
            ) : (
                <button
                    type="button"
                    title="Upload image"
                    className="flex size-10 cursor-pointer items-center justify-center rounded"
                    onClick={() => fileRef.current?.click()}
                >
                    <img src={categoryFallback} alt="" className="size-full object-cover" />
                    <div className="absolute inset-0 flex items-center justify-center bg-black/50 opacity-0 transition-opacity group-hover:opacity-100">
                        <Upload size={14} className="text-white" />
                    </div>
                </button>
            )}
            <input
                type="file"
                ref={fileRef}
                className="hidden"
                accept={acceptAttr('CategoryImage')}
                onChange={handleFile}
            />

            <ImageCropperDialog {...cropper} />
        </div>
    );
}
