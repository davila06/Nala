import type { Metadata } from "next";
import Link from "next/link";
import { ReportDemoForm } from "@/components/report-demo-form";
import { SiteFooter, SiteHeader } from "@/components/site-chrome";

export const metadata: Metadata = {
  title: "Reportar mascota perdida en NALA",
  description:
    "Inicia sesión en NALA y selecciona una mascota registrada para iniciar un reporte de pérdida.",
  robots: { index: false, follow: false },
};

export default function LostPetReportPage() {
  return (
    <>
      <SiteHeader />
      <main className="form-page section-shell" id="main">
        <nav aria-label="Ruta de navegación" className="breadcrumbs">
          <Link href="/">Inicio</Link>
          <span aria-hidden="true">/</span>
          <Link href="/lost-pets">Mascotas perdidas</Link>
          <span aria-hidden="true">/</span>
          <span>Reporte</span>
        </nav>
        <div className="form-page-heading">
          <p className="eyebrow">
            <span /> REPORTE DE MASCOTA PERDIDA
          </p>
          <h1>
            Vamos paso a paso
            <br />
            para ayudarle a <em>volver.</em>
          </h1>
          <p>
            El reporte se inicia desde el perfil de una mascota registrada. No
            compartas información del caso en esta landing.
          </p>
        </div>
        <ReportDemoForm mode="lost" />
      </main>
      <SiteFooter />
    </>
  );
}
