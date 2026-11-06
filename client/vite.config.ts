import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
// The API only accepts calls from port 4000 (CORS in api/Program.cs), so the dev server always uses it
export default defineConfig({
    plugins: [react()],
    server: {
        port: 4000,
        strictPort: true,
    },
})
