import { lazy, Suspense } from 'react';
import { createBrowserRouter } from 'react-router-dom';
import { PublicLayout } from '@/components/layout/PublicLayout';
import { PageFallback } from '@/components/common/PageFallback';
import { publicRoutes } from './publicRoutes';

const CoursePlayerPage = lazy(() => import('@/pages/student/CoursePlayer/CoursePlayerPage'));
const TestLessonPage = lazy(() => import('@/pages/student/TestLesson/TestLessonPage'));

const wrap = (el: React.ReactElement) => <Suspense fallback={<PageFallback />}>{el}</Suspense>;

export const router = createBrowserRouter([
    {
        element: <PublicLayout />,
        children: publicRoutes,
    },
    {
        path: '/courses/:courseId/learn/:lessonId',
        element: wrap(<CoursePlayerPage />),
    },
    {
        path: '/courses/:courseId/learn/:lessonId/test',
        element: wrap(<TestLessonPage />),
    },
]);
