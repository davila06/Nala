import axios from "axios";
import { useAuthStore } from "@/features/auth/store/authStore";
import { decodeRoleFromJwt } from "@/features/auth/api/authApi";

const API_BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

export const apiClient = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: { "Content-Type": "application/json" },
  withCredentials: true,
});

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Single in-flight refresh promise shared across concurrent 401 responses
let refreshPromise: Promise<string> | null = null;

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      try {
        if (!refreshPromise) {
          refreshPromise = axios
            .post<{
              accessToken: string;
              user: {
                id: string;
                name: string;
                email: string;
                isAdmin: boolean;
              };
            }>(
              `${API_BASE_URL}/api/auth/refresh`,
              {},
              { withCredentials: true },
            )
            .then(({ data }) => {
              const role = decodeRoleFromJwt(data.accessToken);
              useAuthStore
                .getState()
                .setAuth(
                  {
                    id: data.user.id,
                    name: data.user.name,
                    email: data.user.email,
                    role,
                    isAdmin: data.user.isAdmin,
                  },
                  data.accessToken,
                );
              return data.accessToken;
            })
            .finally(() => {
              refreshPromise = null;
            });
        }
        const newToken = await refreshPromise;
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return apiClient(originalRequest);
      } catch {
        refreshPromise = null;
        useAuthStore.getState().clearAuth();
        window.location.href = "/login";
      }
    }
    return Promise.reject(error);
  },
);
