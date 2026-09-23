import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Modal } from "@/shared/ui/Modal";

describe("Modal", () => {
  it("moves focus into the dialog, labels it, and restores focus on close", () => {
    const trigger = document.createElement("button");
    trigger.textContent = "Abrir";
    document.body.appendChild(trigger);
    trigger.focus();

    const { rerender } = render(
      <Modal isOpen onClose={() => undefined} title="Confirmar acción">
        <button type="button">Confirmar</button>
      </Modal>,
    );

    const dialog = screen.getByRole("dialog");
    expect(dialog).toHaveAttribute("aria-labelledby");
    expect(screen.getByRole("heading", { name: "Confirmar acción" })).toHaveAttribute(
      "id",
      dialog.getAttribute("aria-labelledby"),
    );
    expect(screen.getByRole("button", { name: "Confirmar" })).toHaveFocus();

    rerender(
      <Modal isOpen={false} onClose={() => undefined} title="Confirmar acción">
        <button type="button">Confirmar</button>
      </Modal>,
    );

    expect(trigger).toHaveFocus();
    trigger.remove();
  });

  it("closes with Escape", () => {
    let open = true;
    const onClose = () => {
      open = false;
    };

    render(
      <Modal isOpen={open} onClose={onClose} title="Cerrar">
        <button type="button">Acción</button>
      </Modal>,
    );

    fireEvent.keyDown(document, { key: "Escape" });
    expect(open).toBe(false);
  });
});
