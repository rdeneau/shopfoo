import {defineConfig} from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from "@tailwindcss/vite";

/** @type {import('vite').UserConfig} */
export default defineConfig({
    plugins: [
        react({ jsxRuntime: 'classic' }), // 'classic' is required for fast-refresh for .js files
        tailwindcss()
    ],
    root: "./src/Shopfoo.Client",
    server: {
        port: 8080,
        proxy: {
            '/api': 'http://localhost:5000',
        },
        watch: {
            ignored: [
                "**/*.fs" // Don't watch F# files
            ]
        }
    },
    css: {devSourcemap: true},
    build: {
        outDir: "../../publish/app/public",
        sourcemap: true
    },
    debug: true
})
