export const TEST_LESSON = {
    HEADER: {
        backToLesson: 'Back to lesson',
        testLabel: 'Test',
    },
    STATUS: {
        attemptsUsed: (used: number, limit: number | null) =>
            limit ? `Attempts: ${used} / ${limit}` : `Attempts: ${used} (unlimited)`,
        passingScore: (pct: number) => `Passing score: ${pct}%`,
        cooldown: (minutes: number) => `Next attempt available in ${minutes} min`,
        noAttemptsLeft: 'No attempts remaining',
        passed: 'Passed',
        failed: 'Failed',
        score: (score: number, max: number) => `${score} / ${max} points`,
        percentage: (score: number, max: number) =>
            `${max > 0 ? Math.round((score / max) * 100) : 0}%`,
    },
    FORM: {
        questionOf: (current: number, total: number) => `Question ${current} of ${total}`,
        textPlaceholder: 'Enter your answer...',
        submitButton: 'Submit test',
        submitting: 'Submitting...',
    },
    RESULTS: {
        heading: 'Test results',
        passed: 'Congratulations! You passed the test.',
        failed: 'You did not pass. Review the material and try again.',
        correct: 'Correct',
        incorrect: 'Incorrect',
        yourAnswer: 'Your answer',
        returnToLesson: 'Return to lesson',
        retakeTest: 'Retake test',
    },
    HISTORY: {
        heading: 'Attempt history',
        attempt: (n: number) => `Attempt #${n}`,
        noAttempts: 'No attempts yet.',
        date: 'Date',
        score: 'Score',
        result: 'Result',
    },
    LOADING: 'Loading test...',
    ERROR: 'Failed to load test. Please try again.',
} as const;
