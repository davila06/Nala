import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import ProfilePage from "@/features/auth/pages/ProfilePage";
import { server } from "../../mocks/server";
import { renderWithProviders } from "../../utils/renderWithProviders";
import { useAuthStore } from "@/features/auth/store/authStore";
import { act } from "react";
import { vi } from "vitest";

const API = "http://localhost:5000/api";

const { successToast } = vi.hoisted(() => ({ successToast: vi.fn() }));
vi.mock("@/shared/lib/toast", () => ({
  toast: {
    success: successToast,
    error: vi.fn(),
  },
}));

function seedAuthStore() {
  act(() => {
    useAuthStore.getState().setAuth(
      {
        id: "user-1",
        name: "Denis Ávila",
        email: "denis@test.cr",
        role: "Owner",
        isAdmin: false,
      },
      "mock-token",
    );
  });
}

describe("ProfilePage — identity section", () => {
  it("shows user name and email from /auth/me", async () => {
    server.use(
      http.get(`${API}/auth/me`, () =>
        HttpResponse.json({
          id: "user-1",
          name: "Denis Ávila",
          email: "denis@test.cr",
          isAdmin: false,
        }),
      ),
      // Foster profile returns 404 (user is not a volunteer)
      http.get(`${API}/fosters/me`, () => new HttpResponse(null, { status: 404 })),
    );

    seedAuthStore();
    renderWithProviders(<ProfilePage />);

    await waitFor(() => expect(screen.getByText("Denis Ávila")).toBeInTheDocument());
    expect(screen.getByText("denis@test.cr")).toBeInTheDocument();
  });

  it('shows edit input when clicking "Editar"', async () => {
    server.use(
      http.get(`${API}/auth/me`, () =>
        HttpResponse.json({
          id: "user-1",
          name: "Denis Ávila",
          email: "denis@test.cr",
          isAdmin: false,
        }),
      ),
      http.get(`${API}/fosters/me`, () => new HttpResponse(null, { status: 404 })),
    );

    seedAuthStore();
    const user = userEvent.setup();
    renderWithProviders(<ProfilePage />);

    await waitFor(() => expect(screen.getByText(/editar/i)).toBeInTheDocument());
    await user.click(screen.getByText(/editar/i));

    expect(screen.getByRole("textbox")).toHaveValue("Denis Ávila");
    // Use exact text to distinguish from 'Guardar perfil de custodio'
    expect(screen.getByRole("button", { name: "Guardar" })).toBeInTheDocument();
  });

  it("saves name via PATCH /auth/me and shows confirmation", async () => {
    successToast.mockClear();
    server.use(
      http.get(`${API}/auth/me`, () =>
        HttpResponse.json({
          id: "user-1",
          name: "Denis Ávila",
          email: "denis@test.cr",
          isAdmin: false,
        }),
      ),
      http.get(`${API}/fosters/me`, () => new HttpResponse(null, { status: 404 })),
      http.patch(`${API}/auth/me`, () => new HttpResponse(null, { status: 204 })),
    );

    seedAuthStore();
    const user = userEvent.setup();
    renderWithProviders(<ProfilePage />);

    await waitFor(() => expect(screen.getByText(/editar/i)).toBeInTheDocument());
    await user.click(screen.getByText(/editar/i));

    const input = screen.getByRole("textbox");
    await user.clear(input);
    await user.type(input, "Denis V2");
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    await waitFor(() => expect(successToast).toHaveBeenCalledWith("Nombre actualizado correctamente."));
  });

  it("enables MFA and displays the one-time recovery codes", async () => {
    server.use(
      http.get(`${API}/auth/me`, () =>
        HttpResponse.json({
          id: "user-1",
          name: "Denis Ávila",
          email: "denis@test.cr",
          isAdmin: false,
          hasMfa: false,
        }),
      ),
      http.get(`${API}/fosters/me`, () => new HttpResponse(null, { status: 404 })),
      http.get(`${API}/auth/me/sessions`, () => HttpResponse.json([])),
      http.get(`${API}/auth/me/trusted-devices`, () => HttpResponse.json([])),
      http.post(`${API}/auth/mfa/setup`, () =>
        HttpResponse.json({ secret: "ABCD1234", otpauthUri: "otpauth://totp/PawTrack:denis@test.cr" }),
      ),
      http.post(`${API}/auth/mfa/enable`, () =>
        HttpResponse.json({ recoveryCodes: ["recover-code-1", "recover-code-2"] }),
      ),
    );

    seedAuthStore();
    const user = userEvent.setup();
    renderWithProviders(<ProfilePage />);

    await user.click(await screen.findByRole("button", { name: "Configurar MFA" }));
    expect(await screen.findByText("ABCD1234")).toBeInTheDocument();
    await user.type(screen.getByLabelText("Código de autenticación"), "123456");
    await user.click(screen.getByRole("button", { name: "Activar MFA" }));

    expect(await screen.findByText("recover-code-1")).toBeInTheDocument();
    expect(screen.getByText("recover-code-2")).toBeInTheDocument();
  });

  it("steps up MFA before trusting the current device", async () => {
    let trustedDeviceName: string | undefined;
    server.use(
      http.get(`${API}/auth/me`, () =>
        HttpResponse.json({
          id: "user-1",
          name: "Denis Ávila",
          email: "denis@test.cr",
          isAdmin: false,
          hasMfa: true,
        }),
      ),
      http.get(`${API}/fosters/me`, () => new HttpResponse(null, { status: 404 })),
      http.get(`${API}/auth/me/sessions`, () =>
        HttpResponse.json([
          {
            sessionId: "session-1",
            startedAt: "2026-09-25T10:00:00Z",
            lastActivityAt: "2026-09-25T11:00:00Z",
            expiresAt: "2026-10-25T10:00:00Z",
            isCurrent: true,
          },
        ]),
      ),
      http.get(`${API}/auth/me/trusted-devices`, () => HttpResponse.json([])),
      http.post(`${API}/auth/mfa/step-up`, () => HttpResponse.json({ accessToken: "elevated-token", expiresIn: 900 })),
      http.post(`${API}/auth/me/trusted-devices`, async ({ request }) => {
        const body = (await request.json()) as { deviceName: string };
        trustedDeviceName = body.deviceName;
        return HttpResponse.json(
          {
            id: "device-1",
            deviceName: body.deviceName,
            createdAt: "2026-09-25T11:00:00Z",
            expiresAt: "2026-10-25T11:00:00Z",
          },
          { status: 201 },
        );
      }),
    );

    seedAuthStore();
    const user = userEvent.setup();
    renderWithProviders(<ProfilePage />);

    await user.type(await screen.findByLabelText("Código MFA para cambios de seguridad"), "123456");
    await user.click(screen.getByRole("button", { name: "Confiar dispositivo" }));

    await waitFor(() => expect(trustedDeviceName).toBe("Este dispositivo"));
    expect(useAuthStore.getState().accessToken).toBe("elevated-token");
  });
});
