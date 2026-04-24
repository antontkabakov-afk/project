import path from "path";
import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, __dirname, "");
  const devProxyTarget = env.VITE_DEV_PROXY_TARGET || "http://localhost:5247";

  return {
    plugins: [react()],
    preview: {
      host: "0.0.0.0",
      port: 4173,
    },
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
    server: {
      host: "0.0.0.0",
      port: 5173,
      proxy: {
        "/api": {
          target: devProxyTarget,
          changeOrigin: true,
        },
        "/health": {
          target: devProxyTarget,
          changeOrigin: true,
        },
      },
    },
  };
});
