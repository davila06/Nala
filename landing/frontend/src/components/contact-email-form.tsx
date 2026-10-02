"use client";

import { useEffect, useState, type FormEvent } from "react";
import { CONTACT_SUPPORT_EMAIL, type ContactTopic } from "@/lib/contact-mailto";
import { getProductUrl } from "@/lib/site-config";

const topics: ContactTopic[] = ["Consulta general", "Problema técnico", "Consulta comercial"];

export function ContactEmailForm() {
  const [submitState, setSubmitState] = useState<
    "idle" | "sending" | "accepted" | "error" | "invalid" | "rate-limited"
  >("idle");
  const [topic, setTopic] = useState<ContactTopic>(topics[0]);
  const [message, setMessage] = useState("");
  const configuredApiUrl = process.env.NEXT_PUBLIC_API_URL?.replace(/\/$/, "");
  const apiUrl = configuredApiUrl || (process.env.NODE_ENV === "development" ? "http://localhost:5199" : "");

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const planName = params
      .get("plan")
      ?.replace(/[\u0000-\u001f\u007f]/g, "")
      .trim()
      .slice(0, 100);
    if (!planName) return;

    const tier = params
      .get("tier")
      ?.replace(/[\u0000-\u001f\u007f]/g, "")
      .trim()
      .slice(0, 40);
    const prefilledMessage = `Quisiera recibir información de precio, límites y condiciones del plan ${planName}${tier ? ` (${tier})` : ""}. Por favor, indíquenme su disponibilidad vigente.`;
    const timeoutId = window.setTimeout(() => {
      setTopic("Consulta comercial");
      setMessage(prefilledMessage);
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, []);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!apiUrl || submitState === "sending" || submitState === "accepted") {
      if (!apiUrl) setSubmitState("error");
      return;
    }

    const values = new FormData(event.currentTarget);
    const topic = String(values.get("topic"));

    if (!topics.includes(topic as ContactTopic)) {
      setSubmitState("error");
      return;
    }

    setSubmitState("sending");
    try {
      const response = await fetch(`${apiUrl}/api/public/contact`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: String(values.get("name") ?? ""),
          email: String(values.get("email") ?? ""),
          topic,
          message: String(values.get("message") ?? ""),
          website: String(values.get("website") ?? ""),
        }),
      });

      if (response.status === 429) {
        setSubmitState("rate-limited");
      } else if (response.status === 422) {
        setSubmitState("invalid");
      } else {
        setSubmitState(response.ok ? "accepted" : "error");
      }
    } catch {
      setSubmitState("error");
    }
  }

  return (
    <section
      aria-labelledby="contact-email-title"
      className="contact-email-section section-shell"
      id="contact-form"
      tabIndex={-1}
    >
      <div className="contact-email-intro">
        <p className="eyebrow">CONTACTO POR CORREO</p>
        <h2 id="contact-email-title">Cuéntanos en qué podemos ayudarte.</h2>
        <p>
          El formulario envía tu mensaje al equipo de soporte. La dirección documentada es {CONTACT_SUPPORT_EMAIL}; la
          aceptación para envío no garantiza entrega ni una respuesta.
        </p>
        <a className="contact-welfare-link" href={getProductUrl("/bienestar/reportar")}>
          Reportar maltrato o un animal en riesgo <span aria-hidden="true">↗</span>
        </a>
      </div>

      <form className="contact-email-form" onChange={() => setSubmitState("idle")} onSubmit={handleSubmit}>
        <div aria-hidden="true" className="contact-honeypot">
          <label>
            Sitio web
            <input autoComplete="off" name="website" tabIndex={-1} type="text" />
          </label>
        </div>
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
          <select
            name="topic"
            onChange={(event) => setTopic(event.target.value as ContactTopic)}
            required
            value={topic}
          >
            {topics.map((topic) => (
              <option key={topic} value={topic}>
                {topic}
              </option>
            ))}
          </select>
        </label>
        <label htmlFor="contact-message">Mensaje</label>
        <textarea
          aria-describedby="contact-message-privacy"
          id="contact-message"
          maxLength={2000}
          minLength={20}
          name="message"
          onChange={(event) => setMessage(event.target.value)}
          required
          rows={6}
          value={message}
        />
        <p className="contact-message-privacy" id="contact-message-privacy">
          No incluyas contraseñas, datos clínicos ni ubicación exacta.
        </p>
        <button
          className="button button-coral"
          disabled={submitState === "sending" || submitState === "accepted"}
          type="submit"
        >
          {submitState === "sending" ? "Enviando…" : submitState === "accepted" ? "Mensaje enviado" : "Enviar mensaje"}
          {submitState === "idle" ||
          submitState === "error" ||
          submitState === "invalid" ||
          submitState === "rate-limited" ? (
            <span aria-hidden="true">↗</span>
          ) : null}
        </button>
        {submitState === "accepted" ? (
          <p aria-live="polite" className="contact-submit-status" role="status">
            El proveedor aceptó el mensaje para envío. Esto no confirma su entrega ni garantiza una respuesta.
          </p>
        ) : null}
        {submitState === "rate-limited" ? (
          <p aria-live="assertive" className="contact-submit-error" role="alert">
            Has enviado varios mensajes. Espera un momento y vuelve a intentarlo.
          </p>
        ) : null}
        {submitState === "error" ? (
          <p aria-live="assertive" className="contact-submit-error" role="alert">
            No pudimos enviar el mensaje. Inténtalo de nuevo más tarde.
          </p>
        ) : null}
        {submitState === "invalid" ? (
          <p aria-live="assertive" className="contact-submit-error" role="alert">
            Revisa el correo y el mensaje: debe tener al menos 20 caracteres.
          </p>
        ) : null}
      </form>
    </section>
  );
}
