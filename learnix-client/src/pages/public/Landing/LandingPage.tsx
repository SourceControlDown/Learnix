import { useTranslation } from 'react-i18next';
import { Seo } from '@/components/common/seo/Seo';
import { ProjectNoticeBanner } from '@/components/common/ui/ProjectNoticeBanner';
import { useCategories } from '@/hooks/course/useCategories';
import { useCourseCount } from '@/hooks/course/useCourseCount';
import { useFeaturedCourses } from '@/hooks/course/useFeaturedCourses';
import { organizationJsonLd, webSiteJsonLd } from '@/utils/seo';
import { AIAssistantSection } from './components/AIAssistantSection';
import { CategoriesSection } from './components/CategoriesSection';
import { FaqSection } from './components/FaqSection';
import { FeaturedCoursesSection } from './components/FeaturedCoursesSection';
import { FinalCTASection } from './components/FinalCTASection';
import { HeroSection } from './components/HeroSection';
import { HowItWorksSection } from './components/HowItWorksSection';
import { InstructorsCTASection } from './components/InstructorsCTASection';
import { StatsSection } from './components/StatsSection';
import { TestimonialsSection } from './components/TestimonialsSection';

export default function LandingPage() {
    const { t } = useTranslation('landing');
    const {
        data: categories = [],
        isLoading: categoriesLoading,
        isError: categoriesError,
        refetch: refetchCategories,
    } = useCategories();
    const {
        data: featuredCourses = [],
        isLoading: coursesLoading,
        isError: coursesError,
        refetch: refetchCourses,
    } = useFeaturedCourses();
    const { data: courseCount } = useCourseCount();

    return (
        <>
            <Seo
                title={t('seo.title')}
                description={t('seo.description')}
                jsonLd={[organizationJsonLd(), webSiteJsonLd()]}
            />
            {/* <AnnouncementBar /> */}
            <ProjectNoticeBanner />
            <HeroSection />
            <StatsSection />
            <CategoriesSection
                categories={categories}
                isLoading={categoriesLoading}
                isError={categoriesError}
                onRetry={refetchCategories}
            />
            <FeaturedCoursesSection
                courses={featuredCourses}
                isLoading={coursesLoading}
                isError={coursesError}
                onRetry={refetchCourses}
                totalCount={courseCount}
            />
            <HowItWorksSection />
            <AIAssistantSection />
            <TestimonialsSection />
            <InstructorsCTASection />
            <FaqSection />
            <FinalCTASection />
        </>
    );
}
