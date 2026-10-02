import Link from "next/link";
import { SiteFooter, SiteHeader } from "@/components/site-chrome";
import { LandingPicture } from "@/components/landing-picture";
import { ProviderOnboarding } from "@/components/provider-onboarding";
import { RecoveryQuickActions } from "@/components/recovery-quick-actions";
import { editorialArticles } from "@/lib/blog-content";
import { getLoginUrl, getProductUrl } from "@/lib/site-config";

const benefits = [
  {
    number: "01",
    title: "Compartir una identidad digital",
    text: "El perfil público puede mostrar foto, nombre, especie y raza; revisa esa información antes de compartir el QR.",
    facts: [
      "El QR abre el perfil; no transmite ubicación.",
      "Una pérdida activa puede añadir contacto y mensaje público.",
    ],
    href: "/pet-id",
    action: "Conocer la identidad digital",
    status: "En el producto",
    statusTone: "ready",
  },
  {
    number: "02",
    title: "Coordinar una búsqueda",
    text: "Inicia un reporte de pérdida o comunica un hallazgo desde PawTrack. La pérdida requiere cuenta y mascota registrada.",
    facts: [
      "Pérdida: requiere cuenta y mascota registrada.",
      "Hallazgo y avistamientos se reportan desde PawTrack; no se garantiza respuesta.",
    ],
    href: "/lost-pets",
    action: "Explorar la recuperación",
    status: "En el producto",
    statusTone: "ready",
  },
  {
    number: "03",
    title: "Organizar su cuidado",
    text: "Expediente, documentos y recordatorios organizan información veterinaria; no diagnostican ni sustituyen una consulta.",
    facts: [
      "Free y Plus: contador y vista previa del expediente.",
      "Familia: registros médicos y recordatorios activos.",
    ],
    href: "/features",
    action: "Explorar el cuidado conectado",
    status: "En el producto",
    statusTone: "ready",
  },
];

const pathways = [
  {
    id: "lost",
    kicker: "PERDÍ UNA MASCOTA",
    title: "Quiero reportar una pérdida",
    detail: "Requiere una cuenta y una mascota registrada.",
    href: "/lost-pets",
    action: "Ver los pasos",
  },
  {
    id: "found",
    kicker: "ENCONTRÉ UNA MASCOTA",
    title: "Quiero ayudar a encontrar a su familia",
    detail: "Reporta un hallazgo desde PawTrack; la landing no recibe la ubicación ni los datos del caso.",
    href: "/found-pets",
    action: "Ver cómo ayudar",
  },
  {
    id: "identity",
    kicker: "IDENTIDAD DIGITAL",
    title: "Quiero identificar a mi mascota",
    detail: "El QR abre un perfil público con datos básicos de la mascota; revísalo antes de compartirlo.",
    href: "/pet-id",
    action: "Explorar identidad",
  },
  {
    id: "provider",
    kicker: "SOY PRESTADOR",
    title: "Ofrezco servicios para mascotas",
    detail: "Envía una solicitud en PawTrack; su revisión no garantiza aprobación ni publicación en el directorio.",
    href: "/services#provider-onboarding",
    action: "Ver registro y revisión",
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

const freeCapabilityGroups = [
  {
    title: "Cuenta e identidad",
    items: [
      "1 mascota activa y 1 persona en la cuenta.",
      "Perfil público, 1 QR permanente y descarga del QR.",
      "Historial de escaneos disponible por 30 días.",
    ],
  },
  {
    title: "Recuperación",
    items: [
      "Reportes de pérdida ilimitados, con 1 caso activo a la vez.",
      "Case Room, avistamientos anónimos ilimitados y reportes de mascotas encontradas sin QR.",
      "Chat enmascarado, código de entrega segura y reporte de fraude.",
      "Notificaciones in-app y push; enlaces manuales para compartir.",
      "1 alerta geográfica en una ubicación, con radio fijo de 3 km.",
      "1 difusión automatizada por caso y participación en la cuadrícula de búsqueda.",
    ],
  },
  {
    title: "Cuidado y herramientas",
    items: [
      "Expediente médico en modo contador y vista previa.",
      "Directorios públicos, solicitudes de adopción y reservas de servicios.",
      "Exportación de datos personales, eliminación de cuenta y centro de ayuda.",
    ],
  },
  {
    title: "Límites de esta versión",
    items: [
      "No permite crear registros médicos ni recordatorios veterinarios activos; tampoco incluye exportación médica o grants clínicos.",
      "No incluye collar GPS, zonas seguras ni historial y alertas GPS.",
      "No admite miembros adicionales, activar o administrar la cuadrícula, ni consultar el historial completo de coordinación.",
      "No incluye repetición programada de la difusión automatizada.",
      "El matching visual por IA no está incluido en la versión gratuita.",
    ],
  },
];

export default function Home() {
  return (
    <>
      <SiteHeader />
      <main id="main">
        <section className="hero section-shell" id="home-hero">
          <div className="hero-copy">
            <p className="eyebrow">
              <span /> RED DE PROTECCIÓN PARA MASCOTAS · COSTA RICA
            </p>
            <h1>
              NALA, <em>una red de protección.</em>
            </h1>
            <p className="hero-intro">
              El hogar digital de tu mascota: identidad, herramientas de recuperación e información para organizar su
              cuidado, reunidos en PawTrack CR.
            </p>
            <div className="hero-actions">
              <a
                className="button button-dark"
                data-analytics-event="hero_primary_cta_clicked"
                href={getProductUrl("/login")}
              >
                Crear cuenta o iniciar sesión en PawTrack <span aria-hidden="true">↗</span>
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
            <a
              className="found-link"
              data-analytics-event="report_found_pet_clicked"
              href={getProductUrl("/encontre-mascota")}
            >
              ¿Encontraste una mascota? Continúa en PawTrack <span aria-hidden="true">↗</span>
            </a>
            <div className="hero-note">
              <span aria-hidden="true" className="note-check">
                ✓
              </span>
              <span>Revisa qué información compartes antes de continuar en la app.</span>
            </div>
          </div>
          <div className="hero-visual">
            <div className="hero-photo-frame">
              <LandingPicture
                alt="Familia reunida con su golden retriever en un parque de San José"
                asset="hero"
                className="hero-photo"
                priority
              />
            </div>
          </div>
        </section>
        <div className="recovery-sticky-boundary">
          <RecoveryQuickActions
            foundHref={getProductUrl("/encontre-mascota")}
            lostHref={getLoginUrl("/lost-pets/report")}
          />

          <section aria-label="Principios de NALA" className="proof-ribbon">
            <div>
              <span>01</span> Identidad digital
            </div>
            <div>
              <span>02</span> Herramientas de recuperación
            </div>
            <div>
              <span>03</span> Información para su cuidado
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
              {pathways.map((pathway) => (
                <article
                  className={`intent-card depth-surface${pathway.id === "lost" ? " intent-card-primary" : ""}`}
                  data-3d-depth="intent"
                  data-depth-strength="4"
                  data-journey={pathway.id}
                  key={pathway.id}
                >
                  <span className="intent-kicker">{pathway.kicker}</span>
                  <h3>{pathway.title}</h3>
                  <p>{pathway.detail}</p>
                  <Link className="intent-action" data-analytics-event="audience_selected" href={pathway.href}>
                    {pathway.action} <span aria-hidden="true">↗</span>
                  </Link>
                  {pathway.id === "lost" ? (
                    <div className="intent-alternatives">
                      <a href={`${getProductUrl("/register")}?return=%2Flost-pets%2Freport`}>Crear una cuenta</a>
                      <a href={getLoginUrl("/pets/new")}>Registra primero a tu mascota</a>
                    </div>
                  ) : null}
                </article>
              ))}
            </div>
          </section>

          <section aria-labelledby="welfare-report-title" className="welfare-report-section section-shell">
            <div className="welfare-report-main">
              <div className="welfare-report-copy">
                <p className="eyebrow">BIENESTAR ANIMAL</p>
                <h2 id="welfare-report-title">¿Necesitas reportar maltrato o un animal en riesgo?</h2>
                <p>El formulario de bienestar no es un servicio de emergencia.</p>
                <p>
                  Permite registrar cantón, severidad y hechos observables; el reporte puede ser anónimo y la ubicación
                  aproximada es opcional.
                </p>
              </div>
              <a
                className="welfare-report-link"
                data-analytics-event="report_welfare_case_clicked"
                href={getProductUrl("/bienestar/reportar")}
              >
                Reportar un caso de bienestar <span aria-hidden="true">↗</span>
              </a>
            </div>
            <p className="welfare-report-note" role="note">
              El reporte se registra en PawTrack, pero no es un servicio de emergencia ni sustituye una denuncia ante
              las autoridades; tampoco garantiza respuesta o derivación. Si hay peligro inmediato, contacta primero a
              las autoridades locales.
            </p>
          </section>

          <section className="benefits section-shell" id="beneficios">
            <div className="section-heading">
              <p className="eyebrow">UN PROPÓSITO · TRES PILARES</p>
              <h2>
                Un hogar digital para
                <br />
                <em>cada etapa.</em>
              </h2>
              <p>Funciones en la app, límites y requisitos visibles antes de continuar.</p>
            </div>
            <div aria-label="Significado del estado de las capacidades" className="capability-status-legend">
              <p>
                <strong>En el producto:</strong> flujo implementado en la app.
              </p>
              <p>
                <strong>Parcial:</strong> cubre una parte del recorrido.
              </p>
              <p>
                <strong>Operación externa:</strong> no verificada.
              </p>
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
                  <ul className="benefit-facts">
                    {benefit.facts.map((fact) => (
                      <li key={fact}>{fact}</li>
                    ))}
                  </ul>
                  <Link href={benefit.href}>
                    {benefit.action} <span aria-hidden="true">↗</span>
                  </Link>
                </article>
              ))}
            </div>
          </section>

          <section aria-labelledby="journey-title" className="journey-section section-shell">
            <div className="journey-copy">
              <p className="eyebrow">RECORRIDO DIGITAL</p>
              <h2 id="journey-title">
                Una identidad útil cuando llega el momento de <em>actuar.</em>
              </h2>
              <p>
                La imagen usa un QR de demostración. En PawTrack, el perfil público puede mostrar datos básicos de la
                mascota y sumar un mensaje cuando hay una pérdida activa.
              </p>
              <div className="journey-steps" aria-label="Etapas del recorrido de PawTrack">
                <span>
                  <b>01</b> Identidad
                </span>
                <span>
                  <b>02</b> Recuperación
                </span>
                <span>
                  <b>03</b> Cuidado
                </span>
              </div>
              <p className="prototype-note">
                El QR de la imagen es ilustrativo; el perfil de ejemplo usa datos ficticios y no recibe información.
              </p>
              <details className="profile-demo-disclosure">
                <summary>Ver perfil de ejemplo</summary>
                <div className="profile-demo-card">
                  <p className="profile-demo-label">PERFIL FICTICIO</p>
                  <h3>Luna</h3>
                  <p>Perra · 4 años · ejemplo de demostración</p>
                  <dl>
                    <div>
                      <dt>Señas</dt>
                      <dd>Pecho blanco y collar verde</dd>
                    </div>
                    <div>
                      <dt>Información compartida</dt>
                      <dd>
                        Un perfil real puede mostrar foto, nombre, especie y raza; una pérdida activa puede añadir
                        contacto y mensaje público.
                      </dd>
                    </div>
                  </dl>
                  <p>No se recopilan datos en esta demostración.</p>
                  <a href={getProductUrl("/register")}>
                    Crear mi perfil en PawTrack <span aria-hidden="true">↗</span>
                  </a>
                </div>
              </details>
            </div>
            <div className="journey-visual-stage">
              <LandingPicture
                alt="Dogtag con un código QR demostrativo en el collar de un perro"
                asset="dogtag"
                className="journey-dogtag-photo"
              />
            </div>
          </section>

          <section aria-labelledby="services-title" className="services-section section-shell">
            <div className="services-copy">
              <p className="eyebrow">ECOSISTEMA DE SERVICIOS</p>
              <h2 id="services-title">
                Encuentra apoyo para cada etapa de su <em>vida.</em>
              </h2>
              <p>
                Consulta perfiles y servicios publicados en el directorio de PawTrack; confirma condiciones y
                disponibilidad con cada prestador.
              </p>
              <p className="prototype-note">
                La presencia en el directorio no confirma afiliación, disponibilidad, precio, calidad ni verificación
                operativa de cada prestador.
              </p>
              <Link className="button button-dark" data-analytics-event="service_directory_clicked" href="/services">
                Buscar servicios <span aria-hidden="true">↗</span>
              </Link>
            </div>
            <div className="service-category-visual">
              <LandingPicture
                alt="Una veterinaria y una tutora acompañan a su perro en un entorno vecinal"
                asset="services"
                className="service-directory-photo"
              />
              <div aria-label="Categorías del directorio de servicios" className="service-category-grid">
                <span className="depth-surface" data-3d-depth="service" data-depth-strength="2">
                  Veterinarias
                </span>
                <span className="depth-surface" data-3d-depth="service" data-depth-strength="2">
                  Grooming
                </span>
                <span className="depth-surface" data-3d-depth="service" data-depth-strength="2">
                  Entrenamiento
                </span>
                <span className="depth-surface" data-3d-depth="service" data-depth-strength="2">
                  Hospedaje
                </span>
                <span className="depth-surface" data-3d-depth="service" data-depth-strength="2">
                  Paseos
                </span>
                <span className="depth-surface" data-3d-depth="service" data-depth-strength="2">
                  Cuidado temporal
                </span>
              </div>
            </div>
          </section>
          <ProviderOnboarding />

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
                  <h3>Qué verá quien escanee</h3>
                  <p>
                    El perfil puede mostrar foto, nombre, especie y raza; en una pérdida activa puede incluir nombre de
                    contacto y mensaje público, no teléfono ni correo del tutor.
                  </p>
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

          <section className="free-section section-shell">
            <div className="free-intro">
              <p className="eyebrow">UN BUEN COMIENZO PARA CADA HOGAR</p>
              <h2>
                Lo esencial para cuidar
                <br />y <em>proteger.</em>
              </h2>
              <p>
                Conoce todo lo que puedes hacer en la versión gratuita, con sus capacidades y límites claramente
                definidos.
              </p>
            </div>
            <div className="free-scope">
              <div className="free-scope-top">
                <span>01 / VERSIÓN GRATUITA</span>
                <span className="free-scope-stamp" aria-hidden="true">
                  N
                </span>
              </div>
              <h3>Alcance gratuito</h3>
              <div className="free-capability-groups">
                {freeCapabilityGroups.map((group) => (
                  <section className="free-capability-group" key={group.title}>
                    <h4>{group.title}</h4>
                    <ul>
                      {group.items.map((item) => (
                        <li key={item}>{item}</li>
                      ))}
                    </ul>
                  </section>
                ))}
              </div>
              <p className="free-scope-note">
                Alcance definido para la versión gratuita; su disponibilidad en producción no está verificada.
              </p>
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
        </div>

        <section className="closing-cta section-shell">
          <div>
            <p className="eyebrow eyebrow-light">NALA · COSTA RICA</p>
            <h2>
              Identidad, recuperación
              <br />y <em>cuidado.</em>
            </h2>
          </div>
          <div>
            <p>Un hogar digital para su identidad y herramientas para actuar con pasos claros en PawTrack CR.</p>
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
