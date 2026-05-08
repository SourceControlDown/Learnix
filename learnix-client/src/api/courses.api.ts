import { api } from './axios.instance';
import type { PaginatedResult } from '@/types/api.types';
import type { CourseDetailDto } from '@/types/course.types';

export interface CourseFilters {
    search?: string;
    skip?: number;
    take?: number;
    categoryId?: string;
    instructorId?: string;
}

/** Matches backend PublicCourseCardDto */
export interface PublicCourseCardDto {
    id: string;
    instructorId: string;
    categoryId: string;
    title: string;
    description: string;
    coverImageUrl: string | null;
    price: number;
    isFree: boolean;
    enrollmentsCount: number;
    tags: string[];
}

/** Matches backend FeaturedCourseDto */
export interface FeaturedCourseCardDto {
    id: string;
    title: string;
    description: string;
    coverImageUrl: string | null;
    price: number;
    isFree: boolean;
    rating: number;
    reviewsCount: number;
    durationHours: number;
    categoryName: string;
    instructor: { id: string; fullName: string };
    badge: 'bestseller' | 'new' | null;
}

export const coursesApi = {
    getPublic: (filters: CourseFilters = {}) =>
        api
            .get<PaginatedResult<PublicCourseCardDto>>('/courses', { params: filters })
            .then((r) => r.data),

    getFeatured: () => api.get<FeaturedCourseCardDto[]>('/courses/featured').then((r) => r.data),

    getById: (id: string) => api.get<CourseDetailDto>(`/courses/${id}`).then((r) => r.data),
};
