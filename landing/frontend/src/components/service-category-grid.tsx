import Image from "next/image";

const categories = [
  {
    name: "Veterinarias",
    description: "Consulta perfiles y servicios publicados; confirma la disponibilidad con cada prestador.",
    alt: "Veterinaria examina a un perro junto a su tutora.",
    image: "service-veterinary-20261001",
  },
  {
    name: "Grooming",
    description: "Explora opciones de baño, peluquería y cuidado estético para mascotas.",
    alt: "Groomer cepilla con cuidado a un perro tranquilo.",
    image: "service-grooming-20261001",
  },
  {
    name: "Entrenamiento",
    description: "Busca servicios de educación y acompañamiento conductual publicados.",
    alt: "Entrenadora practica una señal positiva con un perro en un parque.",
    image: "service-training-20261001",
  },
  {
    name: "Hospedaje",
    description: "Consulta opciones publicadas y acuerda las condiciones directamente.",
    alt: "Un perro descansa junto a una cuidadora en un hospedaje hogareño.",
    image: "service-boarding-20261001",
  },
  {
    name: "Paseos",
    description: "Descubre ofertas locales de paseo y acompañamiento.",
    alt: "Paseadora camina con dos perros por un barrio arbolado.",
    image: "service-walking-20261001",
  },
  {
    name: "Cuidado temporal",
    description: "Explora opciones de cuido puntual y confirma su cobertura.",
    alt: "Cuidadora temporal recibe a un perro en un hogar acogedor.",
    image: "service-temporary-care-20261001",
  },
] as const;

export function ServiceCategoryGrid() {
  return (
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
  );
}
