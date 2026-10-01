import Image from "next/image";
import Link from "next/link";
import { SiteFooter, SiteHeader } from "@/components/site-chrome";
import { editorialArticles } from "@/lib/blog-content";

const benefits = [
  {
    number: "01",
    title: "Que puedan identificarla",
    text: "Un perfil pensado para reunir información útil y que puedas revisar qué compartes.",
    href: "/pet-id",
    action: "Conocer la identidad digital",
  },
  {
    number: "02",
    title: "Que vuelva a casa",
    text: "Una ruta clara para reportar una pérdida o ayudar cuando encuentras una mascota.",
    href: "/lost-pets",
    action: "Explorar la recuperación",
  },
  {
    number: "03",
    title: "Que reciba mejores cuidados",
    text: "Vacunas, documentos y recordatorios organizados en una experiencia sencilla.",
    href: "/features",
    action: "Explorar el cuidado conectado",
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
              <Link className="button button-dark" href="/register">
                Crear perfil <span aria-hidden="true">↗</span>
              </Link>
              <Link className="button button-light" href="/lost-pets/report">
                <span aria-hidden="true" className="button-dot" />
                Perdí una mascota
              </Link>
            </div>
            <Link className="found-link" href="/found-pets/report">
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
            <div className="hero-photo-frame">
              <Image
                alt="Perro mirando con curiosidad mientras descansa al aire libre"
                className="hero-photo"
                height={1000}
                priority
                src="https://images.unsplash.com/photo-1552053831-71594a27632d?auto=format&fit=crop&w=1200&q=85"
                width={820}
              />
              <div className="photo-caption">
                <span className="caption-paw" aria-hidden="true">
                  N
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
              <article className="benefit-item" key={benefit.number}>
                <span className="benefit-number">{benefit.number}</span>
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
              <Link className="button button-coral" href="/lost-pets/report">
                Perdí una mascota <span aria-hidden="true">↗</span>
              </Link>
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
              <article className="guide-card" key={article.slug}>
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
                <h3>Tú decides qué se comparte</h3>
                <p>El perfil público debe mostrar solo la información que el tutor elige.</p>
              </div>
            </article>
            <article>
              <span>02</span>
              <div>
                <h3>QR no es GPS</h3>
                <p>Explicamos qué puede hacer cada tecnología, sin promesas confusas.</p>
              </div>
            </article>
            <article>
              <span>03</span>
              <div>
                <h3>Historias y aliados, verificados</h3>
                <p>Publicaremos testimonios y organizaciones solo con permiso y evidencia.</p>
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
            <p>La identidad básica y la posibilidad de pedir ayuda deben estar al alcance de todas las familias.</p>
            <Link className="text-link" href="/plans">
              Ver la propuesta de planes <span aria-hidden="true">→</span>
            </Link>
          </div>
          <div className="plan-preview">
            <div className="plan-preview-top">
              <span>01 / PARA EMPEZAR</span>
              <span className="plan-stamp">N</span>
            </div>
            <h3>Esencial</h3>
            <p>Una identidad digital para tener lo importante a mano.</p>
            <ul>
              <li>Perfil de mascota</li>
              <li>Información de contacto elegida</li>
              <li>Acceso a los reportes básicos</li>
            </ul>
            <Link href="/register">
              Crear un perfil <span aria-hidden="true">↗</span>
            </Link>
            <small>La estructura de planes y precios está en definición.</small>
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
            <Link className="button button-light" href="/register">
              Crear perfil <span aria-hidden="true">↗</span>
            </Link>
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
