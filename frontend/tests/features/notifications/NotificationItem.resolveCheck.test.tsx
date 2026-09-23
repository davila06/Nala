import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, useLocation } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { NotificationItemCard } from "@/features/notifications/components/NotificationItem";

const markReadMock = vi.fn();
const respondResolveCheckMock = vi.fn();

function LocationProbe() {
  const location = useLocation();
  return <output data-testid="location">{location.pathname}</output>;
}

vi.mock("@/features/notifications/hooks/useNotifications", () => ({
  useMarkNotificationRead: () => ({ mutate: markReadMock }),
  useRespondResolveCheck: () => ({
    mutate: respondResolveCheckMock,
    isPending: false,
  }),
}));

describe("NotificationItemCard resolve-check actions", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("opens the lost-pet case when a lost-pet alert is clicked", () => {
    render(
      <MemoryRouter>
        <NotificationItemCard
          notification={{
            id: "n-lost-1",
            type: "LostPetAlert",
            title: "Mascota perdida cerca de ti",
            body: "Se reportó una mascota en tu zona",
            isRead: false,
            relatedEntityId: "lost-1",
            createdAt: new Date().toISOString(),
          }}
        />
        <LocationProbe />
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByRole("button", { name: /mascota perdida cerca/i }));

    expect(screen.getByTestId("location")).toHaveTextContent("/lost/lost-1/case");
    expect(markReadMock).toHaveBeenCalledWith("n-lost-1");
  });

  it("renders resolve-check actions and sends affirmative response", () => {
    render(
      <MemoryRouter>
        <NotificationItemCard
          notification={{
            id: "n-1",
            type: "ResolveCheck",
            title: "¿Encontraste a Firulais?",
            body: "Confirma estado del reporte",
            isRead: false,
            relatedEntityId: "lost-1",
            createdAt: new Date().toISOString(),
          }}
        />
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByRole("button", { name: /sí, ya está en casa/i }));

    expect(respondResolveCheckMock).toHaveBeenCalledWith({
      id: "n-1",
      foundAtHome: true,
    });
    expect(markReadMock).not.toHaveBeenCalled();
  });
});
