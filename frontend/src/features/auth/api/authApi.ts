import { apiClient } from "@/shared/lib/apiClient";

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  isAdultConfirmed: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface WebAuthnRegisterRequest {
  response: PublicKeyCredentialJSON;
  deviceName?: string;
}

export interface WebAuthnAuthenticateRequest {
  email: string;
  response: PublicKeyCredentialJSON;
}

export interface PublicKeyCredentialJSON {
  id: string;
  rawId: string;
  type: string;
  response: Record<string, string>;
  clientExtensionResults?: Record<string, unknown>;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

/**
 * Shape returned by the backend login / refresh endpoints.
 * `isAdmin` replaces the old `role` string (SEC-05: avoid exposing internal role
 * taxonomy in API responses). Full role is decoded from the signed JWT instead.
 */
export interface AuthTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: {
    id: string;
    name: string;
    email: string;
    isEmailVerified: boolean;
    isAdmin: boolean;
  };
}

/** Profile returned by /auth/me — exposes only operational fields (no raw role). */
export interface UserProfile {
  id: string;
  name: string;
  email: string;
  isAdmin: boolean;
  createdAt: string;
  isAdultConfirmed: boolean;
  hasHealthDataConsent: boolean;
}

// ── JWT decode helper (no signature validation — server validates on every request) ──
const ROLE_CLAIM =
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

export type UserRole =
  | "Owner"
  | "Ally"
  | "Admin"
  | "Clinic"
  | "Municipality"
  | "Store"
  | "ServiceProvider";

export function decodeRoleFromJwt(accessToken: string): UserRole {
  try {
    const payload: unknown = JSON.parse(
      atob(accessToken.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")),
    );
    const raw =
      typeof payload === "object" && payload !== null && ROLE_CLAIM in payload
        ? (payload as Record<string, unknown>)[ROLE_CLAIM]
        : undefined;
    if (
      raw === "Ally" ||
      raw === "Admin" ||
      raw === "Clinic" ||
      raw === "Municipality" ||
      raw === "Store" ||
      raw === "ServiceProvider"
    )
      return raw;
    return "Owner";
  } catch {
    return "Owner";
  }
}

export const authApi = {
  register: (data: RegisterRequest) =>
    apiClient.post<void>("/auth/register", data),

  login: (data: LoginRequest) =>
    apiClient.post<AuthTokenResponse>("/auth/login", data),

  webauthnRegisterOptions: () =>
    apiClient.post<Record<string, unknown>>("/auth/webauthn/register/options"),

  webauthnRegister: (data: WebAuthnRegisterRequest) =>
    apiClient.post<{ credentialId: string }>("/auth/webauthn/register", data),

  webauthnAuthenticateOptions: (email: string) =>
    apiClient.post<Record<string, unknown>>(
      "/auth/webauthn/authenticate/options",
      { email },
    ),

  webauthnAuthenticate: (data: WebAuthnAuthenticateRequest) =>
    apiClient.post<{ accessToken: string; user: AuthTokenResponse["user"] }>(
      "/auth/webauthn/authenticate",
      data,
    ),

  verifyEmail: (token: string) =>
    apiClient.get<void>(
      `/auth/verify-email?token=${encodeURIComponent(token)}`,
    ),

  forgotPassword: (data: ForgotPasswordRequest) =>
    apiClient.post<void>("/auth/forgot-password", data),

  resetPassword: (data: ResetPasswordRequest) =>
    apiClient.post<void>("/auth/reset-password", data),

  logout: () => apiClient.post<void>("/auth/logout"),

  refresh: () => apiClient.post<AuthTokenResponse>("/auth/refresh"),

  getMyProfile: () =>
    apiClient.get<UserProfile>("/auth/me").then((r) => r.data),

  updateProfile: (data: { name: string }) =>
    apiClient.patch<void>("/auth/me", data),

  changePassword: (data: { currentPassword: string; newPassword: string }) =>
    apiClient.patch<void>("/auth/me/password", data),

  deleteAccount: (data: { confirmPassword: string }) =>
    apiClient.delete<void>("/auth/me", { data }),

  grantHealthDataConsent: () =>
    apiClient
      .post<{ consentedAt: string }>("/auth/me/health-data-consent")
      .then((r) => r.data),

  exportMyData: () =>
    apiClient.get<unknown>("/auth/me/export").then((r) => r.data),
};
