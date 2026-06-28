import React, { useRef } from 'react';
import { User, Camera } from 'lucide-react';
import { useTranslation } from 'react-i18next';

interface AvatarUploadProps {
    firstName?: string;
    lastName?: string;
    displayAvatar: string | null;
    isUploading: boolean;
    onAvatarChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export function AvatarUpload({
    firstName,
    lastName,
    displayAvatar,
    isUploading,
    onAvatarChange,
}: AvatarUploadProps) {
    const { t } = useTranslation('profile');
    const fileInputRef = useRef<HTMLInputElement>(null);

    return (
        <div className="flex shrink-0 flex-col items-center text-center md:w-56 md:items-start md:text-left">
            <div className="relative">
                <div className="flex h-28 w-28 items-center justify-center overflow-hidden rounded-full border-4 border-background bg-muted shadow-sm md:h-32 md:w-32">
                    {displayAvatar !== null ? (
                        <img
                            src={displayAvatar}
                            alt="Avatar"
                            className="h-full w-full object-cover"
                        />
                    ) : (
                        <User className="h-12 w-12 text-muted-foreground md:h-14 md:w-14" />
                    )}
                </div>
                <button
                    type="button"
                    onClick={() => fileInputRef.current?.click()}
                    disabled={isUploading}
                    title={isUploading ? t('avatar.uploading') : t('avatar.upload')}
                    className="absolute bottom-1 right-1 flex h-9 w-9 items-center justify-center rounded-full border-2 border-background bg-accent text-accent-foreground shadow-md transition-transform hover:scale-105 hover:opacity-90 disabled:scale-100 disabled:opacity-50"
                >
                    <Camera className="h-4 w-4" />
                </button>
                <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    className="hidden"
                    onChange={onAvatarChange}
                />
            </div>
            <div className="mt-4">
                <h3 className="font-heading text-lg font-semibold text-foreground">
                    {firstName} {lastName}
                </h3>
                <p className="mt-1 max-w-[200px] text-xs text-muted-foreground">
                    {t('avatar.hint')}
                </p>
            </div>
        </div>
    );
}
