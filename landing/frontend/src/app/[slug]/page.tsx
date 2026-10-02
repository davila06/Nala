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
  capabilityCards?: {
    title: string;
    status: string;
    statusTone: "ready" | "partial" | "unverified" | "unavailable";
    summary: string;
    limit: string;
  }[];
};

const lostPetSteps = [
  {
    title: "Prepara una descripción",
    description: "Ten a mano una foto reciente y describe rasgos visibles que ayuden a reconocer a tu mascota.",
    alt: "Una tutora prepara una descripción de su perro con una foto reciente.",
    asset: "lostPreparation",
  },
  {
    title: "Comparte una zona aproximada",
    description:
      "Anota cuándo y en qué zona la viste por última vez; evita publicar tu domicilio o coordenadas exactas.",
    alt: "Una tutora revisa una zona aproximada en un mapa sin direcciones legibles.",
    asset: "lostArea",
  },
  {
    title: "Revisa el contacto y cuida tu privacidad",
    description:
      "En casos activos, cualquier cuenta autenticada puede consultar el nombre y teléfono de contacto del reporte.",
    alt: "Una persona revisa el contacto y la privacidad de un reporte en su teléfono.",
    asset: "lostContact",
  },
] as const;

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
      "Para iniciar el reporte, accede a PawTrack y elige una mascota registrada. Revisa el alcance del contacto: cualquier cuenta autenticada puede consultar nombre y teléfono de un caso activo.",
    points: [
      "Reúne una foto reciente y una descripción con rasgos visibles para ayudar a reconocer a tu mascota.",
      "Indica cuándo y en qué zona aproximada la viste por última vez; evita publicar tu domicilio o coordenadas exactas.",
      "Revisa el contacto del reporte: cualquier cuenta autenticada puede consultar nombre y teléfono de un caso activo.",
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
    eyebrow: "PLANES PAWTRACK",
    title: "Tiers técnicos por hogar y organización.",
    lead: "Consulta inclusiones e importes desde el catálogo API del entorno conectado; las capacidades y límites varían según el tipo de cuenta.",
    detail:
      "El catálogo muestra los planes activos y aprobados en el entorno conectado. La aprobación local en PawTrackDev no confirma precio contractual, impuestos ni disponibilidad en producción.",
    points: [
      "Free es el alcance base técnico: hasta 1 mascota activa, 1 persona y QR. No tiene una fila propia ni precio publicado en el catálogo local.",
      "Para hogares, UserPlus admite hasta 3 mascotas y UserFamilia hasta 25 activas y 5 integrantes; el expediente médico completo corresponde a Familia.",
      "Clínicas, tiendas, refugios y municipalidades usan tiers distintos. No existe un tier técnico Premium; los tiers municipales tienen compra y renovación incompletas.",
      "Los límites del catálogo no garantizan enforcement en cada ruta. Las compras requieren verificación manual; no hay checkout recurrente universal, pago liquidado ni inventario garantizados.",
    ],
    primaryLabel: "Abrir PawTrack",
    primaryHref: getProductUrl("/login"),
    secondaryLabel: "Ver alcance para organizaciones",
    secondaryHref: "/business",
    note: "El catálogo representa el entorno consultado; límites técnicos no equivalen a oferta contractual ni disponibilidad en producción.",
  },
  services: {
    eyebrow: "SERVICIOS PARA MASCOTAS",
    title: "Servicios para cada etapa. Un ecosistema conectado.",
    lead: "NALA conecta a las familias con profesionales, comercios y organizaciones que acompañan el bienestar de sus mascotas mediante directorios y solicitudes en PawTrack.",
    detail:
      "Explora perfiles y servicios publicados. La publicación no confirma afiliación, certificación profesional, calidad, precio, cupo, horarios ni disponibilidad; verifica las condiciones directamente con cada prestador.",
    points: [
      "Profesionales: explora veterinarias, grooming, paseos, entrenamiento, hospedaje y cuidado temporal a partir de perfiles y servicios publicados.",
      "Comercios y adopción: PawTrack cuenta con módulos de tiendas, refugios y solicitudes de adopción; su existencia no confirma organizaciones ni mascotas disponibles ahora.",
      "Reservas: enviar una solicitud no confirma cupo ni pago liquidado; los pagos a prestadores no son automáticos.",
      "Emergencias y novedades: este directorio no es un servicio de emergencias ni garantiza atención inmediata. La telemedicina no está implementada; seguros y transporte especializado no están verificados.",
    ],
    primaryLabel: "Abrir buscador de servicios",
    primaryHref: getProductUrl("/servicios"),
    secondaryLabel: "Ver capacidades para organizaciones",
    secondaryHref: "/business",
    note: "Esta página explica el alcance; el directorio público y las solicitudes se gestionan en PawTrack.",
  },
  features: {
    eyebrow: "CAPACIDADES PAWTRACK",
    title: "Una plataforma para identificar, recuperar y cuidar mejor.",
    lead: "PawTrack CR reúne módulos de identidad, recuperación, salud y coordinación institucional. Cada capacidad conserva su estado real y sus límites.",
    detail:
      "Esta página resume módulos presentes en el código. No confirma que todas las funciones estén habilitadas, probadas de extremo a extremo o disponibles en producción.",
    points: [
      "Identidad/QR: el perfil público puede mostrar foto, nombre, especie y raza; un reporte de pérdida puede añadir nombre de contacto y mensaje público.",
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
      "El perfil muestra foto, nombre, especie y raza; una pérdida activa puede añadir nombre de contacto y mensaje público, no el teléfono ni correo del tutor.",
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
    title: "Registros clínicos con permiso del tutor.",
    lead: "PawTrack incluye escaneo QR/RFID, registros médicos con acceso autorizado y herramientas de agenda clínica. Afiliación y operación por clínica no están verificadas.",
    detail:
      "Un escaneo identifica a la mascota, pero no abre su expediente. La lectura o escritura clínica requiere un grant activo aprobado por el tutor.",
    points: [
      "La lectura y escritura del expediente requieren permisos activos aprobados por el tutor.",
      "Las consultas y citas se registran en PawTrack; no equivalen a telemedicina audiovisual.",
      "Afiliación, operación local e integración con sistemas externos no están verificadas.",
    ],
    capabilityCards: [
      {
        title: "Identificación por QR/RFID",
        status: "En el producto",
        statusTone: "ready",
        summary: "La ruta clínica acepta una URL QR o un identificador de chip y registra el escaneo.",
        limit: "El escaneo identifica a la mascota, pero no concede acceso a sus datos clínicos.",
      },
      {
        title: "Expediente con consentimiento",
        status: "En el producto",
        statusTone: "ready",
        summary:
          "Permite registrar vacunas, desparasitación, controles, cirugías, medicación y alergias, con documentos adjuntos.",
        limit:
          "La lectura y escritura requieren un grant activo aprobado por el tutor; no es un EHR externo ni diagnóstico.",
      },
      {
        title: "Agenda y registro de consulta",
        status: "En el producto",
        statusTone: "ready",
        summary: "Incluye agenda de citas veterinarias y registro estructurado de consultas clínicas.",
        limit: "No incluye consulta veterinaria por video ni audio.",
      },
      {
        title: "Estadísticas de escaneo",
        status: "Sujeto a plan",
        statusTone: "partial",
        summary: "El panel puede mostrar totales, coincidencias y desglose QR/RFID por día.",
        limit: "Requiere ClinicPlus o superior; no ofrece reportes de vacunas próximas ni pacientes frecuentes.",
      },
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
    capabilityCards: [
      {
        title: "Perfiles aliados",
        status: "En el producto",
        statusTone: "ready",
        summary: "El producto incluye perfiles de aliados y herramientas de gestión.",
        limit: "No confirma afiliación de una organización concreta.",
      },
      {
        title: "Publicaciones y adopción",
        status: "En el producto",
        statusTone: "ready",
        summary: "Hay superficies para publicar animales y gestionar solicitudes de adopción.",
        limit: "La operación de refugios, cobertura y respuesta no está verificada.",
      },
      {
        title: "Convenios y cobertura",
        status: "No verificado",
        statusTone: "unverified",
        summary: "La capacidad del producto no demuestra una alianza con una ONG.",
        limit: "No se acredita convenio, SLA ni cobertura operativa.",
      },
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
    capabilityCards: [
      {
        title: "Perfiles y capturas",
        status: "En el producto",
        statusTone: "ready",
        summary: "El módulo permite perfiles municipales y registro interno de capturas.",
        limit: "La existencia del módulo no confirma un despliegue municipal.",
      },
      {
        title: "Reportes institucionales",
        status: "En el producto",
        statusTone: "ready",
        summary: "Hay herramientas para consultar y preparar reportes institucionales internos.",
        limit: "Un reporte interno no equivale a una presentación oficial ante autoridades.",
      },
      {
        title: "Integración y convenios",
        status: "No verificado",
        statusTone: "unverified",
        summary: "El código no acredita una conexión oficial con una municipalidad o autoridad nacional.",
        limit: "Convenios, interoperabilidad, cobertura y SLA no están verificados.",
      },
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
    title: "Prepara un correo para PawTrack.",
    lead: "Completa el formulario y tu dispositivo abrirá una aplicación de correo con un borrador dirigido a soporte.",
    detail:
      "La dirección soporte@pawtrack.cr aparece documentada como contacto de soporte; esta landing no puede confirmar entrega, recepción ni tiempos de respuesta.",
    points: [
      "Para una pérdida o hallazgo, continúa en la app de PawTrack CR y usa los flujos operativos específicos.",
      "Para maltrato o un animal en riesgo, usa el reporte de bienestar; ante peligro inmediato contacta a las autoridades locales.",
      "No incluyas contraseñas, información clínica ni ubicación exacta en el correo.",
    ],
    primaryLabel: "Ver información para organizaciones",
    primaryHref: "/business",
    secondaryLabel: "Volver al inicio",
    secondaryHref: "/",
    note: "Al continuar se abrirá la aplicación de correo; la landing no recibe ni almacena el contenido.",
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
    capabilityCards: [
      {
        title: "Clínicas",
        status: "En el producto",
        statusTone: "ready",
        summary: "Expediente, permisos de acceso y herramientas administrativas clínicas.",
        limit: "Sin telemedicina audiovisual ni centros afiliados verificados.",
      },
      {
        title: "Refugios y adopción",
        status: "En el producto",
        statusTone: "ready",
        summary: "Perfiles aliados, publicaciones y solicitudes de adopción.",
        limit: "No acredita convenios, cobertura ni operación de una ONG concreta.",
      },
      {
        title: "Municipalidades",
        status: "En el producto",
        statusTone: "ready",
        summary: "Perfiles, capturas y reportes institucionales internos.",
        limit: "Integración oficial, convenio y despliegue municipal no verificados.",
      },
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
          <p className="eyebrow">{page.capabilityCards ? "CAPACIDADES Y LÍMITES" : "LO ESENCIAL"}</p>
          {slug === "services" ? <ServiceCategoryGrid /> : null}
          {slug === "lost-pets" ? (
            <div aria-label="Pasos para reportar una mascota perdida" className="lost-pet-step-grid">
              {lostPetSteps.map((step, index) => (
                <article className="lost-pet-step-card" key={step.title}>
                  <LandingPicture alt={step.alt} asset={step.asset} className="lost-pet-step-image" />
                  <div className="lost-pet-step-copy">
                    <span>{String(index + 1).padStart(2, "0")}</span>
                    <h3>{step.title}</h3>
                    <p>{step.description}</p>
                  </div>
                </article>
              ))}
            </div>
          ) : null}
          {page.capabilityCards ? (
            <div
              aria-label="Capacidades por organización"
              className={`capability-card-grid${page.capabilityCards.length === 4 ? " capability-card-grid-balanced" : ""}`}
            >
              {page.capabilityCards.map((card) => (
                <article className="capability-card" key={card.title}>
                  <span className={`capability-status status-${card.statusTone}`}>{card.status}</span>
                  <h3>{card.title}</h3>
                  <p>{card.summary}</p>
                  <p className="capability-limit">
                    <strong>Alcance:</strong> {card.limit}
                  </p>
                </article>
              ))}
            </div>
          ) : null}
          {slug !== "lost-pets" && !page.capabilityCards ? (
            <div className="inner-point-grid">
              {page.points.map((point, index) => (
                <article className="depth-surface" data-3d-depth="module" data-depth-strength="2" key={point}>
                  <span>{String(index + 1).padStart(2, "0")}</span>
                  <p>{point}</p>
                </article>
              ))}
            </div>
          ) : null}
          {slug === "plans" ? <PublicPlansCatalog showPrices={false} /> : null}
        </section>
        <section className="inner-bottom section-shell">
          <div>
            <p className="eyebrow">CAPACIDADES PAWTRACK</p>
            {slug === "services" ? (
              <>
                <p className="services-closing-copy">
                  Más que una plataforma de identificación, NALA conecta a las familias con los servicios, profesionales
                  y organizaciones que acompañan a sus mascotas durante toda su vida.
                </p>
                <h2>
                  <strong>Un perfil. Una comunidad. Un ecosistema completo.</strong>
                </h2>
              </>
            ) : (
              <h2>
                <strong>Identidad digital. Recuperación inteligente. Ecosistema conectado.</strong>
              </h2>
            )}
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
