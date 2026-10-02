import Image from "next/image";

const categories = [
  {
    name: "Veterinarias",
    description:
      "Consulta fichas y servicios publicados; afiliación y disponibilidad de cada centro no se verifican aquí.",
    alt: "Veterinaria examina a un perro junto a su tutora.",
    image: "service-veterinary-20261001",
  },
  {
    name: "Grooming",
    description:
      "Consulta opciones publicadas de baño y peluquería; confirma alcance y disponibilidad con cada prestador.",
    alt: "Groomer cepilla con cuidado a un perro tranquilo.",
    image: "service-grooming-20261001",
  },
  {
    name: "Entrenamiento",
    description: "Explora servicios de educación publicados; la oferta y las condiciones dependen de cada prestador.",
    alt: "Entrenadora practica una señal positiva con un perro en un parque.",
    image: "service-training-20261001",
  },
  {
    name: "Hospedaje",
    description: "Revisa opciones publicadas y confirma cupo, condiciones y acuerdos directamente con el prestador.",
    alt: "Un perro descansa junto a una cuidadora en un hospedaje hogareño.",
    image: "service-boarding-20261001",
  },
  {
    name: "Paseos",
    description: "Consulta servicios publicados y confirma zona, horario y disponibilidad con cada prestador.",
    alt: "Paseadora camina con dos perros por un barrio arbolado.",
    image: "service-walking-20261001",
  },
  {
    name: "Cuidado temporal",
    description: "Explora opciones publicadas y confirma disponibilidad, cobertura y acuerdos de cuidado.",
    alt: "Cuidadora temporal recibe a un perro en un hogar acogedor.",
    image: "service-temporary-care-20261001",
  },
] as const;

const serviceNotices = [
  {
    name: "Reservas y pagos",
    description: "Una solicitud no confirma cupo ni pago liquidado; los pagos a proveedores no son automáticos.",
    alt: "Una persona y una prestadora revisan una solicitud de reserva junto a un perro.",
    image: "service-booking-request-20261001",
  },
  {
    name: "Registro de prestadores",
    description: "PawTrack revisa las solicitudes; enviarlas no confirma afiliación, aprobación ni publicación.",
    alt: "Una prestadora completa la verificación de su perfil en el teléfono junto a un perro.",
    image: "service-provider-verification-20261001",
  },
] as const;

export function ServiceCategoryGrid() {
  return (
    <>
      <div aria-label="Categorías de servicios para mascotas" className="service-category-gallery">
        {categories.map((category, index) => (
          <article className="service-category-card" key={category.name}>
            <picture className="service-category-image">
              <source
                media="(max-width: 700px)"
                srcSet={`/assets/landing/mobile/${category.image}-mobile.avif`}
                type="image/avif"
              />
              <source
                media="(max-width: 700px)"
                srcSet={`/assets/landing/mobile/${category.image}-mobile.webp`}
                type="image/webp"
              />
              <source srcSet={`/assets/landing/desktop/${category.image}-desktop.avif`} type="image/avif" />
              <Image
                alt={category.alt}
                className="service-category-photo"
                height={900}
                loading="lazy"
                quality={80}
                sizes="(max-width: 700px) 45vw, (max-width: 1100px) 30vw, 400px"
                src={`/assets/landing/desktop/${category.image}-desktop.webp`}
                width={1600}
              />
            </picture>
            <div className="service-category-copy">
              <span>{String(index + 1).padStart(2, "0")}</span>
              <h3>{category.name}</h3>
              <p>{category.description}</p>
            </div>
          </article>
        ))}
      </div>
      <div aria-label="Información sobre reservas y registro" className="service-notice-gallery">
        {serviceNotices.map((notice, index) => (
          <article className="service-category-card" key={notice.name}>
            <picture className="service-category-image">
              <source
                media="(max-width: 700px)"
                srcSet={`/assets/landing/mobile/${notice.image}-mobile.avif`}
                type="image/avif"
              />
              <source
                media="(max-width: 700px)"
                srcSet={`/assets/landing/mobile/${notice.image}-mobile.webp`}
                type="image/webp"
              />
              <source srcSet={`/assets/landing/desktop/${notice.image}-desktop.avif`} type="image/avif" />
              <Image
                alt={notice.alt}
                className="service-category-photo"
                height={900}
                loading="lazy"
                quality={80}
                sizes="(max-width: 700px) 100vw, 50vw"
                src={`/assets/landing/desktop/${notice.image}-desktop.webp`}
                width={1600}
              />
            </picture>
            <div className="service-category-copy">
              <span>{String(index + 1).padStart(2, "0")}</span>
              <h3>{notice.name}</h3>
              <p>{notice.description}</p>
            </div>
          </article>
        ))}
      </div>
    </>
  );
}
