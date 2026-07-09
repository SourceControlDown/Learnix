export interface WishlistCourseDto {
    courseId: string;
    title: string;
    coverImageUrl: string | null;
    price: number;
    isFree: boolean;
    addedAt: string;
}
