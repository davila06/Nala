import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { SiteFooter, SiteHeader } from "@/components/site-chrome";

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
    title: "Una vida mejor y más segura para cada mascota.",
    lead: "NALA nace para conectar la identidad, el cuidado y las personas que ayudan a que una mascota vuelva a casa.",
    detail:
      "Nuestra visión empieza en Costa Rica y se construye con transparencia, respeto por los datos y colaboración local.",
    points: [
      "La recuperación no debería depender de poder pagar.",
      "Cada tecnología debe explicar con claridad qué hace y qué no.",
      "La confianza se gana con evidencia, consentimiento y atención responsable.",
    ],
    primaryLabel: "Conocer la propuesta",
    primaryHref: "/features",
    secondaryLabel: "Contactar a NALA",
    secondaryHref: "/contact",
  },
  "lost-pets": {
    eyebrow: "AYUDA PARA VOLVER A CASA",
    title: "Cuando se pierde, cada persona que sabe puede ayudar.",
    lead: "Una ruta sencilla para preparar un aviso, compartir información útil y mantener el contacto protegido.",
    detail:
      "La recuperación básica debe estar disponible sin pagar. No prometemos resultados ni mostramos datos personales por defecto.",
    points: [
      "Prepara una descripción que ayude a reconocer a tu mascota.",
      "Comparte una zona aproximada, no tu domicilio.",
      "Usa un canal seguro para recibir información y reportar estafas.",
    ],
    primaryLabel: "Continuar en NALA",
    primaryHref: "/lost-pets/report",
    secondaryLabel: "Encontré una mascota",
    secondaryHref: "/found-pets",
    note: "El reporte se completa en el portal de NALA; esta landing no recibe datos del caso ni envía alertas.",
  },
  "found-pets": {
    eyebrow: "GRACIAS POR AYUDAR",
    title: "Encontraste una mascota. El siguiente paso puede acercarla a casa.",
    lead: "Comparte información segura para que su familia pueda reconocerla y comunicarse contigo.",
    detail:
      "No publiques la ubicación exacta de forma abierta ni compartas códigos, datos bancarios o dinero con desconocidos.",
    points: [
      "Describe dónde la viste usando una zona general.",
      "Evita acercarte si el animal está asustado o herido.",
      "Si tiene una placa QR, escanéala sin divulgar sus datos públicamente.",
    ],
    primaryLabel: "Continuar en NALA",
    primaryHref: "/found-pets/report",
    secondaryLabel: "Ver mascotas perdidas",
    secondaryHref: "/lost-pets",
    note: "El reporte se completa en el portal de NALA; esta landing no recibe ubicación ni datos de contacto.",
  },
  plans: {
    eyebrow: "PLANES NALA",
    title: "El cuidado esencial debe estar al alcance de todos.",
    lead: "Estamos diseñando opciones claras para tutores, hogares y organizaciones, sin poner la recuperación detrás de un pago.",
    detail:
      "Los nombres y beneficios siguientes son una propuesta. Precios, límites y disponibilidad se definirán mediante investigación local.",
    points: [
      "Esencial: perfil digital y acceso a reportes básicos.",
      "Plus y Premium: organización y coordinación avanzada, sujetas a validación.",
      "Familiar y Empresa: permisos y herramientas según necesidades reales.",
    ],
    primaryLabel: "Crear perfil",
    primaryHref: "/register",
    secondaryLabel: "Consultar sobre planes",
    secondaryHref: "/contact?topic=planes",
    note: "Todavía no hay precios ni suscripciones disponibles para comprar.",
  },
  features: {
    eyebrow: "UNA IDENTIDAD, MUCHAS FORMAS DE CUIDAR",
    title: "La tecnología importa cuando hace más fácil estar presentes.",
    lead: "NALA está concebida como un punto de encuentro entre identificación, recuperación, cuidado preventivo y comunidad.",
    detail:
      "La disponibilidad de cada servicio dependerá de su implementación y de aliados habilitados en cada país.",
    points: [
      "Identidad digital y perfil de mascota configurable.",
      "Identificación mediante QR o NFC, complementaria al microchip.",
      "Historial, recordatorios y acceso a servicios locales cuando estén disponibles.",
    ],
    primaryLabel: "Crear perfil",
    primaryHref: "/register",
    secondaryLabel: "Ver las placas QR",
    secondaryHref: "/qr",
  },
  "pet-id": {
    eyebrow: "IDENTIDAD DIGITAL",
    title: "Que lo importante esté donde puede ayudar.",
    lead: "Un perfil digital de mascota puede reunir rasgos identificables, contactos elegidos e información útil para su cuidado.",
    detail:
      "El tutor debe poder revisar qué información ve una persona antes de compartir o activar un identificador.",
    points: [
      "Tú controlas qué datos aparecen públicamente.",
      "Un perfil QR/NFC se abre al escanear o acercar un teléfono.",
      "La identidad digital complementa, no reemplaza, el microchip ni la atención veterinaria.",
    ],
    primaryLabel: "Crear perfil",
    primaryHref: "/register",
    secondaryLabel: "Entender las placas QR",
    secondaryHref: "/qr",
  },
  qr: {
    eyebrow: "IDENTIFICACIÓN QR",
    title: "Una lectura puede abrir el camino de regreso.",
    lead: "Una placa QR puede enlazar con un perfil que el tutor mantiene actualizado y configura para compartir de forma segura.",
    detail:
      "El QR no transmite ubicación en tiempo real. Se necesita conexión para abrir un perfil en línea.",
    points: [
      "La persona escanea con la cámara de su teléfono.",
      "El tutor elige qué datos de contacto se muestran.",
      "La placa, materiales, compra y entrega aún no están disponibles en este prototipo.",
    ],
    primaryLabel: "Crear perfil",
    primaryHref: "/register",
    secondaryLabel: "Comparar QR y NFC",
    secondaryHref: "/nfc",
  },
  nfc: {
    eyebrow: "IDENTIFICACIÓN NFC",
    title: "Acercar el teléfono. Abrir un perfil seguro.",
    lead: "NFC permite que un dispositivo compatible abra un enlace al acercarlo a una etiqueta configurada.",
    detail:
      "La compatibilidad varía según el teléfono. Un código QR legible puede ser una alternativa útil.",
    points: [
      "NFC no es GPS ni informa una ubicación sin interacción.",
      "La configuración debe probarse en los dispositivos objetivo.",
      "El hardware y la activación todavía no están disponibles para compra.",
    ],
    primaryLabel: "Conocer el perfil digital",
    primaryHref: "/pet-id",
    secondaryLabel: "Ver identificación QR",
    secondaryHref: "/qr",
  },
  telemedicine: {
    eyebrow: "SALUD Y ORIENTACIÓN",
    title: "Atención veterinaria conectada, con profesionales reales.",
    lead: "La telemedicina es parte de la visión de NALA y solo debe ofrecerse donde haya profesionales habilitados y un servicio operativo.",
    detail:
      "Una consulta remota no es apropiada para todas las situaciones. Ante una emergencia, busca atención veterinaria presencial inmediata.",
    points: [
      "Verificación profesional según las reglas del país.",
      "Alcance, precio, horario y privacidad visibles antes de reservar.",
      "La inteligencia artificial no diagnostica ni reemplaza al veterinario.",
    ],
    primaryLabel: "Consultar disponibilidad",
    primaryHref: "/contact?topic=telemedicina",
    secondaryLabel: "Ver clínicas",
    secondaryHref: "/clinics",
    note: "Las consultas veterinarias no están disponibles desde este prototipo.",
  },
  marketplace: {
    eyebrow: "PRODUCTOS Y SERVICIOS",
    title: "Un espacio para encontrar lo que acompaña su bienestar.",
    lead: "La propuesta de marketplace conecta a tutores con proveedores y servicios locales, con información clara para comprar con confianza.",
    detail:
      "Antes de habilitar transacciones, cada vendedor, método de pago, entrega y política de devolución debe estar verificado.",
    points: [
      "Precios, disponibilidad y costos de envío transparentes.",
      "Vendedores y servicios con alcance claramente identificado.",
      "No hay productos ni pagos activos en este prototipo.",
    ],
    primaryLabel: "Proponer un proveedor",
    primaryHref: "/contact?topic=marketplace",
    secondaryLabel: "Ver identificación QR",
    secondaryHref: "/qr",
  },
  community: {
    eyebrow: "COMUNIDAD",
    title: "Una red se construye con ayuda que sí llega.",
    lead: "La comunidad NALA está pensada para conectar tutores, personas que encuentran mascotas y organizaciones locales.",
    detail:
      "La participación necesita moderación, controles contra estafas y privacidad por defecto.",
    points: [
      "Comparte alertas con información útil y limitada.",
      "Reporta contenido abusivo o sospechoso.",
      "Las funciones comunitarias todavía no están activas.",
    ],
    primaryLabel: "Ver flujo de mascotas perdidas",
    primaryHref: "/lost-pets",
    secondaryLabel: "Contactar a NALA",
    secondaryHref: "/contact",
  },
  clinics: {
    eyebrow: "PARA CLÍNICAS VETERINARIAS",
    title: "Más continuidad entre una consulta y el cuidado diario.",
    lead: "NALA propone herramientas para conectar clínicas con perfiles, documentos y familias, siempre con autorización del tutor.",
    detail:
      "El directorio y cualquier servicio clínico requieren verificar la información profesional y la disponibilidad por jurisdicción.",
    points: [
      "Perfiles de clínica con datos vigentes y alcance claro.",
      "Acceso a documentos solo mediante permiso explícito.",
      "Pilotos, integraciones y telemedicina todavía no están activos.",
    ],
    primaryLabel: "Conversar sobre un piloto",
    primaryHref: "/contact?topic=clinicas",
    secondaryLabel: "Conocer la visión de NALA",
    secondaryHref: "/about",
  },
  shelters: {
    eyebrow: "PARA REFUGIOS Y ONGs",
    title: "Cada animal merece una historia clara y un próximo hogar.",
    lead: "La visión para organizaciones conecta perfiles, información de cuidado, reportes y procesos de adopción con los permisos adecuados.",
    detail:
      "NALA no presenta organizaciones como aliadas hasta verificar y acordar formalmente una relación.",
    points: [
      "Herramientas para organizar información de cada animal.",
      "Publicación y coordinación con autorización institucional.",
      "Aún no hay refugios u ONGs afiliados en este prototipo.",
    ],
    primaryLabel: "Explorar una colaboración",
    primaryHref: "/contact?topic=refugios",
    secondaryLabel: "Ver mascotas encontradas",
    secondaryHref: "/found-pets",
  },
  municipalities: {
    eyebrow: "PARA MUNICIPALIDADES",
    title: "Mejores datos para coordinar el bienestar animal local.",
    lead: "NALA explora flujos para gestión de reportes, adopción y coordinación entre instituciones, con métricas agregadas y protección de datos.",
    detail:
      "Cada despliegue requerirá evaluación legal, responsable local, alcance, soporte y acuerdos de manejo de información.",
    points: [
      "Vistas agregadas, no exposición de domicilios o datos personales.",
      "Procesos y roles definidos junto al gobierno local.",
      "No hay convenios municipales activos en este prototipo.",
    ],
    primaryLabel: "Solicitar conversación",
    primaryHref: "/contact?topic=municipalidades",
    secondaryLabel: "Conocer el enfoque de confianza",
    secondaryHref: "/about",
  },
  blog: {
    eyebrow: "RECURSOS",
    title: "Información práctica para cuidar y ayudar mejor.",
    lead: "La biblioteca editorial de NALA cubrirá identificación, pérdida y hallazgo, cuidado preventivo y adopción responsable.",
    detail:
      "Los contenidos de salud deben ser revisados por profesionales y adaptados a las recomendaciones de cada país.",
    points: [
      "Guías de preparación para una búsqueda de mascota perdida.",
      "Diferencias entre QR, NFC, microchip y GPS.",
      "La biblioteca de artículos todavía está en preparación.",
    ],
    primaryLabel: "Ver el flujo de recuperación",
    primaryHref: "/lost-pets",
    secondaryLabel: "Contactar a NALA",
    secondaryHref: "/contact",
  },
  contact: {
    eyebrow: "HABLEMOS",
    title: "Construyamos una mejor forma de cuidarles.",
    lead: "Cuéntanos si eres tutor, clínica, refugio, ONG, municipalidad o proveedor y qué problema necesitas resolver.",
    detail:
      "Este portal es un prototipo y todavía no procesa mensajes. No incluyas información personal o datos de salud en formularios de prueba.",
    points: [
      "Tutores: comparte necesidades y prioridades.",
      "Organizaciones: describe brevemente tu contexto y país.",
      "No uses este contacto para una emergencia veterinaria o una mascota perdida activa.",
    ],
    primaryLabel: "Explorar soluciones para organizaciones",
    primaryHref: "/business",
    secondaryLabel: "Volver al inicio",
    secondaryHref: "/",
    note: "Los canales de soporte se publicarán cuando estén habilitados.",
  },
  business: {
    eyebrow: "NALA PARA ORGANIZACIONES",
    title: "Conecta a quienes hacen posible el bienestar animal.",
    lead: "Clínicas, refugios, ONGs, municipalidades y empresas tienen necesidades distintas. NALA debe resolverlas con flujos y acuerdos claros.",
    detail:
      "Selecciona un contexto para revisar la propuesta; todavía no se ofrecen demos ni pilotos activos.",
    points: [
      "Clínicas: continuidad de cuidado y perfiles autorizados.",
      "Refugios: información y coordinación de adopción.",
      "Municipalidades y empresas: datos agregados e integraciones responsables.",
    ],
    primaryLabel: "Ver propuesta para clínicas",
    primaryHref: "/clinics",
    secondaryLabel: "Ver propuesta para refugios",
    secondaryHref: "/shelters",
  },
  privacy: {
    eyebrow: "PRIVACIDAD",
    title: "Tus datos y los de tu mascota merecen cuidado.",
    lead: "La privacidad es un requisito de diseño: recopilar lo mínimo, explicar los usos y dar control sobre lo que se comparte.",
    detail:
      "Esta página explica principios del prototipo, no es una política legal definitiva. Se publicará una política revisada por jurisdicción antes de recopilar datos.",
    points: [
      "No compartas información personal a través de esta landing.",
      "Las ubicaciones públicas deben ser aproximadas.",
      "Consulta los avisos del producto antes de compartir información en sus flujos.",
    ],
    primaryLabel: "Volver al inicio",
    primaryHref: "/",
    secondaryLabel: "Contactar a NALA",
    secondaryHref: "/contact",
  },
  accessibility: {
    eyebrow: "ACCESIBILIDAD",
    title: "La ayuda debe ser accesible para todas las personas.",
    lead: "Estamos diseñando la experiencia con navegación por teclado, texto legible, estados explícitos y respeto a las preferencias de movimiento.",
    detail:
      "Este prototipo aún necesita auditorías manuales y pruebas con personas usuarias antes de afirmar conformidad WCAG.",
    points: [
      "Usa Tab para recorrer navegación y controles.",
      "Los formularios muestran etiquetas y errores asociados.",
      "Contáctanos si una barrera te impide completar una tarea.",
    ],
    primaryLabel: "Volver al inicio",
    primaryHref: "/",
    secondaryLabel: "Contactar a NALA",
    secondaryHref: "/contact",
  },
  security: {
    eyebrow: "SEGURIDAD Y CONFIANZA",
    title: "La seguridad se explica. No se promete en abstracto.",
    lead: "Una experiencia de recuperación debe prevenir exposición de datos, suplantación, estafas y abuso de contacto.",
    detail:
      "Los controles de producción, la atención de incidentes y los canales de reporte deben estar operativos antes de aceptar casos reales.",
    points: [
      "Contactos mediados en lugar de teléfonos expuestos.",
      "Ubicación aproximada en páginas públicas.",
      "Este prototipo no recibe reportes reales ni credenciales.",
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

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
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

export default async function PublicPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const page = pages[slug];

  if (!page) notFound();

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
        <section
          className="inner-points section-shell"
          aria-label="Puntos importantes"
        >
          <p className="eyebrow">LO ESENCIAL</p>
          <div className="inner-point-grid">
            {page.points.map((point, index) => (
              <article key={point}>
                <span>{String(index + 1).padStart(2, "0")}</span>
                <p>{point}</p>
              </article>
            ))}
          </div>
          {slug === "plans" ? (
            <div className="tier-grid" aria-label="Planes en evaluación">
              {[
                ["Esencial", "Identidad y acceso básico"],
                ["Plus", "Organización del cuidado"],
                ["Premium", "Herramientas avanzadas"],
                ["Familiar", "Coordinación entre tutores"],
              ].map(([name, description]) => (
                <article className="tier-item" key={name}>
                  <span>EN DEFINICIÓN</span>
                  <h2>{name}</h2>
                  <p>{description}</p>
                </article>
              ))}
            </div>
          ) : null}
        </section>
        <section className="inner-bottom section-shell">
          <div>
            <p className="eyebrow">UNA PLATAFORMA EN CONSTRUCCIÓN</p>
            <h2>
              Primero, lo que ayuda.
              <br />
              <em>Después, lo que escala.</em>
            </h2>
          </div>
          <Link className="button button-coral" href="/contact">
            Conversemos <span aria-hidden="true">↗</span>
          </Link>
        </section>
      </main>
      <SiteFooter />
    </>
  );
}
