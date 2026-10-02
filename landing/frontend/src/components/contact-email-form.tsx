"use client";

import type { FormEvent } from "react";
import { buildContactMailto, CONTACT_SUPPORT_EMAIL, type ContactTopic } from "@/lib/contact-mailto";
import { getProductUrl } from "@/lib/site-config";

const topics: ContactTopic[] = ["Consulta general", "Problema técnico", "Consulta comercial"];

export function ContactEmailForm() {
  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const values = new FormData(event.currentTarget);
    const topic = String(values.get("topic"));

    if (!topics.includes(topic as ContactTopic)) return;

    window.location.href = buildContactMailto({
      name: String(values.get("name") ?? ""),
      email: String(values.get("email") ?? ""),
      topic: topic as ContactTopic,
      message: String(values.get("message") ?? ""),
    });
  }

  return (
    <section aria-labelledby="contact-email-title" className="contact-email-section section-shell">
      <div className="contact-email-intro">
        <p className="eyebrow">CONTACTO POR CORREO</p>
        <h2 id="contact-email-title">Cuéntanos en qué podemos ayudarte.</h2>
        <p>
          El formulario prepara un correo para <a href={`mailto:${CONTACT_SUPPORT_EMAIL}`}>{CONTACT_SUPPORT_EMAIL}</a>.
          Revisa y envía el borrador desde tu aplicación de correo.
        </p>
        <p className="contact-email-note" role="note">
          Esta página no envía ni almacena tu mensaje. La entrega y la respuesta dependen de tu aplicación y del buzón.
          No incluyas contraseñas, datos clínicos ni ubicación exacta.
        </p>
        <a className="contact-welfare-link" href={getProductUrl("/bienestar/reportar")}>
          Reportar maltrato o un animal en riesgo <span aria-hidden="true">↗</span>
        </a>
      </div>

      <form className="contact-email-form" onSubmit={handleSubmit}>
        <div className="contact-email-fields">
          <label>
            Tu nombre (opcional)
            <input autoComplete="name" maxLength={100} name="name" type="text" />
          </label>
          <label>
            Tu correo para responderte
            <input autoComplete="email" maxLength={254} name="email" required type="email" />
          </label>
        </div>
        <label>
          Tema
          <select name="topic" required>
            {topics.map((topic) => (
              <option key={topic} value={topic}>
                {topic}
              </option>
            ))}
          </select>
        </label>
        <label>
          Mensaje
          <textarea maxLength={2000} minLength={20} name="message" required rows={6} />
        </label>
        <button className="button button-coral" type="submit">
          Preparar correo <span aria-hidden="true">↗</span>
        </button>
      </form>
    </section>
  );
}
