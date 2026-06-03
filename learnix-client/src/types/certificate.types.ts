export interface MyCertificateDto {
    certificateId: string;
    courseId: string;
    courseTitle: string;
    courseCoverBlobPath: string | null;
    code: string;
    issuedAt: string;
    isReady: boolean;
    downloadUrl: string | null;
    verificationUrl: string;
}

export interface CourseCertificateResponse {
    certificateId: string;
    code: string;
    issuedAt: string;
    isReady: boolean;
    downloadUrl: string | null;
    verificationUrl: string;
}

export interface CertificateReadyNotification {
    certificateId: string;
    courseTitle: string;
}
