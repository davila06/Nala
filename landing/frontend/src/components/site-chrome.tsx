import Link from "next/link";
import { getProductUrl } from "../lib/site-config";

export function SiteHeader() {
  return (
    <header className="site-header">
      <div className="header-inner">
        <Link aria-label="PawTrack CR, NALA, inicio" className="brand" href="/">
          <span aria-hidden="true" className="brand-mark">
            n
          </span>
          <span>NALA</span>
        </Link>
        <nav aria-label="Navegación principal" className="desktop-nav">
          <Link href="/features">Qué puedes hacer</Link>
          <Link href="/lost-pets">Mascotas perdidas</Link>
          <Link href="/plans">Planes</Link>
          <Link href="/business">Organizaciones</Link>
        </nav>
        <a className="button button-small header-cta" href={getProductUrl("/register")}>
          Crear perfil <span aria-hidden="true">↗</span>
        </a>
        <details className="mobile-menu">
          <summary aria-label="Abrir menú de navegación">
            <span />
            <span />
          </summary>
          <nav aria-label="Navegación móvil">
            <Link href="/features">Qué puedes hacer</Link>
            <Link href="/lost-pets">Mascotas perdidas</Link>
            <Link href="/found-pets">Encontré una mascota</Link>
            <Link href="/plans">Planes</Link>
            <Link href="/business">Organizaciones</Link>
            <a href={getProductUrl("/register")}>Crear perfil</a>
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
            <span>NALA</span>
          </Link>
          <p>PawTrack CR · Núcleo de Animal de Localización y Asistencia (NALA).</p>
        </div>
        <div>
          <h2>Explora</h2>
          <Link href="/pet-id">Identidad digital</Link>
          <Link href="/qr">Placas QR</Link>
          <Link href="/telemedicine">Telemedicina</Link>
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
