import type { Metadata } from "next";
import Link from "next/link";
import { SiteFooter, SiteHeader } from "@/components/site-chrome";
import { ProfileDemoForm } from "@/components/profile-demo-form";

export const metadata: Metadata = {
  title: "Crear perfil digital",
  description: "Crea una cuenta en PawTrack CR y administra desde la app el perfil digital de tu mascota.",
  robots: { index: false, follow: false },
};

export default function RegisterPage() {
  return (
    <>
      <SiteHeader />
      <main className="form-page section-shell" id="main">
        <nav aria-label="Ruta de navegación" className="breadcrumbs">
          <Link href="/">Inicio</Link>
          <span aria-hidden="true">/</span>
          <span>Crear perfil</span>
        </nav>
        <div className="form-page-heading">
          <p className="eyebrow">
            <span /> PERFIL EN LA APP PAWTRACK CR
          </p>
          <h1>
            Su identidad digital
            <br />
            <em>empieza aquí.</em>
          </h1>
          <p>Crea tu cuenta en la app de PawTrack CR y administra desde ahí el perfil de tu mascota.</p>
        </div>
        <ProfileDemoForm />
      </main>
      <SiteFooter />
    </>
  );
}
