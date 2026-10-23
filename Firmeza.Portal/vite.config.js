import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  optimizeDeps: { noDiscovery: true },
  server: { host: '0.0.0.0', port: 5173, strictPort: true },
})
