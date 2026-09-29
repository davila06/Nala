import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useAuthStore } from "@/features/auth/store/authStore";
import { PhotoUpload } from "@/features/pets/components/PhotoUpload";
import { OnboardingWizard } from "@/features/pets/components/OnboardingWizard";
import { QRFlipCard } from "@/features/pets/components/QRFlipCard";

const qrApi = vi.hoisted(() => ({ getQrCode: vi.fn() }));
vi.mock("@/features/pets/api/petsApi", () => ({ petsApi: qrApi }));
vi.mock("@/shared/hooks/useHaptic", () => ({ useHaptic: () => ({ tap: vi.fn() }) }));

beforeEach(() => {
  vi.clearAllMocks();
  URL.createObjectURL = vi.fn(() => "blob:qr");
  URL.revokeObjectURL = vi.fn();
  qrApi.getQrCode.mockResolvedValue(new Blob(["qr"]));
  useAuthStore.setState({
    user: { id: "owner-1", name: "Ana Mora", email: "ana@example.com", role: "Owner", isAdmin: false },
  });
});

describe("P0 keyboard and accessible naming", () => {
  it("flips the QR card with Enter and Space", async () => {
    const user = userEvent.setup();
    render(<QRFlipCard petId="pet-1" petName="Luna" />);

    const flip = screen.getByRole("button", { name: "Mostrar código QR de Luna" });
    flip.focus();
    await user.keyboard("{Enter}");
    expect(await screen.findByRole("img", { name: "QR de Luna" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Mostrar foto" }));
    screen.getByRole("button", { name: "Mostrar código QR de Luna" }).focus();
    await user.keyboard(" ");
    expect(await screen.findByRole("img", { name: "QR de Luna" })).toBeInTheDocument();
  });

  it("exposes the file chooser by its visible accessible name", async () => {
    const onChange = vi.fn();
    render(<PhotoUpload onChange={onChange} />);

    const input = screen.getByLabelText("Subir foto de la mascota");
    expect(input).toHaveAttribute("type", "file");

    const file = new File(["pet photo"], "luna.png", { type: "image/png" });
    await userEvent.upload(input, file);
    expect(onChange).toHaveBeenCalledWith(file);
  });

  it("closes onboarding on Escape and restores focus to its trigger", async () => {
    const user = userEvent.setup();
    const trigger = document.createElement("button");
    trigger.textContent = "Open onboarding";
    document.body.append(trigger);
    trigger.focus();
    const onDismiss = vi.fn();

    function OnboardingHarness() {
      const [open, setOpen] = useState(true);
      return open ? (
        <OnboardingWizard
          onDismiss={() => {
            onDismiss();
            setOpen(false);
          }}
        />
      ) : null;
    }

    render(<OnboardingHarness />);
    expect(screen.getByRole("dialog", { name: /bienvenido/i })).toBeInTheDocument();
    await user.keyboard("{Escape}");

    expect(onDismiss).toHaveBeenCalledOnce();
    expect(trigger).toHaveFocus();
    trigger.remove();
  });

  it("describes visual search and QR as conditional aids, not guaranteed outcomes", async () => {
    const user = userEvent.setup();
    render(<MemoryRouter><OnboardingWizard /></MemoryRouter>);

    await user.click(screen.getByRole("button", { name: "Comenzar" }));
    expect(await screen.findByText(/si la búsqueda por imagen está disponible/i)).toBeInTheDocument();
    expect(screen.getByText(/no garantiza identificar/i)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Siguiente" }));
    expect(await screen.findByText(/perfil público/i)).toBeInTheDocument();
    expect(screen.getByText(/no garantiza el contacto/i)).toBeInTheDocument();
  });
});
