import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ShelterPublishPage from "@/features/adoptions/pages/ShelterPublishPage";
import { useAuthStore } from "@/features/auth/store/authStore";
import { renderWithProviders } from "../../utils/renderWithProviders";

const publishAnimal = vi.fn();

vi.mock("@/features/adoptions/hooks/useAdoptions", () => ({
  usePublishAnimal: () => ({ mutate: publishAnimal, isPending: false }),
  useUploadAdoptionPhoto: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

describe("ShelterPublishPage", () => {
  beforeEach(() => {
    publishAnimal.mockClear();
    sessionStorage.clear();
    useAuthStore
      .getState()
      .setAuth(
        { id: "ally-1", name: "Aliado", email: "ally@example.test", role: "Ally", isAdmin: false },
        "test-token",
      );
  });

  it("requires confirmation and preserves an unpublished draft without clinical notes", async () => {
    const { unmount } = renderWithProviders(<ShelterPublishPage />);
    fireEvent.change(screen.getByRole("textbox", { name: /nombre/i }), { target: { value: "Luna" } });
    fireEvent.change(screen.getByRole("textbox", { name: /historia/i }), {
      target: { value: "Animal listo para adopción" },
    });
    fireEvent.change(screen.getByRole("textbox", { name: /notas médicas/i }), { target: { value: "Dato sensible" } });
    fireEvent.click(screen.getByRole("button", { name: "Publicar animal" }));

    expect(screen.getByRole("dialog", { name: "Confirmar publicación" })).toBeInTheDocument();
    expect(publishAnimal).not.toHaveBeenCalled();
    await waitFor(() => expect(sessionStorage.getItem("pawtrack:adoption-draft:ally-1")).toContain("Luna"));
    expect(sessionStorage.getItem("pawtrack:adoption-draft:ally-1")).not.toContain("Dato sensible");

    unmount();
    renderWithProviders(<ShelterPublishPage />);
    expect(screen.getByRole("textbox", { name: /nombre/i })).toHaveValue("Luna");
    expect(screen.getByRole("textbox", { name: /notas médicas/i })).toHaveValue("");
    fireEvent.click(screen.getByRole("button", { name: "Descartar borrador" }));
    expect(screen.getByRole("textbox", { name: /nombre/i })).toHaveValue("");
    expect(sessionStorage.getItem("pawtrack:adoption-draft:ally-1")).toBeNull();
  });

  it("announces a failed publication and keeps the draft available for retry", () => {
    publishAnimal.mockImplementation((_payload: unknown, handlers: { onError?: () => void }) => {
      handlers.onError?.();
    });
    renderWithProviders(<ShelterPublishPage />);
    fireEvent.change(screen.getByRole("textbox", { name: /nombre/i }), { target: { value: "Luna" } });
    fireEvent.change(screen.getByRole("textbox", { name: /historia/i }), { target: { value: "Lista para adopción" } });
    fireEvent.click(screen.getByRole("button", { name: "Publicar animal" }));
    fireEvent.click(screen.getByRole("button", { name: "Confirmar publicación" }));

    expect(screen.getByRole("alert")).toHaveTextContent("No se pudo publicar");
    expect(screen.getByRole("dialog", { name: "Confirmar publicación" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: /nombre/i })).toHaveValue("Luna");
  });
});
