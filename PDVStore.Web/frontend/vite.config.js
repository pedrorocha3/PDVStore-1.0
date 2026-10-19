var _a, _b;
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
// O proxy evita CORS no desenvolvimento e mantém o front apontando para a API real.
export default defineConfig({
    plugins: [react()],
    server: {
        port: 5173,
        proxy: {
            '/api': {
                target: (_a = process.env.VITE_API_PROXY) !== null && _a !== void 0 ? _a : 'http://localhost:5080',
                changeOrigin: true,
            },
            '/health': {
                target: (_b = process.env.VITE_API_PROXY) !== null && _b !== void 0 ? _b : 'http://localhost:5080',
                changeOrigin: true,
            },
        },
    },
});
