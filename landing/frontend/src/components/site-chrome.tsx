"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { getProductUrl } from "../lib/site-config";

export function SiteHeader() {
  const pathname = usePathname();
  const isActive = (path: string) => pathname === path || pathname.startsWith(`${path}/`);

  return (
    <header className="site-header">
      <div className="header-inner">
        <Link aria-label="PawTrack CR, NALA, inicio" className="brand" href="/">
          <span aria-hidden="true" className="brand-mark">
            n
          </span>
          <span>PawTrack CR</span>
        </Link>
        <nav aria-label="Navegación principal" className="desktop-nav">
          <Link
            data-3d-depth="nav"
            data-depth-strength="2"
            aria-current={isActive("/features") ? "page" : undefined}
            href="/features"
          >
            Qué puedes hacer
          </Link>
          <Link
            data-3d-depth="nav"
            data-depth-strength="2"
            aria-current={isActive("/lost-pets") ? "page" : undefined}
            href="/lost-pets"
          >
            Mascotas perdidas
          </Link>
          <Link
            data-3d-depth="nav"
            data-depth-strength="2"
            aria-current={isActive("/plans") ? "page" : undefined}
            href="/plans"
          >
            Planes
          </Link>
          <Link
            data-3d-depth="nav"
            data-depth-strength="2"
            aria-current={isActive("/services") ? "page" : undefined}
            href="/services"
          >
            Servicios
          </Link>
          <Link
            data-3d-depth="nav"
            data-depth-strength="2"
            aria-current={isActive("/business") ? "page" : undefined}
            href="/business"
          >
            Organizaciones
          </Link>
        </nav>
        <a className="button button-small header-cta" href={getProductUrl("/login")}>
          Abrir PawTrack <span aria-hidden="true">↗</span>
        </a>
        <details className="mobile-menu">
          <summary
            aria-controls="mobile-navigation"
            aria-label="Abrir menú de navegación"
            data-3d-depth="menu"
            data-depth-strength="2"
          >
            <span />
            <span />
          </summary>
          <nav aria-label="Navegación móvil" id="mobile-navigation">
            <Link aria-current={isActive("/features") ? "page" : undefined} href="/features">
              Qué puedes hacer
            </Link>
            <Link aria-current={isActive("/lost-pets") ? "page" : undefined} href="/lost-pets">
              Mascotas perdidas
            </Link>
            <Link aria-current={isActive("/found-pets") ? "page" : undefined} href="/found-pets">
              Encontré una mascota
            </Link>
            <Link aria-current={isActive("/plans") ? "page" : undefined} href="/plans">
              Planes
            </Link>
            <Link aria-current={isActive("/services") ? "page" : undefined} href="/services">
              Servicios
            </Link>
            <Link aria-current={isActive("/business") ? "page" : undefined} href="/business">
              Organizaciones
            </Link>
            <a href={getProductUrl("/login")}>
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
          <Link aria-label="PawTrack CR, NALA, inicio" className="brand" href="/">
            <span aria-hidden="true" className="brand-mark">
              n
            </span>
            <span>PawTrack CR</span>
          </Link>
          <p>PawTrack CR · Núcleo de Animal de Localización y Asistencia (NALA).</p>
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
