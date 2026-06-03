export interface CourseEarningsDto {
    courseId: string;
    courseTitle: string;
    paymentsCount: number;
    totalAmount: number;
    lastPaymentAt: string;
}

export interface InstructorEarningsResponse {
    totalEarnings: number;
    totalPayments: number;
    courses: CourseEarningsDto[];
}
