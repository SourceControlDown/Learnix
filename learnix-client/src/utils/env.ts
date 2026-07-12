/**
 * Related ADRs:
 * - ADR-FRONT-API-006: Environment Variables Management
 */
const apiUrl = import.meta.env.VITE_API_URL;
if (!apiUrl) throw new Error('Missing env variable: VITE_API_URL');

// Optional: the public origin the app is served from. Only needed when the deployed origin differs
// from the one crawlers hit (e.g. behind a custom domain). Falls back to the runtime origin.
const siteUrl = (import.meta.env.VITE_SITE_URL || window.location.origin).replace(/\/+$/, '');

export const env = {
    API_URL: apiUrl,
    HUB_URL: apiUrl.replace(/\/api\/?$/, ''),
    SITE_URL: siteUrl,
} as const;
