import { getProductUrl } from "../lib/site-config";

type ReportDemoFormProps = {
  mode: "lost" | "found";
};

export function ReportDemoForm({ mode }: ReportDemoFormProps) {
  const reportType = mode === "lost" ? "Mascota perdida" : "Mascota encontrada";
  const productUrl = getProductUrl(mode === "lost" ? "/register" : "/encontre-mascota");

  return (
    <div className="prototype-form">
      <p className="prototype-alert" role="note">
        {mode === "lost"
          ? "Para reportar una pérdida debes iniciar sesión y elegir una mascota registrada. Este sitio no solicita ni guarda detalles del caso."
          : "El reporte real se completa en la app PawTrack CR. Esta landing no recoge ubicación ni datos de contacto."}
      </p>
      <h2 className="form-mode-title">{reportType}</h2>
      <a className="button button-coral form-submit" href={productUrl}>
        {mode === "lost" ? "Crear cuenta o iniciar sesión" : "Reportar mascota encontrada en NALA"}
        <span aria-hidden="true">↗</span>
      </a>
    </div>
  );
}
