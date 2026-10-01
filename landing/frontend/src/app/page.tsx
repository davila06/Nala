import Image from "next/image";
import Link from "next/link";
import { SiteFooter, SiteHeader } from "@/components/site-chrome";
import { editorialArticles } from "@/lib/blog-content";
import { getLoginUrl, getProductUrl } from "@/lib/site-config";

const benefits = [
  {
    number: "01",
    title: "Que puedan identificarla",
    text: "Un perfil pensado para reunir información útil y que puedas revisar qué compartes.",
    href: "/pet-id",
    action: "Conocer la identidad digital",
    status: "Implementación documentada",
    statusTone: "ready",
  },
  {
    number: "02",
    title: "Que vuelva a casa",
    text: "Una ruta clara para reportar una pérdida o ayudar cuando encuentras una mascota.",
    href: "/lost-pets",
    action: "Explorar la recuperación",
    status: "Flujo en la app",
    statusTone: "ready",
  },
  {
    number: "03",
    title: "Que reciba mejores cuidados",
    text: "Vacunas, documentos y recordatorios organizados en una experiencia sencilla.",
    href: "/features",
    action: "Explorar el cuidado conectado",
    status: "Capacidad parcial",
    statusTone: "partial",
  },
];

const faqs = [
  {
    question: "¿QR, NFC o GPS muestran dónde está mi mascota?",
    answer:
      "No. Un QR abre un perfil al escanearlo; una etiqueta NFC compatible requiere configuración manual. Ninguno transmite ubicación. El GPS requiere un dispositivo y servicio aparte; la disponibilidad de hardware y proveedor de PawTrack CR no está verificada.",
  },
  {
    question: "¿Qué hago si perdí o encontré una mascota?",
    answer:
      "El landing te deriva a la app de PawTrack CR. El reporte de pérdida requiere una cuenta y una mascota registrada; el flujo de hallazgo está en la app. El landing no recibe datos del caso.",
  },
  {
    question: "¿NALA ofrece consultas veterinarias ahora?",
    answer:
      "PawTrack CR tiene funciones de registro y cuidado clínico; las consultas por video o audio no están implementadas. La app no sustituye atención veterinaria.",
  },
];

export default function Home() {
  return (
    <>
      <SiteHeader />
      <main id="main">
        <section className="hero section-shell">
          <div className="hero-copy">
            <p className="eyebrow">
              <span /> PARA CADA ETAPA DE SU VIDA
            </p>
            <h1>
              PawTrack CR <em>· NALA</em>
            </h1>
            <p className="hero-intro">
              NALA significa Núcleo de Animal de Localización y Asistencia: una plataforma para identidad, recuperación
              y cuidado animal en Costa Rica.
            </p>
            <div className="hero-actions">
              <a
                className="button button-dark"
                data-analytics-event="hero_primary_cta_clicked"
                href={getProductUrl("/login")}
              >
                Crear cuenta o iniciar sesión <span aria-hidden="true">↗</span>
              </a>
              <a
                className="button button-light"
                data-analytics-event="report_lost_pet_clicked"
                href={getLoginUrl("/lost-pets/report")}
              >
                <span aria-hidden="true" className="button-dot" />
                Perdí una mascota
              </a>
            </div>
            <Link className="found-link" data-analytics-event="report_found_pet_clicked" href="/found-pets/report">
              ¿Encontraste una mascota? Ayuda a que vuelva a casa <span aria-hidden="true">→</span>
            </Link>
            <div className="hero-note">
              <span aria-hidden="true" className="note-check">
                ✓
              </span>
              <span>Revisa qué información compartes antes de continuar en la app.</span>
            </div>
          </div>
          <div className="hero-visual">
            <div className="hero-photo-frame depth-surface" data-3d-depth="hero" data-depth-strength="2">
              <Image
                alt="Perro mirando con curiosidad mientras descansa al aire libre"
                className="hero-photo"
                fetchPriority="high"
                height={1000}
                priority
                quality={85}
                sizes="(max-width: 700px) 100vw, (max-width: 960px) 46vw, 510px"
                src="https://images.unsplash.com/photo-1552053831-71594a27632d?auto=format&fit=crop&w=1200&q=85"
                width={820}
              />
              <div className="photo-caption">
                <span className="caption-paw" aria-hidden="true">
                  🐾
                </span>
                <span>
                  <strong>Una identidad.</strong>
                  <br />
                  Toda una vida de cuidado.
                </span>
              </div>
            </div>
            <div aria-hidden="true" className="hero-orbit orbit-one" />
            <div aria-hidden="true" className="hero-orbit orbit-two" />
            <span aria-hidden="true" className="hero-spark">
              ✳
            </span>
          </div>
          <div aria-hidden="true" className="hero-index">
            CR <span>·</span> 01
          </div>
        </section>

        <section aria-label="Principios de NALA" className="proof-ribbon">
          <div>
            <span>01</span> Identificación que conecta
          </div>
          <div>
            <span>02</span> Recuperación sin barreras
          </div>
          <div>
            <span>03</span> Cuidado a lo largo de su vida
          </div>
        </section>

        <section aria-labelledby="intent-title" className="intent-section section-shell">
          <div className="intent-heading">
            <p className="eyebrow">EMPIEZA POR LO QUE NECESITAS HOY</p>
            <h2 id="intent-title">
              Una entrada clara para cada <em>situación.</em>
            </h2>
            <p>No necesitas entender toda la plataforma para dar el siguiente paso correcto.</p>
          </div>
          <div className="intent-grid">
            <Link
              className="intent-card intent-card-primary depth-surface"
              data-3d-depth="intent"
              data-depth-strength="4"
              data-analytics-event="audience_selected"
              href="/pet-id"
            >
              <span className="intent-kicker">SOY TUTOR</span>
              <strong>Quiero identificar a mi mascota</strong>
              <span>Perfil digital, QR y datos que tú decides compartir.</span>
              <span className="intent-action">
                Abrir el recorrido <span aria-hidden="true">↗</span>
              </span>
            </Link>
            <Link
              className="intent-card depth-surface"
              data-3d-depth="intent"
              data-depth-strength="4"
              data-analytics-event="audience_selected"
              href="/found-pets"
            >
              <span className="intent-kicker">ENCONTRÉ UNA MASCOTA</span>
              <strong>Quiero ayudar a encontrar a su familia</strong>
              <span>Un flujo de hallazgo que se completa en la app.</span>
              <span className="intent-action">
                Ver cómo ayudar <span aria-hidden="true">↗</span>
              </span>
            </Link>
            <Link
              className="intent-card depth-surface"
              data-3d-depth="intent"
              data-depth-strength="4"
              data-analytics-event="audience_selected"
              href="/business"
            >
              <span className="intent-kicker">REPRESENTO UNA ORGANIZACIÓN</span>
              <strong>Quiero conocer las capacidades</strong>
              <span>Clínicas, refugios y municipalidades, con límites visibles.</span>
              <span className="intent-action">
                Explorar alcance <span aria-hidden="true">↗</span>
              </span>
            </Link>
          </div>
        </section>

        <section className="benefits section-shell" id="beneficios">
          <div className="section-heading">
            <p className="eyebrow">MÁS QUE UNA PLACA</p>
            <h2>
              Todo lo que importa,
              <br />
              <em>en un mismo lugar.</em>
            </h2>
            <p>Menos información dispersa. Más momentos para estar juntos.</p>
          </div>
          <div className="benefit-grid">
            {benefits.map((benefit) => (
              <article
                className="benefit-item depth-surface"
                data-3d-depth="benefit"
                data-depth-strength="2"
                key={benefit.number}
              >
                <span className="benefit-number">{benefit.number}</span>
                <span className={`capability-status status-${benefit.statusTone}`}>{benefit.status}</span>
                <h3>{benefit.title}</h3>
                <p>{benefit.text}</p>
                <Link href={benefit.href}>
                  {benefit.action} <span aria-hidden="true">↗</span>
                </Link>
              </article>
            ))}
          </div>
        </section>

        <section className="how-section">
          <div className="how-inner section-shell">
            <div className="how-heading">
              <p className="eyebrow eyebrow-light">SENCILLO DESDE EL PRIMER DÍA</p>
              <h2>
                Una red de cuidado
                <br />
                que empieza <em>contigo.</em>
              </h2>
              <p>La tecnología ayuda. La confianza y las personas hacen la diferencia.</p>
              <Link className="text-link text-link-light" href="/features">
                Conoce la visión de NALA <span aria-hidden="true">→</span>
              </Link>
            </div>
            <div className="steps-list">
              <article className="step-item">
                <span className="step-index">01</span>
                <div>
                  <h3>Crea su identidad</h3>
                  <p>Reúne los datos que ayudan a reconocerla y cuidarla.</p>
                </div>
                <span aria-hidden="true" className="step-arrow">
                  ↗
                </span>
              </article>
              <article className="step-item">
                <span className="step-index">02</span>
                <div>
                  <h3>Conecta una placa</h3>
                  <p>
                    El QR abre el perfil al escanearlo; una etiqueta NFC compatible requiere escritura manual con una
                    app externa.
                  </p>
                </div>
                <span aria-hidden="true" className="step-arrow">
                  ↗
                </span>
              </article>
              <article className="step-item">
                <span className="step-index">03</span>
                <div>
                  <h3>Comparte el cuidado</h3>
                  <p>Invita a las personas de confianza a estar al tanto.</p>
                </div>
                <span aria-hidden="true" className="step-arrow">
                  ↗
                </span>
              </article>
              <p className="how-footnote">
                QR y NFC no transmiten ubicación. GPS requiere un dispositivo y servicio compatible; disponibilidad y
                cobertura de PawTrack CR no están verificadas.
              </p>
            </div>
          </div>
        </section>

        <section className="recovery-section section-shell">
          <div aria-hidden="true" className="recovery-illustration">
            <span className="map-ring ring-large" />
            <span className="map-ring ring-medium" />
            <span className="map-cross cross-one">+</span>
            <span className="map-cross cross-two">+</span>
            <div className="map-card">
              <span className="map-pin">N</span>
              <span>
                Una comunidad
                <br />
                puede acercarlos.
              </span>
            </div>
          </div>
          <div className="recovery-copy">
            <p className="eyebrow">CUANDO CADA MINUTO CUENTA</p>
            <h2>
              Perderse no debería
              <br />
              significar estar <em>solos.</em>
            </h2>
            <p>
              Un reporte organizado dentro de la app y pasos claros para compartir información. Revisa quién puede ver
              los datos de contacto antes de iniciar un caso.
            </p>
            <div className="recovery-actions">
              <a className="button button-coral" href={getLoginUrl("/lost-pets/report")}>
                Perdí una mascota <span aria-hidden="true">↗</span>
              </a>
              <Link className="text-link" href="/found-pets/report">
                Encontré una mascota <span aria-hidden="true">→</span>
              </Link>
            </div>
            <p className="prototype-note">
              Los reportes se completan en la app; esta landing no recibe datos del caso ni publica alertas.
            </p>
          </div>
        </section>

        <section className="care-band">
          <div className="care-inner section-shell">
            <div>
              <p className="eyebrow">EL CUIDADO TAMBIÉN ES PREVENCIÓN</p>
              <h2>
                Un historial claro.
                <br />
                <em>Más tranquilidad.</em>
              </h2>
            </div>
            <p>
              Vacunas, medicamentos y documentos importantes: organizados para que puedas compartirlos con las personas
              que cuidan de tu mascota.
            </p>
            <Link className="button button-outline" href="/features">
              Explorar funciones <span aria-hidden="true">↗</span>
            </Link>
          </div>
        </section>

        <section aria-labelledby="services-title" className="services-section section-shell">
          <div className="services-copy">
            <p className="eyebrow">ECOSISTEMA DE SERVICIOS</p>
            <h2 id="services-title">
              Encuentra apoyo para cada etapa de su <em>vida.</em>
            </h2>
            <p>
              Explora el directorio público de prestadores de PawTrack: servicios para mascotas, perfiles y detalles
              disponibles en el entorno conectado.
            </p>
            <p className="prototype-note">
              La presencia en el directorio no confirma afiliación, disponibilidad, precio, calidad ni verificación
              operativa de cada prestador.
            </p>
            <Link className="button button-dark" data-analytics-event="service_directory_clicked" href="/services">
              Buscar servicios <span aria-hidden="true">↗</span>
            </Link>
          </div>
          <div aria-label="Categorías del directorio de servicios" className="service-category-grid">
            <span>Veterinarias</span>
            <span>Grooming</span>
            <span>Entrenamiento</span>
            <span>Hospedaje</span>
            <span>Paseos</span>
            <span>Cuidado temporal</span>
          </div>
        </section>

        <section aria-labelledby="guides-title" className="guides-section section-shell">
          <div className="guides-heading">
            <div>
              <p className="eyebrow">GUÍAS NALA</p>
              <h2 id="guides-title">
                Información clara para cuidar con <em>confianza.</em>
              </h2>
            </div>
            <Link className="text-link" href="/blog">
              Ver todas las guías <span aria-hidden="true">→</span>
            </Link>
          </div>
          <div className="guide-grid">
            {editorialArticles.slice(0, 3).map((article, index) => (
              <article
                className="guide-card depth-surface"
                data-3d-depth="guide"
                data-depth-strength="2"
                key={article.slug}
              >
                <p className="guide-category">
                  {article.category} <span>· {String(index + 1).padStart(2, "0")}</span>
                </p>
                <h3>
                  <Link href={`/blog#${article.slug}`}>{article.title}</Link>
                </h3>
                <p>{article.summary}</p>
                <Link
                  aria-label={`Leer guía: ${article.title}`}
                  className="guide-read-link"
                  href={`/blog#${article.slug}`}
                >
                  Leer guía <span aria-hidden="true">↗</span>
                </Link>
              </article>
            ))}
          </div>
        </section>

        <section className="trust-section section-shell">
          <div className="trust-heading">
            <p className="eyebrow">LA CONFIANZA SE DEMUESTRA</p>
            <h2>
              Hecha con cuidado.
              <br />
              <em>Abierta sobre sus límites.</em>
            </h2>
          </div>
          <div className="trust-points">
            <article>
              <span>01</span>
              <div>
                <h3>Datos bajo tu decisión</h3>
                <p>El perfil público debe mostrar solo la información que el tutor elige.</p>
              </div>
            </article>
            <article>
              <span>02</span>
              <div>
                <h3>Capacidades con estado</h3>
                <p>Distinguimos lo implementado, lo parcial y lo que todavía no está disponible.</p>
              </div>
            </article>
            <article>
              <span>03</span>
              <div>
                <h3>Alianzas verificables</h3>
                <p>No presentamos organizaciones, cobertura ni resultados externos sin evidencia vigente.</p>
              </div>
            </article>
          </div>
        </section>

        <section className="plans-section section-shell">
          <div className="plans-intro">
            <p className="eyebrow">UN BUEN COMIENZO PARA CADA HOGAR</p>
            <h2>
              El cuidado no debería
              <br />
              depender de un <em>plan.</em>
            </h2>
            <p>
              La identidad y la recuperación son el punto de partida; los planes publicados se consultan desde el
              catálogo aprobado.
            </p>
            <Link className="text-link" data-analytics-event="pricing_viewed" href="/plans">
              Ver el estado de planes <span aria-hidden="true">→</span>
            </Link>
          </div>
          <div className="plan-preview depth-surface" data-3d-depth="plan" data-depth-strength="3">
            <div className="plan-preview-top">
              <span>01 / PARA EMPEZAR</span>
              <span className="plan-stamp">N</span>
            </div>
            <h3>Catálogo aprobado y consultable</h3>
            <p>
              Los planes e importes se consumen desde PawTrack; esta landing no mantiene una tabla duplicada de precios.
            </p>
            <Link href="/plans">
              Ver estado de planes <span aria-hidden="true">↗</span>
            </Link>
            <div className="plan-status-grid" aria-label="Estado del catálogo">
              <div>
                <span>CAPACIDADES</span>
                <strong>Tiers técnicos</strong>
                <small>Documentados en backend</small>
              </div>
              <div>
                <span>OFERTA</span>
                <strong>No publicada</strong>
                <small>Requiere aprobación</small>
              </div>
              <div>
                <span>COMPRA</span>
                <strong>No disponible</strong>
                <small>Sin checkout aquí</small>
              </div>
            </div>
            <small>La cuenta y cualquier contratación se gestionan en PawTrack según el entorno conectado.</small>
          </div>
        </section>

        <section className="faq-section section-shell">
          <div className="faq-intro">
            <p className="eyebrow">PREGUNTAS CLARAS</p>
            <h2>
              Lo que quieres
              <br />
              saber <em>primero.</em>
            </h2>
            <Link href="/contact" className="text-link">
              Más preguntas <span aria-hidden="true">→</span>
            </Link>
          </div>
          <div className="faq-list">
            {faqs.map((faq) => (
              <details key={faq.question}>
                <summary>
                  {faq.question}
                  <span aria-hidden="true">+</span>
                </summary>
                <p>{faq.answer}</p>
              </details>
            ))}
          </div>
        </section>

        <section className="closing-cta section-shell">
          <div>
            <p className="eyebrow eyebrow-light">NALA · COSTA RICA</p>
            <h2>
              Para que cada
              <br />
              historia siga <em>junta.</em>
            </h2>
          </div>
          <div>
            <p>Un perfil para reconocerla. Una comunidad para acompañarte. Una vida entera por cuidar.</p>
            <a className="button button-light" href={getProductUrl("/login")}>
              Crear cuenta o iniciar sesión <span aria-hidden="true">↗</span>
            </a>
          </div>
          <span aria-hidden="true" className="closing-mark">
            N
          </span>
        </section>
      </main>
      <SiteFooter />
    </>
  );
}
