import { getProductUrl } from "../lib/site-config";

export function ProfileDemoForm() {
  return (
    <div className="profile-demo-grid">
      <div className="prototype-form">
        <p className="prototype-alert" role="note">
          El registro y los datos de tu mascota se gestionan en la app de PawTrack CR. Este sitio no solicita ni
          almacena información personal.
        </p>
        <a className="button button-dark form-submit" href={getProductUrl("/login")}>
          Crear cuenta o iniciar sesión en NALA <span aria-hidden="true">↗</span>
        </a>
      </div>
      <aside aria-live="polite" className="profile-preview">
        <span className="aside-index">VISTA PREVIA / PERFIL</span>
        <span aria-hidden="true" className="preview-avatar">
          N
        </span>
        <h2>Perfil digital</h2>
        <p>La cuenta y la información de cada mascota se crean y administran en el portal de NALA.</p>
        <span className="preview-status">App PawTrack CR</span>
      </aside>
    </div>
  );
}
