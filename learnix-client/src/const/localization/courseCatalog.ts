export const COURSE_CATALOG = {
    PAGE_TITLE: 'All courses',
    SEARCH_PLACEHOLDER: 'Search courses...',
    RESULTS_COUNT: (n: number, q?: string) =>
        q
            ? `Showing ${n} result${n !== 1 ? 's' : ''} for "${q}"`
            : `${n} course${n !== 1 ? 's' : ''} available`,
    NO_RESULTS_TITLE: 'No courses found',
    NO_RESULTS_DESC: 'Try adjusting your search or filters to find what you are looking for.',
    CLEAR_FILTERS: 'Clear filters',

    FILTERS: {
        TITLE: 'Filters',
        CATEGORY: 'Category',
        PRICE: 'Price',
        PRICE_ALL: 'All prices',
        PRICE_FREE: 'Free',
        PRICE_PAID: 'Paid',
        RATING: 'Rating',
        RATING_45: '4.5 & up',
        RATING_40: '4.0 & up',
        RATING_35: '3.5 & up',
    },

    SORT: {
        LABEL: 'Sort by',
        POPULAR: 'Most popular',
        NEWEST: 'Newest',
        RATING: 'Highest rated',
    },

    ACTIVE_CHIPS: {
        FREE: 'Free',
        PAID: 'Paid',
        RATING: (n: number) => `${n}★ & up`,
    },

    PAGINATION: {
        PREV: '‹',
        NEXT: '›',
    },

    STUDENTS: (n: number) => `${n.toLocaleString()} students`,
} as const;
