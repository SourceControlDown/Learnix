import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'node:path';

export default defineConfig(({ mode, command }) => {
  const env = loadEnv(mode, process.cwd(), '');

  // A production bundle built without this silently canonicalises the whole site to localhost: the
  // canonical link, og:url and og:image in index.html all point at a machine nobody can reach, so
  // Google indexes an address that does not exist and link previews come back empty. Nothing crashes
  // and the site looks fine — which is why this has to fail here rather than be noticed months later.
  if (command === 'build' && mode === 'production' && !env.VITE_SITE_URL) {
    throw new Error(
      'VITE_SITE_URL is not set. A production build needs the public origin of the site ' +
        '(e.g. https://learnix.azurestaticapps.net) for canonical URLs, og:image and the sitemap. ' +
        'Set it as a GitHub Actions variable, or in learnix-client/.env for a local production build.',
    );
  }

  const siteUrl = (env.VITE_SITE_URL || 'http://localhost:5173').replace(/\/+$/, '');

  return {
    plugins: [
      react(),
      {
        // index.html carries the fallback OG tags for scrapers that don't run JS, and those need
        // absolute URLs. Vite's own %VAR% substitution silently leaves the placeholder in place
        // when the var is unset, which would ship a broken og:image — so resolve it explicitly.
        name: 'learnix:inject-site-url',
        transformIndexHtml: (html: string) => html.replaceAll('%VITE_SITE_URL%', siteUrl),
      },
    ],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
    },
    build: {
      sourcemap: false,
    },
  };
});
