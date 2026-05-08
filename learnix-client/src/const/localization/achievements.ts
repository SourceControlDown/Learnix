import type { LucideIcon } from 'lucide-react';
import {
    BookOpen,
    GraduationCap,
    Layers,
    Trophy,
    Zap,
    Globe,
    CheckCircle2,
    Star,
    Award,
    Flame,
} from 'lucide-react';

export interface AchievementMeta {
    name: string;
    description: string;
    icon: LucideIcon;
}

export const ACHIEVEMENT_META: Record<string, AchievementMeta> = {
    FIRST_LESSON: {
        name: 'First Step',
        description: 'Complete your very first lesson',
        icon: BookOpen,
    },
    LESSONS_50: {
        name: 'Eager Learner',
        description: 'Complete 50 lessons',
        icon: Flame,
    },
    LESSONS_200: {
        name: 'Knowledge Seeker',
        description: 'Complete 200 lessons',
        icon: Star,
    },
    LESSONS_500: {
        name: 'Scholar',
        description: 'Complete 500 lessons',
        icon: Layers,
    },
    FIRST_COURSE: {
        name: 'Course Graduate',
        description: 'Complete your first course',
        icon: GraduationCap,
    },
    COURSES_3: {
        name: 'Triple Crown',
        description: 'Complete 3 courses',
        icon: Trophy,
    },
    COURSES_5: {
        name: 'Dedicated Student',
        description: 'Complete 5 courses',
        icon: Award,
    },
    SPEED_DEMON: {
        name: 'Speed Demon',
        description: 'Complete a 20+ question test in under 5 minutes',
        icon: Zap,
    },
    POLYMATH: {
        name: 'Polymath',
        description: 'Complete courses in 3 or more different categories',
        icon: Globe,
    },
    PROFILE_COMPLETE: {
        name: 'All Set',
        description: 'Fill in your bio and set a profile photo',
        icon: CheckCircle2,
    },
} as const;

export const ACHIEVEMENTS_PAGE = {
    TITLE: 'Achievements',
    SUBTITLE: 'Track your learning milestones',
    PROGRESS_SECTION: 'Your Progress',
    STATS: {
        LESSONS: 'Lessons Completed',
        COURSES: 'Courses Completed',
        CATEGORIES: 'Categories Explored',
    },
    EARNED_COUNT: (n: number) => `${n} achievement${n !== 1 ? 's' : ''} earned`,
    LOCKED_LABEL: 'Locked',
    EARNED_LABEL: 'Earned',
} as const;
