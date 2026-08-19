import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { VitePWA } from "vite-plugin-pwa";
import { resolve } from "path";

export default defineConfig({
  plugins: [
    tailwindcss(),
    react(),
    VitePWA({
      registerType: "prompt",
      strategies: "injectManifest",
      srcDir: "src",
      filename: "sw.ts",
      injectManifest: {
        globPatterns: ["**/*.{js,css,ico,png,svg}"],
        globIgnores: ["widget.js", "**/widget.js"],
      },
      includeAssets: ["favicon.ico", "apple-touch-icon.png", "mask-icon.svg"],
      devOptions: {
        enabled: true,
        type: "module",
      },
      manifest: {
        name: "PawTrack CR",
        short_name: "PawTrack",
        description:
          "Identidad digital de mascotas y recuperación en caso de pérdida",
        theme_color: "#f97316",
        background_color: "#ffffff",
        display: "standalone",
        start_url: "/",
        icons: [
          {
            src: "pwa-192x192.png",
            sizes: "192x192",
            type: "image/png",
            purpose: "any",
          },
          {
            src: "pwa-512x512.png",
            sizes: "512x512",
            type: "image/png",
            purpose: "any maskable",
          },
        ],
        screenshots: [
          {
            src: "screenshot-mobile.png",
            sizes: "390x844",
            type: "image/png",
            form_factor: "narrow",
            label: "PawTrack CR - Login",
          },
        ],
      },
    }),
  ],
  resolve: {
    alias: {
      "@": resolve(__dirname, "src"),
    },
  },
  build: {
    rollupOptions: {
      input: {
        main: resolve(__dirname, "index.html"),
        widget: resolve(__dirname, "src/widget/widget.ts"),
      },
      output: {
        entryFileNames: (chunk) =>
          chunk.name === "widget" ? "widget.js" : "assets/[name]-[hash].js",
      },
    },
  },
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: ["./tests/setup.ts"],
  },
});
