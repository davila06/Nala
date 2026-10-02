export const planDescriptions: Record<string, string> = {
  Free: "Alcance base técnico: hasta 1 mascota, perfil y QR. No tiene fila de precio propia en el catálogo local.",
  UserPlus:
    "Hasta 3 mascotas, QR e identidad digital; 3 casos perdidos simultáneos. El expediente médico completo corresponde a Familia.",
  UserFamilia: "Hasta 25 mascotas activas, miembros de familia (máximo 5) y expediente completo.",
  ClinicBasic: "Acceso base técnico de directorio y escaneo; no equivale a afiliación ni contrato clínico.",
  ClinicPlus: "Clínica: visibilidad, estadísticas, alertas y límite técnico de 500 escaneos por ciclo.",
  ClinicPartner:
    "Clínica: API, certificados e integraciones sujetas a grants, scopes y cuotas; operación externa no verificada.",
  StoreBasic: "Acceso base técnico para tienda; inventario, pagos y fulfillment no están acreditados.",
  StorePlus: "Tienda: catálogo y solicitudes de pedido; no incluye pago liquidado ni stock reservado.",
  StorePartner:
    "Tienda: límites técnicos superiores de catálogo y pedidos; multi-sede, pagos y operación transaccional no verificados.",
  ShelterBasic: "Acceso base técnico para perfiles y adopción; la operación de un refugio real no se deduce del tier.",
  ShelterPlus: "Refugio: publicación y flujos de adopción con límites técnicos; no procesa pagos ni acredita alianzas.",
  MuniBasica: "Tier municipal anual modelado para capturas y reportes; compra y renovación están incompletas.",
  MuniFull: "Tier municipal anual modelado con mayor volumen y herramientas de reporte; contratación no confirmada.",
  MuniRedRegional:
    "Tier anual modelado para alcance regional y transferencias entre cantones; integración oficial no verificada.",
};

export const planFeatures: Record<string, string[]> = {
  Free: [
    "Hasta 1 mascota",
    "Perfil público y QR",
    "Reportes esenciales de recuperación",
    "Sin collar GPS ni expediente médico completo",
  ],
  UserPlus: [
    "3 mascotas",
    "3 casos perdidos simultáneos",
    "Vista previa de 3 registros médicos",
    "Cuota técnica de 1 collar GPS",
  ],
  UserFamilia: [
    "Hasta 25 mascotas activas",
    "Hasta 5 integrantes",
    "Hasta 50 recordatorios",
    "Hasta 10 casos",
    "Cuota técnica de 5 collares GPS",
  ],
  ClinicPlus: ["Límite técnico de 500 escaneos/ciclo", "Visibilidad y badge", "Estadísticas clínicas"],
  ClinicPartner: [
    "Hasta 5.000 escaneos",
    "10 API keys",
    "Certificados sujetos a permisos y cuotas",
    "Hasta 25 veterinarios autorizados",
  ],
  StorePlus: ["Hasta 100 productos", "Hasta 250 solicitudes de pedido/ciclo", "Sin pago liquidado ni stock reservado"],
  StorePartner: [
    "Hasta 1.000 productos",
    "Hasta 2.500 pedidos/ciclo modelados",
    "Exportaciones técnicas; operación comercial no verificada",
  ],
  ShelterPlus: [
    "Hasta 500 animales adoptables modelados",
    "Publicación y solicitudes de adopción",
    "Ferias sujetas a límites técnicos",
  ],
  MuniBasica: [
    "Hasta 500 capturas/año modeladas",
    "Portal y reportes institucionales",
    "Compra/renovación incompletas",
  ],
  MuniFull: ["Hasta 5.000 capturas/año modeladas", "Lotes de 500", "Fotos y estadísticas según el tier"],
  MuniRedRegional: [
    "Hasta 30.000 capturas/año modeladas",
    "Lotes de 2.000",
    "Transferencias inter-cantón; convenio no verificado",
  ],
};
