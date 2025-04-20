import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'fs';
import { env } from 'process';
import { readFile } from 'fs/promises';
import mkcert from 'vite-plugin-mkcert'

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://localhost:8080';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin(), mkcert()],
    optimizeDeps: {
        exclude: ['imagescript'],
        esbuildOptions: {
            plugins: [
                {
                    name: 'esbuild-plugin-react-virtualized',
                    setup({ onLoad }) {
                        onLoad(
                            {
                                filter: /react-virtualized[/\\]dist[/\\]es[/\\]WindowScroller[/\\]utils[/\\]onScroll\.js$/
                            },
                            async ({ path }) => {
                                const code = await readFile(path, 'utf8')
                                const broken = `import { bpfrpt_proptype_WindowScroller } from "../WindowScroller.js";`
                                return { contents: code.replace(broken, '') }
                            })
                    },
                }
            ],
        },
    },
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '^/api/.*': {
                target,
                secure: false
            }
        },
        port: 5173,
        host: "0.0.0.0",
    }
})
