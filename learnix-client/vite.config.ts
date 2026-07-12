import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'node:path';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
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
