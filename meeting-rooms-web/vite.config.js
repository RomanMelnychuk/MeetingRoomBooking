import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
    plugins: [react()],
    build: {
        // The production build goes straight into the API's wwwroot,
        // so one Azure Web App serves both the API and the frontend.
        outDir: '../MeetingRooms.Api/wwwroot',
        emptyOutDir: true
    },
    server: {
        // In development, API and SignalR calls are proxied to the backend:
        // same origin for the browser, so the auth cookie just works.
        proxy: {
            '/api': { target: 'https://localhost:7185', secure: false },
            '/hubs': { target: 'https://localhost:7185', secure: false, ws: true }
        }
    }
})