import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// O proxy evita CORS no desenvolvimento e mantém o front apontando para a API real.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.VITE_API_PROXY ?? 'http://localhost:5080',
        changeOrigin: true,
      },
      '/health': {
        target: process.env.VITE_API_PROXY ?? 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
});
