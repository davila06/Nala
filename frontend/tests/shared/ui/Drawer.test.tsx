import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { Drawer } from "@/shared/ui/Drawer";

describe("Drawer", () => {
  it("traps focus, closes with Escape, and restores focus", () => {
    const trigger = document.createElement("button");
    trigger.textContent = "Abrir panel";
    document.body.appendChild(trigger);
    trigger.focus();
    const onClose = vi.fn();

    const { rerender } = render(
      <Drawer isOpen onClose={onClose} title="Filtros">
        <button type="button">Aplicar</button>
      </Drawer>,
    );

    const dialog = screen.getByRole("dialog", { name: "Filtros" });
    const closeButton = screen.getByRole("button", { name: "Cerrar" });
    const actionButton = screen.getByRole("button", { name: "Aplicar" });
    expect(closeButton).toHaveFocus();

    actionButton.focus();
    fireEvent.keyDown(actionButton, { key: "Tab" });
    expect(closeButton).toHaveFocus();
    fireEvent.keyDown(closeButton, { key: "Tab", shiftKey: true });
    expect(actionButton).toHaveFocus();

    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(onClose).toHaveBeenCalledOnce();

    rerender(
      <Drawer isOpen={false} onClose={onClose} title="Filtros">
        <button type="button">Aplicar</button>
      </Drawer>,
    );
    expect(trigger).toHaveFocus();
    trigger.remove();
  });

  it("locks body scrolling while open and restores its prior value", () => {
    document.body.style.overflow = "auto";
    const { unmount } = render(
      <Drawer isOpen onClose={() => undefined} title="Filtros">
        <p>Contenido</p>
      </Drawer>,
    );

    expect(document.body.style.overflow).toBe("hidden");
    unmount();
    expect(document.body.style.overflow).toBe("auto");
    document.body.style.overflow = "";
  });
});
