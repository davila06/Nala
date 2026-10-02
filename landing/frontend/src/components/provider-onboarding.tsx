import { getProductUrl } from "../lib/site-config";

const steps = [
  {
    number: "01",
    title: "Prepara tu perfil",
    description: "Nombre comercial, categoría, descripción y ubicación pública del servicio.",
  },
  {
    number: "02",
    title: "Envía la solicitud",
    description: "Completa tus datos de contacto en el formulario de PawTrack.",
  },
  {
    number: "03",
    title: "Espera la revisión",
    description: "PawTrack revisa cada solicitud antes de mostrarla en el directorio.",
  },
] as const;

export function ProviderOnboarding() {
  return (
    <section
      aria-labelledby="provider-onboarding-title"
      className="provider-onboarding section-shell"
      id="provider-onboarding"
    >
      <div className="provider-onboarding-heading">
        <p className="eyebrow">PARA PRESTADORES</p>
        <h2 id="provider-onboarding-title">Así se publica un servicio.</h2>
        <p>El registro vive en PawTrack. Enviar una solicitud no garantiza su aprobación ni publicación.</p>
      </div>
      <ol className="provider-onboarding-steps">
        {steps.map((step) => (
          <li key={step.number}>
            <span>{step.number}</span>
            <div>
              <h3>{step.title}</h3>
              <p>{step.description}</p>
            </div>
          </li>
        ))}
      </ol>
      <div className="provider-onboarding-action">
        <a className="button button-dark" href={getProductUrl("/servicio/registro")}>
          Registrar servicio en PawTrack <span aria-hidden="true">↗</span>
        </a>
        <p>Continuarás en la app de PawTrack.</p>
      </div>
    </section>
  );
}
