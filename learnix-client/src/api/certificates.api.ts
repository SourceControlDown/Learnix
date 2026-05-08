import { api } from './axios.instance';
import type { MyCertificateDto, CourseCertificateResponse } from '@/types/certificate.types';

export const certificatesApi = {
    getMyCertificates: () => api.get<MyCertificateDto[]>('/certificates/mine').then((r) => r.data),

    getCourseCertificate: (courseId: string) =>
        api.get<CourseCertificateResponse>(`/certificates/courses/${courseId}`).then((r) => r.data),
};
