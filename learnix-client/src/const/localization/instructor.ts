export const INSTRUCTOR = {
    // Layout / navigation
    NAV_DASHBOARD: 'Dashboard',
    NAV_MY_COURSES: 'My courses',
    NAV_NEW_COURSE: 'New course',
    NAV_MESSAGES: 'Messages',
    NAV_EARNINGS: 'Earnings',
    NAV_BACK_TO_CATALOG: 'Back to catalog',
    NAV_SIGN_OUT: 'Sign out',

    // Dashboard
    DASHBOARD_TITLE: 'Instructor dashboard',
    DASHBOARD_SUBTITLE: 'Overview of your courses and students',
    STAT_TOTAL_COURSES: 'Total courses',
    STAT_TOTAL_STUDENTS: 'Students enrolled (recent)',
    RECENT_COURSES_TITLE: 'Recently updated',
    RECENT_COURSES_VIEW_ALL: 'View all →',
    COL_COURSE: 'Course',
    COL_STATUS: 'Status',
    COL_STUDENTS: 'Students',
    COL_ACTIONS: '',
    BTN_NEW_COURSE: '+ New course',
    BTN_EDIT: 'Edit',
    DASHBOARD_EMPTY: "You haven't created any courses yet.",
    DASHBOARD_EMPTY_CTA: 'Create your first course →',

    // My Courses page
    MY_COURSES_TITLE: 'My courses',
    MY_COURSES_SUBTITLE: 'Manage, publish and track your courses',
    MY_COURSES_SEARCH: 'Search courses...',
    MY_COURSES_EMPTY: 'No courses match your search.',
    MY_COURSES_EMPTY_DEFAULT: "You haven't created any courses yet.",
    BTN_PUBLISH: 'Publish',
    BTN_UNPUBLISH: 'Unpublish',
    BTN_ARCHIVE: 'Archive',
    BTN_UNARCHIVE: 'Unarchive',
    BTN_DELETE: 'Delete',
    CONFIRM_ARCHIVE: (title: string) =>
        `Archive "${title}"? It will be hidden from the catalog. You can unarchive it later.`,
    CONFIRM_DELETE: (title: string) => `Delete "${title}"? This action cannot be undone.`,
    PAGINATION_PREV: 'Previous',
    PAGINATION_NEXT: 'Next',
    PAGINATION_PAGE: (current: number, total: number) => `Page ${current} of ${total}`,

    // Status badges
    STATUS_DRAFT: 'Draft',
    STATUS_PUBLISHED: 'Published',
    STATUS_ARCHIVED: 'Archived',

    // Course editor
    EDITOR_TITLE_NEW: 'New course',
    EDITOR_BACK: '← My courses',
    EDITOR_UNSAVED: 'Unsaved changes',
    EDITOR_ARCHIVED_BANNER:
        'This course is archived and cannot be edited. Unarchive it first to make changes.',
    BTN_SAVE: 'Save',
    BTN_PUBLISH_COURSE: 'Publish',
    BTN_UNPUBLISH_COURSE: 'Unpublish',
    BTN_UNARCHIVE_COURSE: 'Unarchive',
    TAB_INFO: 'Course info',
    TAB_CURRICULUM: 'Curriculum',

    // Course info form
    FIELD_TITLE: 'Title',
    FIELD_TITLE_PLACEHOLDER: 'e.g. Advanced React Patterns',
    FIELD_DESCRIPTION: 'Description',
    FIELD_DESCRIPTION_PLACEHOLDER: 'Describe what students will learn...',
    FIELD_CATEGORY: 'Category',
    FIELD_CATEGORY_PLACEHOLDER: 'Select category',
    FIELD_PRICE: 'Price (USD)',
    FIELD_PRICE_PLACEHOLDER: '0 = free',
    FIELD_TAGS: 'Tags',
    FIELD_TAGS_PLACEHOLDER: 'Add tag and press Enter...',
    COVER_IMAGE_LABEL: 'Cover image',
    COVER_IMAGE_HINT: 'Click or drop to upload (JPEG, PNG, WebP)',
    COVER_IMAGE_UPLOADING: 'Uploading...',
    COVER_IMAGE_REPLACE: 'Click or drop to replace',

    // Curriculum
    BTN_ADD_SECTION: '+ Add section',
    SECTION_PLACEHOLDER: 'Section title',
    LESSON_COUNT: (n: number) => `${n} lesson${n !== 1 ? 's' : ''}`,
    BTN_ADD_VIDEO: '+ Video',
    BTN_ADD_POST: '+ Post',
    BTN_ADD_TEST: '+ Test',
    BADGE_VIDEO: 'VIDEO',
    BADGE_POST: 'POST',
    BADGE_TEST: 'TEST',

    // Lesson editor modal
    MODAL_NEW_VIDEO: 'New video lesson',
    MODAL_NEW_POST: 'New post lesson',
    MODAL_NEW_TEST: 'New test',
    MODAL_EDIT_VIDEO: 'Edit video lesson',
    MODAL_EDIT_POST: 'Edit post lesson',
    MODAL_EDIT_TEST: 'Edit test',
    BTN_SAVE_LESSON: 'Save lesson',
    BTN_CANCEL: 'Cancel',

    // Video lesson form
    FIELD_VIDEO: 'Video file',
    VIDEO_HINT: 'Upload MP4 or WebM (max 2 GB)',
    VIDEO_UPLOADING: 'Uploading video...',
    VIDEO_UPLOADED: 'Video uploaded',
    FIELD_DURATION: 'Duration (seconds)',

    // Post lesson form
    FIELD_CONTENT: 'Content',

    // Test lesson form
    FIELD_PASSING_THRESHOLD: 'Passing score (%)',
    FIELD_ATTEMPT_LIMIT: 'Attempt limit (leave blank for unlimited)',
    FIELD_COOLDOWN: 'Cooldown between attempts (minutes)',
    BTN_ADD_QUESTION: '+ Add question',
    BTN_REMOVE_QUESTION: 'Remove',
    FIELD_QUESTION_TEXT: 'Question',
    FIELD_QUESTION_TYPE: 'Type',
    QUESTION_TYPE_SINGLE: 'Single choice',
    QUESTION_TYPE_MULTIPLE: 'Multiple choice',
    BTN_ADD_OPTION: '+ Add option',
    BTN_REMOVE_OPTION: 'Remove',
    FIELD_OPTION_TEXT: 'Option text',
    OPTION_CORRECT: 'Correct',

    // Become instructor page
    BECOME_TITLE: 'Become an instructor',
    BECOME_SUBTITLE: 'Share your knowledge with thousands of students on Learnix.',
    FIELD_MOTIVATION: 'Why do you want to teach?',
    FIELD_MOTIVATION_PLACEHOLDER:
        'Describe your teaching experience, expertise, and what courses you plan to create...',
    FIELD_PORTFOLIO: 'Portfolio URL (optional)',
    FIELD_PORTFOLIO_PLACEHOLDER: 'https://your-portfolio.com',
    BTN_SUBMIT_APPLICATION: 'Submit application',
    APPLICATION_PENDING_TITLE: 'Application under review',
    APPLICATION_PENDING_BODY:
        'Your application was submitted and is being reviewed by our team. We usually respond within 3 business days.',
    APPLICATION_APPROVED_TITLE: "You're an instructor!",
    APPLICATION_APPROVED_BODY:
        'Your application was approved. Head to your dashboard to start creating courses.',
    BTN_GO_TO_DASHBOARD: 'Go to dashboard',
    APPLICATION_REJECTED_TITLE: 'Application not approved',
    APPLICATION_REJECTED_REASON_LABEL: 'Reason:',
    BTN_RESUBMIT: 'Resubmit application',
    ALREADY_INSTRUCTOR_TITLE: "You're already an instructor",
    ALREADY_INSTRUCTOR_BODY: 'You can start creating courses from your instructor dashboard.',
    NOT_LOGGED_IN_TITLE: 'Sign in to apply',
    NOT_LOGGED_IN_BODY: 'You need to be logged in to submit an instructor application.',
    BTN_SIGN_IN: 'Sign in',

    // Earnings page
    EARNINGS_TITLE: 'Earnings',
    EARNINGS_SUBTITLE: 'Revenue overview across all your courses',
    EARNINGS_TOTAL: 'Total earnings',
    EARNINGS_PAYMENTS: 'Total payments',
    EARNINGS_COURSES_TABLE_TITLE: 'Revenue by course',
    EARNINGS_COL_COURSE: 'Course',
    EARNINGS_COL_PAYMENTS: 'Payments',
    EARNINGS_COL_REVENUE: 'Revenue',
    EARNINGS_COL_LAST: 'Last payment',
    EARNINGS_EMPTY: 'No earnings yet. Students must enroll in your paid courses first.',
    EARNINGS_FREE_BADGE: 'Free courses not included',

    // Toasts
    TOAST_COURSE_SAVED: 'Course saved',
    TOAST_COURSE_PUBLISHED: 'Course published',
    TOAST_COURSE_UNPUBLISHED: 'Course moved to draft',
    TOAST_COURSE_ARCHIVED: 'Course archived',
    TOAST_COURSE_UNARCHIVED: 'Course moved back to draft',
    TOAST_COURSE_DELETED: 'Course deleted',
    TOAST_SECTION_ADDED: 'Section added',
    TOAST_SECTION_DELETED: 'Section deleted',
    CONFIRM_DELETE_LESSON: (title: string) => `Delete "${title}"? This action cannot be undone.`,
    TOAST_LESSON_SAVED: 'Lesson saved',
    TOAST_LESSON_DELETED: 'Lesson deleted',
    TOAST_APPLICATION_SUBMITTED: 'Application submitted',
    TOAST_ERROR: 'Something went wrong. Please try again.',

    // Publish checklist
    PUBLISH_CHECKLIST_TITLE: 'Before publishing',
    PUBLISH_CHECKLIST_COVER: 'Cover image uploaded',
    PUBLISH_CHECKLIST_SECTIONS: 'At least one section',
    PUBLISH_CHECKLIST_LESSONS: 'At least one lesson per section',
} as const;
