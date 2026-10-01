export const planDescriptions: Record<string, string> = {
  UserPlus: "Hasta 3 mascotas, QR e identidad digital; las capacidades adicionales dependen de sus gates técnicos.",
  UserFamilia: "Hasta 25 mascotas activas, miembros de familia y expediente ampliado.",
  ClinicPlus: "Visibilidad, badge, estadísticas y alertas para clínicas.",
  ClinicPartner: "Integraciones, API, widget y certificados sujetos a grants, scopes y cuotas.",
  StorePlus: "Catálogo, directorio y solicitudes de pedido para tiendas.",
  StorePartner:
    "Capacidades avanzadas de tienda; la operación multi-sede y pagos externos no están verificados como oferta.",
  ShelterPlus: "Publicación y funciones avanzadas de adopción para refugios.",
  MuniBasica: "Tier municipal anual modelado para capacidades básicas de captura y reportes.",
  MuniFull: "Tier municipal anual modelado para capturas ampliadas y reportes institucionales.",
  MuniRedRegional: "Tier anual modelado para redes regionales, transferencias y reportes inter-cantón.",
};

export const planFeatures: Record<string, string[]> = {
  UserPlus: [
    "3 mascotas",
    "3 casos perdidos simultáneos",
    "10 matching/ciclo",
    "1 collar GPS",
    "Preview médico de 3 registros",
  ],
  UserFamilia: ["25 mascotas activas", "Hasta 5 miembros", "50 recordatorios", "10 casos perdidos", "5 collares GPS"],
  ClinicPlus: ["500 escaneos/ciclo", "Visibilidad y badge", "Estadísticas", "Alertas clínicas"],
  ClinicPartner: ["5.000 escaneos", "10 API keys", "500 certificados", "250 pasaportes", "25 veterinarios autorizados"],
  StorePlus: ["100 productos", "250 pedidos/ciclo", "Catálogo", "Directorio y solicitudes"],
  StorePartner: ["1.000 productos", "2.500 pedidos/ciclo", "5 sedes técnicas", "20 exportaciones/ciclo"],
  ShelterPlus: ["500 animales", "Publicación de adopciones", "Funciones avanzadas", "24 ferias/año modeladas"],
  MuniBasica: ["500 capturas/año", "Portal municipal básico", "Reportes institucionales"],
  MuniFull: ["5.000 capturas/año", "Lotes de 500", "Fotos y estadísticas", "Panel municipal"],
  MuniRedRegional: ["30.000 capturas/año", "Lotes de 2.000", "Dashboard regional", "Transferencias inter-cantón"],
};
