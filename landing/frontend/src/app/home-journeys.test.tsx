// @vitest-environment jsdom

import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.hoisted(() => {
  process.env.NEXT_PUBLIC_APP_URL = "https://app.pawtrack.test";
});

import Home from "./page";
import PublicPage from "./[slug]/page";

vi.mock("next/link", async () => {
  const React = await import("react");

  return {
    default: ({ href, children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement>) =>
      React.createElement("a", { ...props, href }, children),
  };
});
vi.mock("@/components/site-chrome", () => ({ SiteHeader: () => null, SiteFooter: () => null }));
vi.mock("@/components/landing-picture", async () => {
  const React = await import("react");

  return {
    LandingPicture: ({ alt, className }: { alt: string; className?: string }) =>
      React.createElement("img", { alt, className }),
  };
});

beforeEach(() => {
  vi.stubEnv("NEXT_PUBLIC_APP_URL", "https://app.pawtrack.test");
});

afterEach(() => {
  cleanup();
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

describe("home journeys", () => {
  it("hides public prices on the plans page and directs each plan to commercial contact", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://api.pawtrack.test");
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => [
          {
            id: "plan-1",
            tier: "UserPlus",
            displayName: "Plan Plus",
            monthlyPriceCrc: 3000,
          },
        ],
      }),
    );
    const page = await PublicPage({ params: Promise.resolve({ slug: "plans" }) });
    const { container } = render(page);

    await waitFor(() => expect(screen.getByText("Plan Plus")).toBeTruthy());
    expect(container.querySelector(".catalog-plan-price")).toBeNull();
    expect(container.querySelector(".inner-lead")?.textContent).toMatch(/precios no se publican en esta página/i);
    expect(container.querySelector(".inner-lead")?.textContent).toMatch(/solicita información comercial por correo/i);
    expect(screen.getByRole("link", { name: /consultar este plan por correo/i }).getAttribute("href")).toBe(
      "/contact?topic=Consulta+comercial&plan=Plan+Plus&tier=UserPlus",
    );
  });

  it("positions NALA as a protection network grounded in identity, recovery and care", () => {
    const { container } = render(<Home />);

    expect(screen.getByRole("heading", { level: 1, name: /NALA, una red de protección\./i })).toBeTruthy();
    expect(screen.getByText(/RED DE PROTECCIÓN PARA MASCOTAS · COSTA RICA/i)).toBeTruthy();
    expect(screen.getByText(/hogar digital.*identidad.*recuperación.*cuidado/i)).toBeTruthy();
    expect(
      within(container.querySelectorAll(".benefit-item")[1] as HTMLElement).getByRole("heading", {
        name: "Coordinar una búsqueda",
      }),
    ).toBeTruthy();
  });

  it("positions PawTrack as the connected NALA ecosystem", async () => {
    const page = await PublicPage({ params: Promise.resolve({ slug: "features" }) });
    const { container } = render(page);
    const closingSection = container.querySelector(".inner-bottom");

    expect(
      within(closingSection as HTMLElement).getByRole("heading", {
        level: 2,
        name: "Identidad digital. Recuperación inteligente. Ecosistema conectado.",
      }),
    ).toBeTruthy();
    expect(closingSection?.querySelector("h2 strong")?.textContent).toBe(
      "Identidad digital. Recuperación inteligente. Ecosistema conectado.",
    );
  });

  it("shows concrete identity, recovery and plan-aware care facts in the pillar cards", () => {
    const { container } = render(<Home />);
    const cards = container.querySelectorAll(".benefit-item");

    expect(within(cards[0] as HTMLElement).getByText(/foto, nombre, especie y raza/i)).toBeTruthy();
    expect(within(cards[1] as HTMLElement).getByText(/pérdida: requiere cuenta y mascota registrada/i)).toBeTruthy();
    expect(within(cards[2] as HTMLElement).getByText(/Free y Plus: contador y vista previa/i)).toBeTruthy();
  });

  it("uses consistent capability cards for organization pages", async () => {
    for (const slug of ["business", "clinics", "shelters", "municipalities"]) {
      const page = await PublicPage({ params: Promise.resolve({ slug }) });
      const { container } = render(page);
      const cards = container.querySelectorAll(".capability-card");

      expect(cards, slug).toHaveLength(slug === "clinics" ? 4 : 3);
      expect(Array.from(cards).every((card) => card.querySelector(".capability-status"))).toBe(true);
      expect(Array.from(cards).every((card) => card.querySelector(".capability-limit"))).toBe(true);
      if (slug === "clinics") {
        expect(container.querySelector(".capability-card-grid-balanced")).toBeTruthy();
      }
      cleanup();
    }
  });

  it("shows a descriptive image on each municipality capability card", async () => {
    const page = await PublicPage({ params: Promise.resolve({ slug: "municipalities" }) });
    const { container } = render(page);
    const cards = container.querySelectorAll(".capability-card");
    const images = Array.from(cards, (card) => card.querySelector(".capability-card-image"));

    expect(images).toHaveLength(3);
    expect(images.every((image) => image instanceof HTMLImageElement && image.alt.length > 0)).toBe(true);
  });

  it("describes clinic scanning, consent-gated records and plan-limited scan analytics", async () => {
    const page = await PublicPage({ params: Promise.resolve({ slug: "clinics" }) });
    const { container } = render(page);
    const cards = container.querySelectorAll(".capability-card");

    expect(screen.getByRole("heading", { level: 1, name: "Registros clínicos con permiso del tutor." })).toBeTruthy();
    expect(within(cards[0] as HTMLElement).getByText(/URL QR o un identificador de chip/i)).toBeTruthy();
    expect(within(cards[0] as HTMLElement).getByText(/no concede acceso a sus datos clínicos/i)).toBeTruthy();
    expect(within(cards[1] as HTMLElement).getByText(/vacunas, desparasitación, controles/i)).toBeTruthy();
    expect(within(cards[1] as HTMLElement).getByText(/grant activo aprobado por el tutor/i)).toBeTruthy();
    expect(
      within(cards[2] as HTMLElement).getByText(/no incluye consulta veterinaria por video ni audio/i),
    ).toBeTruthy();
    expect(within(cards[3] as HTMLElement).getByText(/ClinicPlus/i)).toBeTruthy();
    expect(within(cards[3] as HTMLElement).getByText(/no ofrece reportes de vacunas/i)).toBeTruthy();
  });

  it("hands the found-pet guide off to PawTrack's real report form", async () => {
    const page = await PublicPage({ params: Promise.resolve({ slug: "found-pets" }) });
    render(page);

    expect(screen.getByRole("link", { name: /continuar en nala/i }).getAttribute("href")).toBe(
      "https://app.pawtrack.test/encontre-mascota",
    );
  });

  it("describes the services ecosystem without promising affiliation or emergency availability", async () => {
    const page = await PublicPage({ params: Promise.resolve({ slug: "services" }) });
    const { container } = render(page);
    const closingSection = container.querySelector(".inner-bottom");

    expect(
      screen.getByRole("heading", { level: 1, name: "Servicios para cada etapa. Un ecosistema conectado." }),
    ).toBeTruthy();
    expect(screen.getByText(/NALA conecta a las familias con profesionales, comercios y organizaciones/i)).toBeTruthy();
    expect(
      screen.getByText(
        /no confirma afiliación, certificación profesional, calidad, precio, cupo, horarios ni disponibilidad/i,
      ),
    ).toBeTruthy();
    expect(screen.getByText(/módulos de tiendas, refugios y solicitudes de adopción/i)).toBeTruthy();
    expect(screen.getByText(/no es un servicio de emergencias ni garantiza atención inmediata/i)).toBeTruthy();
    expect(screen.getByText(/telemedicina no está implementada/i)).toBeTruthy();
    expect(screen.getByText(/seguros y transporte especializado no están verificados/i)).toBeTruthy();
    expect(
      within(closingSection as HTMLElement).getByText(
        /Más que una plataforma de identificación, NALA conecta a las familias/i,
      ),
    ).toBeTruthy();
    expect(closingSection?.querySelector("h2 strong")?.textContent).toBe(
      "Un perfil. Una comunidad. Un ecosistema completo.",
    );
  });

  it("offers four clear starting paths and keeps welfare reporting separate", () => {
    const { container } = render(<Home />);
    const selector = container.querySelector(".intent-grid");

    expect(selector?.querySelectorAll("[data-journey]")).toHaveLength(4);
    expect(selector?.querySelector('a[href="/lost-pets"]')).toBeTruthy();
    expect(selector?.querySelector('a[href="/found-pets"]')).toBeTruthy();
    expect(selector?.querySelector('a[href="/pet-id"]')).toBeTruthy();
    expect(selector?.querySelector('a[href="/services#provider-onboarding"]')).toBeTruthy();
    expect(
      container.querySelector('.welfare-report-section a[href="https://app.pawtrack.test/bienestar/reportar"]'),
    ).toBeTruthy();
  });

  it("provides a safe animal-welfare reporting path with an emergency caveat", () => {
    const { container } = render(<Home />);
    const welfareSection = container.querySelector(".welfare-report-section");

    expect(screen.getByRole("link", { name: /reportar un caso de bienestar/i }).getAttribute("href")).toBe(
      "https://app.pawtrack.test/bienestar/reportar",
    );
    expect(welfareSection).toBeTruthy();
    expect(welfareSection?.closest(".intent-section")).toBeNull();
    expect(
      within(welfareSection as HTMLElement).getByText("El formulario de bienestar no es un servicio de emergencia."),
    ).toBeTruthy();
    expect(within(welfareSection as HTMLElement).getByText(/peligro inmediato/i)).toBeTruthy();
    expect(
      within(welfareSection as HTMLElement).getByText(/ni sustituye una denuncia ante las autoridades/i),
    ).toBeTruthy();
  });

  it("explains the loss-report prerequisites and offers a registration alternative", () => {
    const { container } = render(<Home />);

    const lostPath = container.querySelector('[data-journey="lost"]');
    expect(within(lostPath as HTMLElement).getByText(/requiere una cuenta y una mascota registrada/i)).toBeTruthy();
    expect(
      within(lostPath as HTMLElement)
        .getByRole("link", { name: /crear una cuenta/i })
        .getAttribute("href"),
    ).toBe("https://app.pawtrack.test/register?return=%2Flost-pets%2Freport");
    expect(screen.getByRole("link", { name: /registra primero a tu mascota/i }).getAttribute("href")).toContain(
      "/login?return=%2Fpets%2Fnew",
    );
  });

  it("shows three illustrated, accessible lost-pet essentials", async () => {
    const page = await PublicPage({ params: Promise.resolve({ slug: "lost-pets" }) });
    const { container } = render(page);
    const essentials = container.querySelector('[aria-label="Puntos importantes"]');

    expect(essentials?.querySelectorAll(".lost-pet-step-image")).toHaveLength(3);
    expect(within(essentials as HTMLElement).getByRole("heading", { name: /prepara una descripción/i })).toBeTruthy();
    expect(
      within(essentials as HTMLElement).getByRole("heading", { name: /comparte una zona aproximada/i }),
    ).toBeTruthy();
    expect(within(essentials as HTMLElement).getByRole("heading", { name: /revisa el contacto/i })).toBeTruthy();
    expect(within(essentials as HTMLElement).getAllByRole("img")).toHaveLength(3);
  });

  it("pairs each lost-pet essential step with a relevant accessible image", async () => {
    const page = await PublicPage({ params: Promise.resolve({ slug: "lost-pets" }) });
    const { container } = render(page);
    const essentials = container.querySelector('[aria-label="Puntos importantes"]');

    expect(within(essentials as HTMLElement).getAllByRole("img")).toHaveLength(3);
    expect(within(essentials as HTMLElement).getByRole("img", { name: /prepara una descripción/i })).toBeTruthy();
    expect(within(essentials as HTMLElement).getByRole("img", { name: /zona aproximada/i })).toBeTruthy();
    expect(within(essentials as HTMLElement).getByRole("img", { name: /contacto y la privacidad/i })).toBeTruthy();
  });

  it("opens a clearly fictitious QR profile example without collecting data", () => {
    render(<Home />);
    const disclosure = screen.getByText("Ver perfil de ejemplo");

    fireEvent.click(disclosure);

    expect(screen.getByText("PERFIL FICTICIO")).toBeTruthy();
    expect(screen.getByText(/no se recopilan datos/i)).toBeTruthy();
    expect(screen.getByRole("link", { name: /crear mi perfil en pawtrack/i }).getAttribute("href")).toBe(
      "https://app.pawtrack.test/register",
    );
  });

  it("shows consistent capability definitions and real provider-registration handoff", () => {
    const { container } = render(<Home />);

    expect(screen.getByText("En el producto:")).toBeTruthy();
    expect(container.querySelectorAll(".capability-status")).toHaveLength(3);
    expect(
      Array.from(container.querySelectorAll(".capability-status")).every(
        (item) => item.textContent === "En el producto",
      ),
    ).toBe(true);
    expect(screen.getByText("Operación externa:")).toBeTruthy();

    const providerLink = screen.getByRole("link", { name: /registrar servicio en pawtrack/i });
    expect(providerLink.getAttribute("href")).toBe("https://app.pawtrack.test/servicio/registro");
  });

  it("describes the full documented free scope without exposing plan offers", () => {
    const { container } = render(<Home />);
    const freeScope = container.querySelector(".free-scope");

    expect(container.querySelectorAll(".benefit-item")[0].textContent).toMatch(/foto, nombre, especie y raza/i);
    expect(container.querySelectorAll(".benefit-item")[2].textContent).toMatch(/no diagnostica/i);
    expect(freeScope?.textContent).toMatch(/versión gratuita/i);
    expect(freeScope?.textContent).toMatch(/1 mascota activa y 1 persona/i);
    expect(freeScope?.textContent).toMatch(/1 caso activo a la vez/i);
    expect(freeScope?.textContent).toMatch(/avistamientos anónimos ilimitados/i);
    expect(freeScope?.textContent).toMatch(/No permite crear registros médicos/i);
    expect(freeScope?.textContent).toMatch(/matching visual por IA no está incluido/i);
    expect(freeScope?.textContent).toMatch(/No incluye collar GPS/i);
    expect(freeScope?.textContent).toMatch(/disponibilidad en producción no está verificada/i);
    expect(freeScope?.textContent).not.toMatch(/UserPlus|UserFamilia|₡|precio|catálogo completo/i);
    expect(screen.queryByRole("link", { name: /catálogo completo/i })).toBeNull();
  });

  it("describes public-profile fields without claiming owner contact details are exposed", () => {
    const { container } = render(<Home />);
    const trustCard = container.querySelector(".trust-points article");

    expect(within(trustCard as HTMLElement).getByRole("heading", { name: "Qué verá quien escanee" })).toBeTruthy();
    expect(within(trustCard as HTMLElement).getByText(/foto, nombre, especie y raza/i)).toBeTruthy();
    expect(within(trustCard as HTMLElement).getByText(/no teléfono ni correo del tutor/i)).toBeTruthy();
  });

  it("describes the public QR profile fields without promising selectable contact data", async () => {
    const page = await PublicPage({ params: Promise.resolve({ slug: "qr" }) });
    const { container } = render(page);
    const essentials = container.querySelector('[aria-label="Puntos importantes"]');

    expect(within(essentials as HTMLElement).getByText(/foto, nombre, especie y raza/i)).toBeTruthy();
    expect(within(essentials as HTMLElement).getByText(/teléfono ni correo del tutor/i)).toBeTruthy();
  });

  it("explains actual plan tiers and separates technical limits from commercial availability", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_URL", "");
    const page = await PublicPage({ params: Promise.resolve({ slug: "plans" }) });
    const { container } = render(page);
    const aside = container.querySelector(".inner-aside");
    const essentials = container.querySelector('[aria-label="Puntos importantes"]');

    expect(within(aside as HTMLElement).getByText(/planes activos y aprobados en el entorno conectado/i)).toBeTruthy();
    expect(
      within(essentials as HTMLElement).getByText(/Free es el alcance base técnico.*fila propia.*catálogo local/i),
    ).toBeTruthy();
    expect(within(essentials as HTMLElement).getByText(/UserPlus.*3 mascotas.*UserFamilia.*25/i)).toBeTruthy();
    expect(within(essentials as HTMLElement).getByText(/no existe un tier técnico Premium/i)).toBeTruthy();
    expect(within(essentials as HTMLElement).getByText(/checkout recurrente universal.*pago liquidado/i)).toBeTruthy();
  });

  it("keeps lost and found actions in a visible navigation immediately after the hero", () => {
    const { container } = render(<Home />);

    const hero = container.querySelector("#home-hero");
    const quickActions = screen.getByRole("navigation", { name: "Acciones rápidas de recuperación" });
    const boundary = quickActions.closest(".recovery-sticky-boundary");
    expect(quickActions.hasAttribute("hidden")).toBe(false);
    expect(Boolean((hero?.compareDocumentPosition(quickActions) ?? 0) & Node.DOCUMENT_POSITION_FOLLOWING)).toBe(true);
    expect(boundary?.contains(container.querySelector(".closing-cta"))).toBe(false);
    expect(within(quickActions).getByRole("link", { name: "Perdí" }).getAttribute("href")).toContain(
      "/login?return=%2Flost-pets%2Freport",
    );
    expect(within(quickActions).getByRole("link", { name: "Encontré" }).getAttribute("href")).toBe(
      "https://app.pawtrack.test/encontre-mascota",
    );
  });
});
