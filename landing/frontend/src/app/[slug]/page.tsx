import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { SiteFooter, SiteHeader } from "@/components/site-chrome";
import { ContactEmailForm } from "@/components/contact-email-form";
import { LandingPicture } from "@/components/landing-picture";
import { ServiceCategoryGrid } from "@/components/service-category-grid";
import { getLoginUrl, getProductUrl } from "@/lib/site-config";
import { PublicPlansCatalog } from "@/components/public-plans-catalog";

type PageDefinition = {
  eyebrow: string;
  title: string;
  lead: string;
  detail: string;
  points: string[];
  primaryLabel: string;
  primaryHref: string;
  secondaryLabel: string;
  secondaryHref: string;
  note?: string;
};

const pages: Record<string, PageDefinition> = {
  about: {
    eyebrow: "QUIÉNES SOMOS",
    title: "PawTrack CR · Núcleo Animal de Localización y Asistencia (NALA).",
    lead: "PawTrack CR reúne herramientas de identidad, recuperación y cuidado animal para Costa Rica.",
    detail:
      "El código y las pruebas describen capacidades de PawTrack CR. La relación jurídica entre PawTrack CR y NALA, y la titularidad de marca, siguen pendientes de confirmación.",
    points: [
      "El código contiene perfiles de mascotas, QR, reportes y módulos de salud; no equivale a disponibilidad productiva.",
      "QR, NFC, GPS, atención clínica remota y servicios externos tienen alcances distintos.",
      "No publicamos resultados, alianzas ni aprobaciones que no estén verificadas.",
    ],
    primaryLabel: "Ver capacidades y límites",
    primaryHref: "/features",
    secondaryLabel: "Estado de contacto",
    secondaryHref: "/contact",
  },
  "lost-pets": {
    eyebrow: "AYUDA PARA VOLVER A CASA",
    title: "Cuando se pierde, cada persona que sabe puede ayudar.",
    lead: "La app incluye un flujo para iniciar y organizar reportes de pérdida; el landing no recibe datos del caso.",
    detail:
      "El reporte se inicia en la app con una cuenta y una mascota registrada. La consulta de contacto de un caso activo puede devolver nombre y teléfono a cualquier cuenta autenticada.",
    points: [
      "Prepara una descripción que ayude a reconocer a tu mascota.",
      "Comparte una zona aproximada, no tu domicilio.",
      "Consulta quién puede acceder a los datos de contacto; no describimos este flujo como un relay anónimo.",
    ],
    primaryLabel: "Continuar en NALA",
    primaryHref: getLoginUrl("/lost-pets/report"),
    secondaryLabel: "Encontré una mascota",
    secondaryHref: "/found-pets",
    note: "El reporte se completa en el portal de NALA; esta landing no recibe datos del caso ni envía alertas.",
  },
  "found-pets": {
    eyebrow: "GRACIAS POR AYUDAR",
    title: "Encontraste una mascota. El siguiente paso puede acercarla a casa.",
    lead: "La app tiene un flujo para reportar una mascota encontrada; este landing no recopila la ubicación ni datos del caso.",
    detail:
      "El reporte de hallazgo se completa en la app, no en esta landing. Si la mascota tiene QR, su perfil público se abre sin iniciar sesión; evita divulgar su ubicación exacta.",
    points: [
      "Describe dónde la viste usando una zona general.",
      "Evita acercarte si el animal está asustado o herido.",
      "Si tiene una placa QR, escanéala sin divulgar sus datos públicamente.",
    ],
    primaryLabel: "Continuar en NALA",
    primaryHref: getProductUrl("/encontre-mascota"),
    secondaryLabel: "Buscar mascotas perdidas en el mapa",
    secondaryHref: getProductUrl("/map"),
    note: "El reporte se completa en el portal de NALA; esta landing no recibe ubicación ni datos de contacto.",
  },
  plans: {
    eyebrow: "PLANES NALA",
    title: "Planes y capacidades de PawTrack.",
    lead: "Consulta los planes publicados por PawTrack y sus capacidades en el catálogo aprobado del entorno conectado.",
    detail:
      "Los planes e importes se leen del catálogo del entorno conectado. La aprobación local no confirma contratación, impuestos, soporte ni disponibilidad en producción.",
    points: [
      "Los planes visibles deben estar activos y aprobados en el catálogo público.",
      "Los tiers varían entre tutores y organizaciones; revisa las capacidades y límites publicados.",
      "La disponibilidad, impuestos, contratación y soporte dependen del entorno y sus condiciones vigentes.",
    ],
    primaryLabel: "Abrir PawTrack",
    primaryHref: getProductUrl("/login"),
    secondaryLabel: "Ver alcance para organizaciones",
    secondaryHref: "/business",
    note: "La landing muestra el catálogo aprobado; la contratación se realiza en PawTrack según disponibilidad.",
  },
  services: {
    eyebrow: "SERVICIOS PARA MASCOTAS",
    title: "Encuentra apoyo para cada etapa de su vida.",
    lead: "PawTrack reúne un directorio público de prestadores y servicios para mascotas. Explora las opciones disponibles en el entorno conectado y revisa los detalles antes de contactar.",
    detail:
      "El directorio público permite descubrir perfiles y servicios publicados. La presencia de un prestador no confirma afiliación, disponibilidad, precio, calidad ni verificación operativa.",
    points: [
      "Veterinarias y cuidado clínico: consulta perfiles y servicios publicados; la disponibilidad profesional depende de cada prestador.",
      "Grooming, paseos, entrenamiento y hospedaje: descubre categorías del ecosistema sin asumir cobertura o reserva confirmada.",
      "Cuidado temporal y aliados: las capacidades del producto no acreditan una organización afiliada ni un SLA operativo.",
    ],
    primaryLabel: "Abrir buscador de servicios",
    primaryHref: getProductUrl("/servicios"),
    secondaryLabel: "Ver capacidades para organizaciones",
    secondaryHref: "/business",
    note: "Esta página explica el alcance; el directorio público y cualquier contacto se gestionan en PawTrack.",
  },
  features: {
    eyebrow: "CAPACIDADES PAWTRACK",
    title: "Una plataforma para identificar, recuperar y cuidar mejor.",
    lead: "PawTrack CR reúne módulos de identidad, recuperación, salud y coordinación institucional. Cada capacidad conserva su estado real y sus límites.",
    detail:
      "Esta página resume módulos presentes en el código. No confirma que todas las funciones estén habilitadas, probadas de extremo a extremo o disponibles en producción.",
    points: [
      "Identidad digital: perfil de mascota configurable, datos elegidos por el tutor y QR implementado.",
      "Recuperación: reportes de pérdida, hallazgos, avistamientos y coordinación del caso dentro de PawTrack.",
      "QR y NFC: el QR abre un perfil; NFC requiere configuración manual con una aplicación externa.",
      "Salud: expediente, documentos, timeline y recordatorios; no es diagnóstico ni tratamiento autónomo.",
      "Clínicas: grants de acceso, consultas administrativas y certificados; profesionales externos no verificados.",
      "Refugios y adopción: perfiles aliados, publicaciones, solicitudes y ferias; alianzas reales no verificadas.",
      "Municipalidades: perfiles, capturas y reportes institucionales; convenios oficiales no verificados.",
      "GPS e IA visual: capacidades condicionadas por proveedor, hardware, configuración y evaluación operativa.",
      "Marketplace y planes: directorios, reservas y catálogo aprobado; checkout universal y operación externa tienen límites.",
    ],
    primaryLabel: "Abrir PawTrack",
    primaryHref: getProductUrl("/login"),
    secondaryLabel: "Ver las placas QR",
    secondaryHref: "/qr",
  },
  "pet-id": {
    eyebrow: "IDENTIDAD DIGITAL",
    title: "Que lo importante esté donde puede ayudar.",
    lead: "Un perfil digital de mascota puede reunir rasgos identificables, contactos elegidos e información útil para su cuidado.",
    detail:
      "El perfil puede mostrar foto, nombre, especie y raza. Si hay una pérdida activa, añade nombre de contacto y mensaje público; no publica el teléfono ni el correo del tutor. Revísalo antes de compartir el QR.",
    points: [
      "Revisa la información visible antes de compartir o activar un perfil.",
      "El QR abre un perfil al escanearlo; NFC requiere etiqueta compatible y escritura manual externa.",
      "La identidad digital complementa, no reemplaza, el microchip ni la atención veterinaria.",
    ],
    primaryLabel: "Crear perfil",
    primaryHref: getProductUrl("/login"),
    secondaryLabel: "Entender las placas QR",
    secondaryHref: "/qr",
  },
  qr: {
    eyebrow: "IDENTIFICACIÓN QR",
    title: "Una lectura puede abrir el camino de regreso.",
    lead: "Una placa QR puede enlazar con un perfil que el tutor mantiene actualizado y configura para compartir de forma segura.",
    detail:
      "El QR abre el perfil público de la mascota en el navegador y requiere conexión a internet; no transmite ubicación. La disponibilidad de placas físicas no está verificada.",
    points: [
      "La persona escanea con la cámara de su teléfono.",
      "El tutor elige qué datos de contacto se muestran.",
      "El perfil QR está implementado; disponibilidad de placa física, compra y entrega no está verificada.",
    ],
    primaryLabel: "Crear perfil",
    primaryHref: getProductUrl("/login"),
    secondaryLabel: "Comparar QR y NFC",
    secondaryHref: "/nfc",
  },
  nfc: {
    eyebrow: "IDENTIFICACIÓN NFC",
    title: "Una etiqueta NFC compatible puede abrir una URL configurada.",
    lead: "La app incluye una guía para escribir manualmente la URL del perfil con NFC Tools, una aplicación externa.",
    detail:
      "La guía permite escribir manualmente la URL del perfil en una etiqueta compatible con una app externa. PawTrack no configura ni valida el chip NFC.",
    points: [
      "La guía requiere una etiqueta compatible y escritura manual mediante una app externa.",
      "La lectura depende del teléfono y del chip; debe comprobarse en los dispositivos que se usarán.",
      "NFC no es GPS; el bundle modelado no confirma compra, stock ni fulfillment.",
    ],
    primaryLabel: "Crear cuenta en la app",
    primaryHref: getProductUrl("/login"),
    secondaryLabel: "Ver identificación QR",
    secondaryHref: "/qr",
  },
  telemedicine: {
    eyebrow: "SALUD Y ORIENTACIÓN",
    title: "La telemedicina audiovisual no está disponible.",
    lead: "PawTrack CR incluye consultas clínicas administrativas, pero el repositorio no implementa consultas veterinarias por video o audio.",
    detail:
      "El registro clínico y las consultas administrativas no son telemedicina: no hay sala audiovisual, tokens ACS, grabación ni agenda remota implementados.",
    points: [
      "Una consulta registrada en la app no equivale a atención remota.",
      "No se verificó operación de profesionales o proveedores externos.",
      "PawTrack CR no ofrece diagnóstico y no reemplaza la atención veterinaria.",
    ],
    primaryLabel: "Ver capacidades clínicas",
    primaryHref: "/clinics",
    secondaryLabel: "Ver estado para organizaciones",
    secondaryHref: "/business",
  },
  marketplace: {
    eyebrow: "PRODUCTOS Y SERVICIOS",
    title: "Directorios y reservas parciales; operación comercial pendiente.",
    lead: "El producto contiene directorios, servicios, reservas y pedidos, pero esta landing no publica un catálogo para compra.",
    detail:
      "Hay directorios, servicios, reservas y pedidos parciales. No se acredita inventario transaccional, pago liquidado, payout ni entrega comercial activa.",
    points: [
      "Una solicitud o reserva no prueba que un pago haya sido liquidado.",
      "Las tiendas administran disponibilidad y entrega; no se garantiza inventario.",
      "Membresías, comisión, precios y operación de proveedores no son oferta aprobada.",
    ],
    primaryLabel: "Ver el alcance para organizaciones",
    primaryHref: "/business",
    secondaryLabel: "Conocer identificación QR",
    secondaryHref: "/qr",
  },
  community: {
    eyebrow: "COMUNIDAD",
    title: "Una red local requiere operación y acuerdos verificables.",
    lead: "La app contiene reportes y flujos de coordinación; no se ha verificado una comunidad pública con cobertura, moderación o SLA operativos.",
    detail:
      "La app contiene reportes y flujos de coordinación; no se verifican cobertura territorial, moderación, respuesta de terceros ni una comunidad operativa.",
    points: [
      "Los reportes de pérdida, hallazgo y avistamiento existen en la app.",
      "La cobertura geográfica, moderación y respuesta de terceros no están verificadas.",
      "No se afirma que un aliado o una alerta externa vaya a responder.",
    ],
    primaryLabel: "Ver flujo de mascotas perdidas",
    primaryHref: "/lost-pets",
    secondaryLabel: "Estado de contacto",
    secondaryHref: "/contact",
  },
  clinics: {
    eyebrow: "PARA CLÍNICAS VETERINARIAS",
    title: "Módulos clínicos en el producto; disponibilidad por clínica no verificada.",
    lead: "El código incluye expediente, permisos de acceso y funciones administrativas clínicas; no acredita una clínica afiliada ni una operación en producción.",
    detail:
      "El código incluye expediente, permisos y herramientas administrativas clínicas. No confirma centros afiliados, profesionales externos aprobados ni operación local.",
    points: [
      "El expediente puede compartir información mediante grants y permisos del producto.",
      "El contenido clínico no equivale a diagnóstico ni a historia clínica externa integrada.",
      "Pilotos, contratos, disponibilidad de profesionales e integraciones externas no están verificados.",
    ],
    primaryLabel: "Ver el estado para organizaciones",
    primaryHref: "/business",
    secondaryLabel: "Conocer PawTrack CR · NALA",
    secondaryHref: "/about",
  },
  shelters: {
    eyebrow: "PARA REFUGIOS Y ONGs",
    title: "Perfiles aliados y adopciones en el producto; alianzas reales no verificadas.",
    lead: "El código contiene perfiles de aliados y flujos de adopción, pero el repositorio no acredita convenios u operación de refugios afiliados.",
    detail:
      "El producto tiene perfiles y flujos de adopción; no acredita refugios afiliados, convenios, cobertura ni la operación de una ONG concreta.",
    points: [
      "Existen superficies de publicación y solicitud de adopción en el producto.",
      "Las organizaciones reales deben revisar y autorizar los datos que publican.",
      "No hay operación, cobertura ni SLA de una ONG concreta verificados.",
    ],
    primaryLabel: "Ver capacidades del producto",
    primaryHref: "/features",
    secondaryLabel: "Ver mascotas encontradas",
    secondaryHref: "/found-pets",
  },
  municipalities: {
    eyebrow: "PARA MUNICIPALIDADES",
    title: "Herramientas institucionales en el producto; convenios no verificados.",
    lead: "El producto incluye perfiles, capturas y reportes municipales internos. No se ha verificado integración oficial, convenio ni operación con una municipalidad.",
    detail:
      "El producto incluye perfiles y reportes institucionales internos. No se verifican integración oficial, convenio ni despliegue municipal.",
    points: [
      "El acceso depende de roles y alcances configurados en el producto.",
      "Los reportes internos no equivalen a una integración oficial con autoridades.",
      "No se afirma convenio, despliegue, cobertura ni SLA municipal activo.",
    ],
    primaryLabel: "Ver capacidades del producto",
    primaryHref: "/features",
    secondaryLabel: "Conocer el enfoque de confianza",
    secondaryHref: "/about",
  },
  blog: {
    eyebrow: "RECURSOS",
    title: "Información práctica para cuidar y ayudar mejor.",
    lead: "La biblioteca de este sitio incluye guías generales sobre identificación, pérdida y hallazgo, cuidado y adopción.",
    detail:
      "Las guías son informativas y generales; el contenido de salud no sustituye la atención veterinaria y requiere revisión profesional y adaptación local.",
    points: [
      "Guías de preparación para una búsqueda de mascota perdida.",
      "Diferencias entre QR, NFC, microchip y GPS.",
      "El contenido es informativo, puede variar por localidad y no sustituye atención profesional.",
    ],
    primaryLabel: "Ver el flujo de recuperación",
    primaryHref: "/lost-pets",
    secondaryLabel: "Ver capacidades y límites",
    secondaryHref: "/features",
  },
  contact: {
    eyebrow: "CANALES DE CONTACTO",
    title: "Contacta al equipo de PawTrack.",
    lead: "Completa el formulario para enviar un mensaje al equipo de soporte desde PawTrack.",
    detail:
      "La dirección soporte@pawtrack.cr aparece documentada como contacto de soporte; esta landing no puede confirmar entrega, recepción ni tiempos de respuesta.",
    points: [
      "Para una pérdida o hallazgo, continúa en la app de PawTrack CR y usa los flujos operativos específicos.",
      "Para maltrato o un animal en riesgo, usa el reporte de bienestar; ante peligro inmediato contacta a las autoridades locales.",
      "No incluyas contraseñas, información clínica ni ubicación exacta en el correo.",
    ],
    primaryLabel: "Ir al formulario",
    primaryHref: "#contact-form",
    secondaryLabel: "Volver al inicio",
    secondaryHref: "/",
    note: "PawTrack solicitará el envío por correo. La aceptación del proveedor no garantiza entrega ni respuesta.",
  },
  business: {
    eyebrow: "NALA PARA ORGANIZACIONES",
    title: "PawTrack CR para organizaciones: módulos y límites actuales.",
    lead: "El repositorio incluye módulos para clínicas, aliados, adopciones, proveedores y municipalidades; no acredita acuerdos o servicios operativos con organizaciones reales.",
    detail:
      "Esta página informa capacidades técnicas; no ofrece formulario comercial, demo, piloto, contrato ni SLA para organizaciones.",
    points: [
      "Clínicas: expediente y grants de acceso en código; profesionales/centros externos no verificados.",
      "Refugios: perfiles y adopciones en código; organizaciones afiliadas no verificadas.",
      "Municipalidades: captura y reportes internos; convenios e integraciones oficiales no verificados.",
    ],
    primaryLabel: "Ver capacidades clínicas",
    primaryHref: "/clinics",
    secondaryLabel: "Ver capacidades para refugios",
    secondaryHref: "/shelters",
  },
  privacy: {
    eyebrow: "PRIVACIDAD",
    title: "Privacidad: revisa los avisos antes de usar la app.",
    lead: "El landing no envía ni almacena mensajes. El formulario de contacto prepara un correo en la aplicación del visitante.",
    detail:
      "Los términos y la política de privacidad del repositorio siguen en borrador. Los controles presentes en código no prueban operación ni aprobación jurídica.",
    points: [
      "El correo se prepara en tu dispositivo; evita incluir información clínica, contraseñas o ubicación exacta.",
      "El backend implementa exportación y jobs configurables de retención; operación y ejecución en producción no están verificadas.",
      "Esta página no es una política aprobada ni una declaración de cumplimiento legal.",
    ],
    primaryLabel: "Volver al inicio",
    primaryHref: "/",
    secondaryLabel: "Estado de contacto",
    secondaryHref: "/contact",
  },
  accessibility: {
    eyebrow: "ACCESIBILIDAD",
    title: "La accesibilidad requiere pruebas, no solo intención.",
    lead: "El landing usa navegación semántica y controles HTML, pero esta revisión no certifica su experiencia con teclado, lector de pantalla o ampliación.",
    detail:
      "No se ha realizado una auditoría WCAG 2.2 AA ni pruebas completas con teclado, lector de pantalla o ampliación; el sitio no está certificado.",
    points: [
      "La presencia de etiquetas y landmarks no acredita conformidad WCAG.",
      "El formulario de contacto prepara un correo local; no transmite mensajes a un servidor del landing.",
      "La revisión manual con teclado, lector de pantalla y móvil sigue pendiente.",
    ],
    primaryLabel: "Volver al inicio",
    primaryHref: "/",
    secondaryLabel: "Estado de contacto",
    secondaryHref: "/contact",
  },
  security: {
    eyebrow: "SEGURIDAD Y CONFIANZA",
    title: "Conoce el alcance del contacto en reportes de pérdida.",
    lead: "El endpoint de contacto de un caso activo requiere autenticación y tiene rate limit; el código actual no lo restringe al propietario del caso.",
    detail:
      "La consulta de contacto de un reporte activo requiere sesión, pero cualquier cuenta autenticada puede obtener nombre y teléfono; este acceso requiere revisión de privacidad.",
    points: [
      "Una prueba de rate limit confirma frecuencia limitada, no privacidad por propietario.",
      "No afirmes que el teléfono se oculta o se transmite por relay.",
      "No se verificó entorno productivo, respuesta a incidentes ni control de abuso operativo.",
    ],
    primaryLabel: "Ver el flujo de recuperación",
    primaryHref: "/lost-pets",
    secondaryLabel: "Revisar privacidad",
    secondaryHref: "/privacy",
  },
};

export function generateStaticParams() {
  return Object.keys(pages).map((slug) => ({ slug }));
}

export const dynamicParams = false;

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const page = pages[slug];

  if (!page) return { title: "Página no encontrada" };

  return {
    title: page.eyebrow,
    description: page.lead,
    openGraph: {
      title: `${page.eyebrow} | NALA`,
      description: page.lead,
    },
  };
}

export default async function PublicPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const page = pages[slug];

  if (!page) notFound();

  const closingAction =
    slug === "plans"
      ? { label: "Ver capacidades para organizaciones", href: "/business" }
      : slug === "contact"
        ? { label: "Volver al inicio", href: "/" }
        : { label: "Abrir PawTrack", href: getProductUrl("/login") };

  return (
    <>
      <SiteHeader />
      <main className="inner-page" id="main">
        <div className="inner-page-top section-shell">
          <nav aria-label="Ruta de navegación" className="breadcrumbs">
            <Link href="/">Inicio</Link>
            <span aria-hidden="true">/</span>
            <span>{page.eyebrow}</span>
          </nav>
          <div className="inner-hero-grid">
            <div className="inner-hero-copy">
              <p className="eyebrow">
                <span /> {page.eyebrow}
              </p>
              <h1>{page.title}</h1>
              <p className="inner-lead">{page.lead}</p>
              <div className="inner-actions">
                <Link className="button button-dark" href={page.primaryHref}>
                  {page.primaryLabel}
                  <span aria-hidden="true">↗</span>
                </Link>
                <Link className="text-link" href={page.secondaryHref}>
                  {page.secondaryLabel}
                  <span aria-hidden="true">→</span>
                </Link>
              </div>
              {page.note ? (
                <p className="inline-notice" role="note">
                  {page.note}
                </p>
              ) : null}
            </div>
            <aside className="inner-aside">
              <span className="aside-index">NALA / {slug.toUpperCase()}</span>
              <p>{page.detail}</p>
              <span className="aside-mark" aria-hidden="true">
                N
              </span>
            </aside>
          </div>
        </div>
        {slug === "qr" ? (
          <div className="identity-feature-image section-shell">
            <LandingPicture
              alt="Una persona acerca su teléfono a la placa lisa del collar de su perro"
              asset="qr"
              className="identity-feature-photo"
            />
          </div>
        ) : null}
        {slug === "contact" ? <ContactEmailForm /> : null}
        <section className="inner-points section-shell" aria-label="Puntos importantes">
          <p className="eyebrow">LO ESENCIAL</p>
          {slug === "services" ? <ServiceCategoryGrid /> : null}
          {slug !== "services" ? (
            <div className="inner-point-grid">
              {page.points.map((point, index) => (
                <article className="depth-surface" data-3d-depth="module" data-depth-strength="2" key={point}>
                  <span>{String(index + 1).padStart(2, "0")}</span>
                  <p>{point}</p>
                </article>
              ))}
            </div>
          ) : null}
          {slug === "plans" ? <PublicPlansCatalog /> : null}
        </section>
        <section className="inner-bottom section-shell">
          <div>
            <p className="eyebrow">CAPACIDADES PAWTRACK</p>
            <h2>
              Identidad clara.
              <br />
              <em>Acción coordinada.</em>
            </h2>
          </div>
          {closingAction.href.startsWith("http") ? (
            <a className="button button-coral" href={closingAction.href}>
              {closingAction.label} <span aria-hidden="true">↗</span>
            </a>
          ) : (
            <Link className="button button-coral" href={closingAction.href}>
              {closingAction.label} <span aria-hidden="true">↗</span>
            </Link>
          )}
        </section>
      </main>
      <SiteFooter />
    </>
  );
}
