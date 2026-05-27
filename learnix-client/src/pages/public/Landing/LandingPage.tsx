import { useCategories } from '@/hooks/useCategories';
import { useFeaturedCourses } from '@/hooks/useFeaturedCourses';
import { useCourseCount } from '@/hooks/useCourseCount';
import { AIAssistantSection } from './components/AIAssistantSection';
import { AnnouncementBar } from './components/AnnouncementBar';
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
    const { data: categories = [], isLoading: categoriesLoading } = useCategories();
    const { data: featuredCourses = [], isLoading: coursesLoading } = useFeaturedCourses();
    const { data: courseCount } = useCourseCount();

    return (
        <>
            <AnnouncementBar />
            <HeroSection />
            <StatsSection />
            <CategoriesSection categories={categories} isLoading={categoriesLoading} />
            <FeaturedCoursesSection courses={featuredCourses} isLoading={coursesLoading} totalCount={courseCount} />
            <HowItWorksSection />
            <AIAssistantSection />
            <TestimonialsSection />
            <InstructorsCTASection />
            <FaqSection />
            <FinalCTASection />
        </>
    );
}
