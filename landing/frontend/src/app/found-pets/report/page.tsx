import type { Metadata } from "next";
import Link from "next/link";
import { ReportDemoForm } from "@/components/report-demo-form";
import { SiteFooter, SiteHeader } from "@/components/site-chrome";

export const metadata: Metadata = {
  title: "Reportar mascota encontrada en NALA",
  description:
    "Usa el flujo de reporte de hallazgo de NALA para ayudar a reunir una mascota con su familia.",
  robots: { index: false, follow: false },
};

export default function FoundPetReportPage() {
  return (
    <>
      <SiteHeader />
      <main className="form-page section-shell" id="main">
        <nav aria-label="Ruta de navegación" className="breadcrumbs">
          <Link href="/">Inicio</Link>
          <span aria-hidden="true">/</span>
          <Link href="/found-pets">Mascotas encontradas</Link>
          <span aria-hidden="true">/</span>
          <span>Reporte</span>
        </nav>
        <div className="form-page-heading">
          <p className="eyebrow">
            <span /> REPORTE DE MASCOTA ENCONTRADA
          </p>
          <h1>
            Gracias por ayudarle
            <br />a encontrar su <em>hogar.</em>
          </h1>
          <p>
            Describe una zona general y evita publicar direcciones exactas o
            datos de contacto personales.
          </p>
        </div>
        <ReportDemoForm mode="found" />
      </main>
      <SiteFooter />
    </>
  );
}
