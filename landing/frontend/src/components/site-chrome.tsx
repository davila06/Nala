"use client";

import type { MouseEvent } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { getLoginUrl, getProductUrl } from "../lib/site-config";

function PawTrailMark() {
  return (
    <span aria-hidden="true" className="brand-mark">
      <svg viewBox="0 0 30 30">
        <g className="brand-pawprint">
          <ellipse cx="8" cy="10" rx="3.2" ry="2.5" />
          <ellipse cx="4.2" cy="5.7" rx="1.4" ry="2" transform="rotate(-24 4.2 5.7)" />
          <ellipse cx="7.7" cy="3.9" rx="1.4" ry="2" transform="rotate(-8 7.7 3.9)" />
          <ellipse cx="11.1" cy="4.5" rx="1.4" ry="2" transform="rotate(12 11.1 4.5)" />
          <ellipse cx="13.4" cy="6.6" rx="1.35" ry="1.9" transform="rotate(27 13.4 6.6)" />
        </g>
        <g className="brand-pawprint" transform="translate(12 12) rotate(15 8 8) scale(.72)">
          <ellipse cx="8" cy="10" rx="3.2" ry="2.5" />
          <ellipse cx="4.2" cy="5.7" rx="1.4" ry="2" transform="rotate(-24 4.2 5.7)" />
          <ellipse cx="7.7" cy="3.9" rx="1.4" ry="2" transform="rotate(-8 7.7 3.9)" />
          <ellipse cx="11.1" cy="4.5" rx="1.4" ry="2" transform="rotate(12 11.1 4.5)" />
          <ellipse cx="13.4" cy="6.6" rx="1.35" ry="1.9" transform="rotate(27 13.4 6.6)" />
        </g>
      </svg>
    </span>
  );
}

const navigationGroups = [
  {
    id: "pets",
    label: "Mascotas",
    links: [
      { label: "Qué puedes hacer", href: "/features", description: "Identidad, recuperación y cuidado." },
      { label: "Identidad digital", href: "/pet-id", description: "Perfil público y datos que compartes." },
      { label: "Mascota perdida", href: "/lost-pets", description: "Requisitos y pasos para iniciar un reporte." },
      { label: "Encontré una mascota", href: "/found-pets", description: "Cómo registrar un hallazgo." },
      { label: "Placas QR y NFC", href: "/qr", description: "Qué hace cada tipo de identificación." },
    ],
  },
  {
    id: "ecosystem",
    label: "Ecosistema",
    links: [
      { label: "Servicios para mascotas", href: "/services", description: "Directorio y servicios publicados." },
      { label: "Clínicas", href: "/clinics", description: "Capacidades clínicas y sus permisos." },
      { label: "Refugios y ONGs", href: "/shelters", description: "Perfiles y flujos de adopción." },
      { label: "Municipalidades", href: "/municipalities", description: "Herramientas institucionales." },
      { label: "Organizaciones", href: "/business", description: "Módulos, estados y límites actuales." },
    ],
  },
  {
    id: "resources",
    label: "Recursos",
    links: [
      { label: "Guías NALA", href: "/blog", description: "Información práctica para familias." },
      { label: "Quiénes somos", href: "/about", description: "Propósito y alcance de PawTrack CR." },
      { label: "Contacto", href: "/contact", description: "Escribe al equipo de PawTrack." },
      { label: "Privacidad", href: "/privacy", description: "Información sobre datos y límites." },
      { label: "Accesibilidad", href: "/accessibility", description: "Alcance y estado de accesibilidad." },
    ],
  },
] as const;

export function SiteHeader() {
  const pathname = usePathname();
  const isActive = (path: string) => pathname === path || pathname.startsWith(`${path}/`);
  const closeAfterNavigation = (event: MouseEvent<HTMLAnchorElement>) => {
    const group = event.currentTarget.closest<HTMLDetailsElement>("details");
    if (group) group.open = false;

    const mobileMenu = event.currentTarget.closest<HTMLDetailsElement>(".mobile-menu");
    if (mobileMenu) mobileMenu.open = false;
  };

  return (
    <header className="site-header">
      <div className="header-inner">
        <Link aria-label="PawTrack CR, Núcleo Animal de Localización y Asistencia, inicio" className="brand" href="/">
          <PawTrailMark />
          <span>PawTrack CR</span>
        </Link>
        <nav aria-label="Navegación principal" className="desktop-nav">
          {navigationGroups.map((group) => {
            const groupIsActive = group.links.some((link) => isActive(link.href));

            return (
              <details
                className="desktop-nav-group"
                data-active={groupIsActive || undefined}
                data-nav-group={group.id}
                name="desktop-navigation-groups"
                key={group.id}
              >
                <summary>
                  {group.label}
                  <span aria-hidden="true">⌄</span>
                </summary>
                <div className="desktop-mega-panel">
                  <p className="desktop-mega-label">
                    {group.id === "pets"
                      ? "PARA CADA ETAPA"
                      : group.id === "ecosystem"
                        ? "MÓDULOS Y ALIADOS"
                        : "INFORMACIÓN"}
                  </p>
                  <div className="desktop-mega-links">
                    {group.links.map((link) => (
                      <Link
                        aria-current={isActive(link.href) ? "page" : undefined}
                        href={link.href}
                        key={link.href}
                        onClick={closeAfterNavigation}
                      >
                        <span>{link.label}</span>
                        <small>{link.description}</small>
                      </Link>
                    ))}
                  </div>
                </div>
              </details>
            );
          })}
          <Link aria-current={isActive("/plans") ? "page" : undefined} href="/plans">
            Planes
          </Link>
        </nav>
        <a
          aria-label="Perdí una mascota: iniciar reporte"
          className="header-lost-cta"
          href={getLoginUrl("/lost-pets/report")}
          title="Reportar una mascota perdida"
        >
          <span aria-hidden="true" className="header-lost-mark">
            !
          </span>
          <span>Perdí una mascota</span>
        </a>
        <a className="button button-small header-cta" href={getProductUrl("/login")}>
          Abrir PawTrack <span aria-hidden="true">↗</span>
        </a>
        <details className="mobile-menu">
          <summary
            aria-controls="mobile-navigation"
            aria-label="Menú principal"
            data-3d-depth="menu"
            data-depth-strength="2"
          >
            <span />
            <span />
            <span />
          </summary>
          <nav aria-label="Navegación móvil" id="mobile-navigation">
            {navigationGroups.map((group) => {
              const groupIsActive = group.links.some((link) => isActive(link.href));

              return (
                <details
                  className="mobile-nav-group"
                  data-active={groupIsActive || undefined}
                  key={group.id}
                  name="mobile-navigation-groups"
                >
                  <summary aria-controls={`mobile-${group.id}-links`}>{group.label}</summary>
                  <div id={`mobile-${group.id}-links`}>
                    {group.links.map((link) => (
                      <Link
                        aria-current={isActive(link.href) ? "page" : undefined}
                        href={link.href}
                        key={link.href}
                        onClick={closeAfterNavigation}
                      >
                        {link.label}
                      </Link>
                    ))}
                  </div>
                </details>
              );
            })}
            <Link aria-current={isActive("/plans") ? "page" : undefined} href="/plans" onClick={closeAfterNavigation}>
              Planes
            </Link>
            <a className="mobile-cta" href={getProductUrl("/login")} onClick={closeAfterNavigation}>
              Abrir PawTrack <span aria-hidden="true">↗</span>
            </a>
          </nav>
        </details>
      </div>
    </header>
  );
}

export function SiteFooter() {
  return (
    <footer className="site-footer">
      <div className="footer-main">
        <div className="footer-brand-block">
          <Link aria-label="PawTrack CR, Núcleo Animal de Localización y Asistencia, inicio" className="brand" href="/">
            <PawTrailMark />
            <span>PawTrack CR</span>
          </Link>
          <p>PawTrack CR · Núcleo Animal de Localización y Asistencia (NALA).</p>
        </div>
        <div>
          <h2>Explora</h2>
          <Link href="/pet-id">Identidad digital</Link>
          <Link href="/qr">Placas QR</Link>
          <Link href="/telemedicine">Telemedicina</Link>
          <Link href="/services">Servicios para mascotas</Link>
        </div>
        <div>
          <h2>Ayuda</h2>
          <Link href="/lost-pets">Mascota perdida</Link>
          <Link href="/found-pets">Mascota encontrada</Link>
          <Link href="/contact">Estado de contacto</Link>
        </div>
        <div>
          <h2>Organizaciones</h2>
          <Link href="/clinics">Clínicas</Link>
          <Link href="/shelters">Refugios y ONGs</Link>
          <Link href="/municipalities">Municipalidades</Link>
        </div>
      </div>
      <div className="footer-bottom">
        <span>© {new Date().getFullYear()} PawTrack CR · NALA</span>
        <span>Portal en desarrollo · Costa Rica</span>
        <div>
          <Link href="/privacy">Privacidad</Link>
          <Link href="/accessibility">Accesibilidad</Link>
        </div>
      </div>
    </footer>
  );
}
