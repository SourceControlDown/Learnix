/* eslint-disable react-refresh/only-export-components */
import { lazy, Suspense, type ReactElement } from 'react';
import type { RouteObject } from 'react-router-dom';
import { PageFallback } from '@/components/common/PageFallback';

const LandingPage = lazy(() => import('@/pages/public/Landing/LandingPage'));
const LoginPage = lazy(() => import('@/pages/public/Login/LoginPage'));
const RegisterPage = lazy(() => import('@/pages/public/Register/RegisterPage'));
const CourseCatalogPage = lazy(() => import('@/pages/public/CourseCatalog/CourseCatalogPage'));
const CourseDetailPage = lazy(() => import('@/pages/public/CourseDetail/CourseDetailPage'));
const NotFoundPage = lazy(() => import('@/pages/public/NotFound/NotFoundPage'));

const wrap = (el: ReactElement) => <Suspense fallback={<PageFallback />}>{el}</Suspense>;

export const publicRoutes: RouteObject[] = [
    { index: true, element: wrap(<LandingPage />) },
    { path: '/login', element: wrap(<LoginPage />) },
    { path: '/register', element: wrap(<RegisterPage />) },
    { path: '/courses', element: wrap(<CourseCatalogPage />) },
    { path: '/courses/:courseId', element: wrap(<CourseDetailPage />) },
    { path: '*', element: wrap(<NotFoundPage />) },
];
