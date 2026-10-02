export type ContactTopic = "Consulta general" | "Problema técnico" | "Consulta comercial";

export type ContactMailDetails = {
  name: string;
  email: string;
  topic: ContactTopic;
  message: string;
};

export const CONTACT_SUPPORT_EMAIL = "soporte@pawtrack.cr";

export function buildContactMailto({ name, email, topic, message }: ContactMailDetails): string {
  const body = [
    `Nombre: ${name.trim() || "No indicado"}`,
    `Correo de respuesta: ${email.trim()}`,
    "",
    message.trim(),
  ].join("\n");
  const params = new URLSearchParams({
    subject: `Contacto PawTrack CR · ${topic}`,
    body,
  });

  return `mailto:${CONTACT_SUPPORT_EMAIL}?${params.toString()}`;
}
