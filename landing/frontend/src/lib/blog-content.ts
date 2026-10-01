export type EditorialArticle = {
  slug: string;
  category: string;
  title: string;
  summary: string;
  sections: { heading: string; paragraphs: string[] }[];
  relatedHref: string;
  relatedLabel: string;
};

export const editorialPillars = [
  {
    id: "identificacion-privacidad",
    title: "Identificación y privacidad",
    description: "Perfiles útiles, tecnologías y decisiones informadas sobre qué compartir.",
    categories: ["IDENTIFICACIÓN", "IDENTIDAD DIGITAL", "PRIVACIDAD"],
  },
  {
    id: "perdida-recuperacion",
    title: "Pérdida, hallazgo y seguridad",
    description: "Preparación, comunicación segura y pasos cuidadosos para una reunificación.",
    categories: ["RECUPERACIÓN", "PREVENCIÓN", "SEGURIDAD"],
  },
  {
    id: "cuidado-vida-diaria",
    title: "Cuidado y vida diaria",
    description: "Convivencia, viajes, adopción y coordinación alrededor del bienestar animal.",
    categories: [
      "CUIDADO",
      "CUIDADO COMPARTIDO",
      "ADOPCIÓN",
      "VIAJES",
      "BIENESTAR",
      "PREPARACIÓN",
      "CONVIVENCIA",
      "COMUNIDAD",
    ],
  },
];

export const editorialArticles: EditorialArticle[] = [
  {
    slug: "qr-nfc-microchip-gps",
    category: "IDENTIFICACIÓN",
    title: "QR, NFC, microchip y GPS: qué hace cada tecnología",
    summary:
      "No todas las formas de identificación hacen lo mismo. Conocer sus límites ayuda a elegir y combinarlas mejor.",
    sections: [
      {
        heading: "QR y NFC abren información cuando alguien interactúa",
        paragraphs: [
          "Un código QR se lee con la cámara; una etiqueta NFC se detecta al acercar un teléfono compatible. Ambos pueden abrir un enlace a un perfil, pero no localizan a una mascota por sí solos.",
          "Un perfil útil debe mostrar únicamente la información que su tutor decidió compartir y explicar cómo contactar de forma segura.",
        ],
      },
      {
        heading: "El microchip identifica; no transmite ubicación",
        paragraphs: [
          "El microchip se consulta con un lector compatible y puede ayudar a asociar un animal con un registro. No funciona como un GPS ni se lee con la cámara de un teléfono.",
          "El proceso de registro y consulta puede variar según el país y el proveedor. Conviene confirmar cómo mantener actualizados los datos asociados.",
        ],
      },
      {
        heading: "GPS depende de un dispositivo y su servicio",
        paragraphs: [
          "Un localizador GPS requiere un dispositivo que obtenga y comparta ubicación, además de batería y conectividad. Revisa cobertura, autonomía, costos y condiciones antes de elegir uno.",
          "Ninguna tecnología garantiza por sí sola una reunificación. Una identificación visible y datos de contacto actualizados pueden complementar otras medidas.",
        ],
      },
    ],
    relatedHref: "/qr",
    relatedLabel: "Conocer la propuesta de identificación QR",
  },
  {
    slug: "perfil-mascota-privacidad",
    category: "PRIVACIDAD",
    title: "Qué información incluir en el perfil de tu mascota",
    summary: "Un perfil claro ayuda a reconocerla sin exponer datos personales que no hacen falta para pedir ayuda.",
    sections: [
      {
        heading: "Prioriza lo que permite reconocerla",
        paragraphs: [
          "Una foto reciente, el nombre que reconoce, la especie, rasgos visibles y una descripción breve pueden orientar a quien la encuentre. Mantén los datos al día si cambian su aspecto o sus necesidades.",
          "Si decides incluir información de cuidado, comparte solo lo necesario y evita publicar detalles sensibles que no ayuden a identificarla.",
        ],
      },
      {
        heading: "Elige con cuidado cómo pueden contactarte",
        paragraphs: [
          "Antes de hacer público un teléfono, correo o domicilio, considera si existe una alternativa de contacto mediado. No publiques tu dirección exacta, contraseñas, códigos de verificación ni información financiera.",
          "Este portal no recoge datos de mascotas. Crea y administra el perfil en el portal de NALA.",
        ],
      },
      {
        heading: "Revisa lo que ve otra persona",
        paragraphs: [
          "Antes de compartir un identificador, comprueba qué información abre y con quién se comparte. La ubicación pública, si se usa en un reporte, debería ser aproximada y no revelar tu casa.",
        ],
      },
    ],
    relatedHref: "/pet-id",
    relatedLabel: "Explorar la identidad digital",
  },
  {
    slug: "encontraste-una-mascota",
    category: "RECUPERACIÓN",
    title: "Encontraste una mascota: pasos para ayudar con cuidado",
    summary:
      "Una respuesta tranquila puede protegerte y facilitar que su familia la reconozca, sin difundir información de más.",
    sections: [
      {
        heading: "Primero, evalúa el entorno",
        paragraphs: [
          "Cuida tu seguridad y la del animal. Si está asustado, herido o en una zona peligrosa, evita exponerte o intentar manipularlo si no es seguro; contacta servicios locales que puedan orientarte.",
          "Si una placa ofrece un QR o NFC, puedes revisar el enlace antes de compartir cualquier dato. No entregues dinero ni códigos de verificación a personas desconocidas.",
        ],
      },
      {
        heading: "Comparte lo necesario, no todos los detalles",
        paragraphs: [
          "Una descripción y una zona aproximada ayudan a difundir el hallazgo. Reserva algún rasgo distintivo para comprobar que quien responde conoce realmente a la mascota.",
          "Evita publicar tu domicilio, teléfono personal si no quieres hacerlo público, o el punto exacto donde permanece el animal.",
        ],
      },
      {
        heading: "Coordina el siguiente paso con cuidado",
        paragraphs: [
          "Acuerda un lugar seguro para reunirse y, ante dudas sobre la salud del animal, busca orientación veterinaria profesional. Las reglas y recursos disponibles dependen de cada localidad.",
          "El reporte de hallazgo se completa en el portal de NALA; esta landing no recoge ubicación ni datos de contacto.",
        ],
      },
    ],
    relatedHref: "/found-pets",
    relatedLabel: "Ver la propuesta para mascotas encontradas",
  },
  {
    slug: "preparar-plan-mascota-perdida",
    category: "PREVENCIÓN",
    title: "Prepara una ficha antes de necesitarla",
    summary:
      "Tener a mano una foto reciente, una descripción y contactos de confianza facilita actuar con calma si tu mascota se pierde.",
    sections: [
      {
        heading: "Guarda información que ayude a reconocerla",
        paragraphs: [
          "Conserva una foto reciente y anota sus rasgos visibles, señas particulares y los datos de identificación que ya tenga. Actualiza la ficha cuando cambie su apariencia.",
          "No es necesario incluir tu domicilio ni publicar documentos personales para preparar esta información.",
        ],
      },
      {
        heading: "Define a quién llamar y cómo coordinar",
        paragraphs: [
          "Acuerda con una persona de confianza quién puede ayudar y qué canal usar para mantenerse en contacto. Revisa que teléfonos y correos sigan vigentes.",
          "Piensa de antemano en un lugar seguro para coordinar una reunificación, sin compartir una dirección privada en publicaciones abiertas.",
        ],
      },
      {
        heading: "Conoce los límites de cada identificador",
        paragraphs: [
          "Una placa QR o NFC puede abrir información cuando alguien la escanea o acerca un teléfono compatible; no muestra por sí sola dónde está tu mascota.",
          "El perfil de NALA se administra desde el portal del producto. Esta guía no sustituye los servicios locales de búsqueda.",
        ],
      },
    ],
    relatedHref: "/lost-pets",
    relatedLabel: "Conocer el flujo de recuperación",
  },
  {
    slug: "alerta-mascota-perdida-segura",
    category: "RECUPERACIÓN",
    title: "Una alerta de mascota perdida: qué compartir",
    summary:
      "Una descripción útil y una zona aproximada pueden orientar a la comunidad sin poner en riesgo tu privacidad.",
    sections: [
      {
        heading: "Incluye datos que ayuden a identificarla",
        paragraphs: [
          "Añade una foto reciente, tamaño aproximado, colores y rasgos visibles. Indica la última zona donde la viste de manera general, no una dirección exacta.",
          "Reserva un detalle distintivo para confirmar con cuidado que quien responda conoce a la mascota.",
        ],
      },
      {
        heading: "Protege tus canales de contacto",
        paragraphs: [
          "Elige un canal que puedas revisar y evita publicar datos financieros, contraseñas o códigos de verificación. Desconfía de solicitudes de dinero para devolverla.",
          "Si coordinas un encuentro, acuerda un espacio público y avisa a alguien de confianza.",
        ],
      },
      {
        heading: "Mantén la información clara",
        paragraphs: [
          "Si compartes una actualización, corrige datos antiguos y avisa cuando la búsqueda termine para reducir confusiones.",
          "Los reportes reales se gestionan en el portal del producto; usa también los canales locales activos para una búsqueda.",
        ],
      },
    ],
    relatedHref: "/lost-pets/report",
    relatedLabel: "Continuar en NALA",
  },
  {
    slug: "elegir-placa-qr-nfc",
    category: "IDENTIFICACIÓN",
    title: "Una placa QR o NFC: qué revisar antes de elegir",
    summary: "Compatibilidad, legibilidad y control sobre el perfil importan más que las promesas de una etiqueta.",
    sections: [
      {
        heading: "Comprueba cómo se abre el perfil",
        paragraphs: [
          "Un QR requiere que una persona lo escanee con la cámara; NFC requiere un teléfono compatible que pueda leer la etiqueta al acercarlo. La experiencia puede variar según el dispositivo.",
          "Pregunta si abrir el perfil requiere una aplicación o una cuenta. En el caso de NALA, los identificadores y placas aún no están disponibles para compra.",
        ],
      },
      {
        heading: "Revisa qué información se comparte",
        paragraphs: [
          "Antes de activar un enlace, confirma qué datos verá quien lo abra y cómo puedes actualizarlos. Evita hacer visible una dirección residencial o información que no sea necesaria.",
          "Si el proveedor ofrece contacto mediado, revisa cómo funciona y qué datos recopila antes de usarlo.",
        ],
      },
      {
        heading: "No confundas identificación con rastreo",
        paragraphs: [
          "Una etiqueta QR o NFC no transmite ubicación en tiempo real. Un localizador GPS es otro tipo de dispositivo y depende de su batería, conectividad y servicio.",
          "La identificación digital puede complementar otras medidas, pero no garantiza que una mascota sea encontrada.",
        ],
      },
    ],
    relatedHref: "/qr",
    relatedLabel: "Leer sobre identificación QR",
  },
  {
    slug: "mantener-perfil-mascota-actualizado",
    category: "IDENTIDAD DIGITAL",
    title: "Cómo mantener útil el perfil digital de tu mascota",
    summary:
      "Una revisión sencilla de fotos, rasgos y contactos evita que una ficha quede desactualizada cuando alguien necesita consultarla.",
    sections: [
      {
        heading: "Actualiza los datos cuando algo cambie",
        paragraphs: [
          "Revisa la foto, la descripción y los contactos después de un cambio importante. Si una persona deja de ser contacto de confianza, actualiza la información que compartes.",
          "La app de PawTrack CR incluye recordatorios de salud. Este landing no los configura ni confirma su entrega en producción; consulta la app y a un profesional veterinario.",
        ],
      },
      {
        heading: "Comparte solo lo que hace falta",
        paragraphs: [
          "Los rasgos visibles y un canal de contacto elegido pueden ayudar a identificarla. Guarda la dirección de casa y otros datos sensibles fuera del perfil público.",
          "Comprueba el perfil desde la perspectiva de otra persona antes de colocar un identificador en el collar.",
        ],
      },
      {
        heading: "Ten presente el estado del servicio",
        paragraphs: [
          "Antes de depender de un perfil, verifica que el enlace abra y que el servicio esté disponible. La creación del perfil se realiza en el portal de NALA.",
        ],
      },
    ],
    relatedHref: "/pet-id",
    relatedLabel: "Explorar el perfil digital propuesto",
  },
  {
    slug: "organizar-documentos-cuidado-mascota",
    category: "CUIDADO",
    title: "Organiza la información de cuidado para compartirla mejor",
    summary:
      "Tener documentos y datos relevantes reunidos puede facilitar una conversación con tu clínica veterinaria o con quien cuida de tu mascota.",
    sections: [
      {
        heading: "Reúne documentos que ya te entregaron",
        paragraphs: [
          "Puedes mantener juntos los documentos de identificación, constancias y las instrucciones que te haya dado un profesional veterinario. Anota quién los emitió y cuándo, si esa información aparece en el documento.",
          "No cambies tratamientos ni dosis basándote en una ficha digital; confirma dudas con un profesional que conozca el caso.",
        ],
      },
      {
        heading: "Controla con quién los compartes",
        paragraphs: [
          "Los documentos pueden incluir información sensible. Comparte solo lo pertinente, con personas de confianza y mediante un canal adecuado.",
          "La app incluye registros y documentos de cuidado. Este artículo no es una historia clínica, no interpreta síntomas y no confirma disponibilidad de producción.",
        ],
      },
      {
        heading: "Usa la información como apoyo",
        paragraphs: [
          "Una ficha ayuda a organizar datos, pero no interpreta síntomas ni reemplaza una consulta. Para preocupaciones de salud, contacta a un profesional veterinario.",
        ],
      },
    ],
    relatedHref: "/features",
    relatedLabel: "Conocer las funciones propuestas",
  },
  {
    slug: "coordinar-cuidado-personas-confianza",
    category: "CUIDADO COMPARTIDO",
    title: "Coordina el cuidado de tu mascota con personas de confianza",
    summary:
      "Acordar responsabilidades y contactos por adelantado ayuda a que los cambios de rutina sean más claros para todos.",
    sections: [
      {
        heading: "Acuerden quién hace qué",
        paragraphs: [
          "Hablen sobre horarios, alimentación indicada por su tutor y a quién contactar si surge una duda. Deja por escrito las instrucciones importantes con lenguaje claro.",
          "Si hay una decisión de salud, acuerden contactar a la persona tutora y al profesional veterinario correspondiente.",
        ],
      },
      {
        heading: "Comparte lo necesario para cada rol",
        paragraphs: [
          "Una persona que cuida por unas horas quizá solo necesite un contacto y la información práctica de esa jornada. Limita el acceso a otros datos personales.",
          "Pide permiso antes de compartir documentos, fotos o información de contacto de otra persona.",
        ],
      },
      {
        heading: "No dependas de una función que aún no existe",
        paragraphs: [
          "La app de PawTrack CR incluye gestión de invitaciones familiares. Revisa los permisos y el estado de las invitaciones en la app; este landing no administra accesos.",
        ],
      },
    ],
    relatedHref: "/features",
    relatedLabel: "Ver la visión de NALA",
  },
  {
    slug: "identidad-digital-mascota-adoptada",
    category: "ADOPCIÓN",
    title: "Adoptar también es crear una nueva identidad digital",
    summary:
      "Una ficha actualizada puede acompañar la transición y reunir la información que la nueva familia elige conservar.",
    sections: [
      {
        heading: "Empieza con la información confirmada",
        paragraphs: [
          "Pregunta qué datos están verificados y qué documentos pueden compartirse. Si algo no está claro, anótalo como pendiente en vez de asumirlo.",
          "Actualiza el nombre de uso, las fotos y los contactos según los acuerdos de adopción y las preferencias de la familia.",
        ],
      },
      {
        heading: "Protege los datos de todas las personas",
        paragraphs: [
          "Antes de reutilizar un perfil o identificador, confirma quién puede administrar la información y elimina del uso público los contactos anteriores que ya no correspondan.",
          "No publiques documentos con domicilios, firmas o información personal de quienes participaron en la adopción.",
        ],
      },
      {
        heading: "Consulta los requisitos de tu localidad",
        paragraphs: [
          "Los procesos y documentos de adopción varían entre organizaciones y lugares. La app contiene flujos de adopción, pero no se ha verificado afiliación ni operación de una organización concreta.",
        ],
      },
    ],
    relatedHref: "/shelters",
    relatedLabel: "Conocer la propuesta para refugios y ONGs",
  },
  {
    slug: "viajar-con-mascota-latinoamerica",
    category: "VIAJES",
    title: "Viajar con tu mascota por Latinoamérica: planifica cada tramo",
    summary:
      "Los requisitos cambian entre países, transportes y destinos. Una buena preparación empieza por verificar fuentes oficiales para tu ruta concreta.",
    sections: [
      {
        heading: "Confirma las reglas para origen, destino y escalas",
        paragraphs: [
          "Antes de comprar o reservar, consulta a las autoridades competentes del país de salida y llegada, además de la empresa de transporte. Los requisitos pueden cambiar y variar por especie, edad, ruta y modalidad de viaje.",
          "Si hay escalas o cruces terrestres, revisa también las condiciones de cada tramo. No des por hecho que los documentos aceptados en un viaje anterior sirven para otro.",
        ],
      },
      {
        heading: "Habla con un profesional con tiempo",
        paragraphs: [
          "Consulta a un profesional veterinario sobre la condición y las necesidades de tu mascota para ese viaje. Pregunta qué documentación debe emitir y con cuánta anticipación, porque los plazos dependen de cada destino.",
          "Verifica que el nombre y los datos de identificación coincidan en los documentos que te soliciten. NALA no emite certificados ni determina requisitos de ingreso.",
        ],
      },
      {
        heading: "Revisa las condiciones del transporte",
        paragraphs: [
          "Confirma directamente con aerolíneas, empresas de autobús o transporte terrestre si aceptan animales, qué transportador piden, dónde viajaría la mascota y cuáles son las restricciones aplicables.",
          "No compres un transportador basándote solo en consejos generales: mide a tu mascota y valida las especificaciones del operador que usarás.",
        ],
      },
      {
        heading: "Lleva un plan alternativo y los contactos necesarios",
        paragraphs: [
          "Guarda copias accesibles de los documentos, contactos de emergencia y la dirección de tu alojamiento. Prepara una alternativa si una conexión o reserva cambia.",
          "Las condiciones locales y los servicios disponibles pueden variar mucho dentro de Latinoamérica. Confirma la información cerca de la fecha de salida con las fuentes oficiales correspondientes.",
        ],
      },
    ],
    relatedHref: "/pet-id",
    relatedLabel: "Explorar la identidad digital",
  },
  {
    slug: "mudanza-con-mascota",
    category: "BIENESTAR",
    title: "Mudarte con una mascota: prepara el cambio de hogar",
    summary:
      "Una mudanza implica nuevas puertas, sonidos, recorridos y rutinas. Preparar la transición puede hacerla más ordenada para toda la familia.",
    sections: [
      {
        heading: "Prepara el traslado antes del día de mudanza",
        paragraphs: [
          "Confirma cómo se transportará la mascota y quién estará a cargo mientras se mueven cajas y muebles. Asegura puertas y accesos para reducir el riesgo de que salga sin supervisión.",
          "Mantén a mano su identificación, correa o transportador habitual y los contactos de confianza. Si el trayecto es largo, consulta a un profesional veterinario sobre sus necesidades particulares.",
        ],
      },
      {
        heading: "Revisa el nuevo entorno",
        paragraphs: [
          "Antes de permitir que explore, comprueba accesos, ventanas, balcones y lugares donde podría quedar atrapada. Identifica un espacio tranquilo con sus objetos familiares.",
          "Si vives en un edificio o residencia con reglas para animales, confirma las normas directamente con la administración y conserva los contactos relevantes.",
        ],
      },
      {
        heading: "Actualiza la identificación y los contactos",
        paragraphs: [
          "Cuando el cambio sea definitivo, revisa los datos asociados a sus identificadores y a las personas de confianza. No publiques tu nueva dirección en un perfil abierto.",
          "Los cambios del perfil se gestionan desde el portal de NALA; mantén actualizados los canales de contacto que hayas elegido compartir.",
        ],
      },
      {
        heading: "Dale tiempo para adaptarse",
        paragraphs: [
          "Mantén, en la medida posible, horarios y señales conocidas mientras la familia se instala. Observa cómo responde al nuevo ambiente sin forzar interacciones.",
          "Si tienes dudas sobre cambios de conducta o bienestar, pide orientación a un profesional que pueda evaluar la situación. Esta guía no ofrece diagnóstico.",
        ],
      },
    ],
    relatedHref: "/pet-id",
    relatedLabel: "Revisar qué incluir en un perfil",
  },
  {
    slug: "mascotas-ruidos-fuertes-fiestas",
    category: "BIENESTAR",
    title: "Ruidos fuertes y celebraciones: prepara un espacio tranquilo",
    summary:
      "Anticipar cambios en el ambiente y ofrecer opciones seguras puede ayudar a que la mascota tenga un lugar donde resguardarse.",
    sections: [
      {
        heading: "Prepara un lugar familiar y supervisado",
        paragraphs: [
          "Elige un espacio interior con agua disponible y objetos familiares. Revisa que puertas, ventanas y accesos permanezcan cerrados y que la mascota pueda retirarse sin quedar atrapada.",
          "Acompáñala de la forma que le resulte cómoda. No la obligues a acercarse a la fuente del ruido ni la castigues por una reacción de miedo.",
        ],
      },
      {
        heading: "Reduce riesgos de escape",
        paragraphs: [
          "Antes de una celebración, confirma que sus datos de identificación estén actualizados y evita dejar abiertas salidas mientras entran o salen visitas.",
          "Si participa en una actividad fuera de casa, evalúa si el entorno y el transporte son adecuados para ella; no todas las mascotas toleran las mismas situaciones.",
        ],
      },
      {
        heading: "Evita medicarla sin indicación profesional",
        paragraphs: [
          "No administres sedantes, medicamentos humanos ni productos por recomendación informal. Consulta previamente a un profesional veterinario, que puede considerar su historia y necesidades.",
          "Si observas una situación que te preocupa, comunícate con una clínica o servicio veterinario local. NALA no brinda diagnóstico ni atención de urgencia.",
        ],
      },
    ],
    relatedHref: "/features",
    relatedLabel: "Conocer las funciones propuestas para el cuidado",
  },
  {
    slug: "plan-emergencia-evacuacion-mascotas",
    category: "PREPARACIÓN",
    title: "Incluye a tu mascota en el plan familiar de emergencia",
    summary:
      "Sismos, inundaciones, incendios y otras emergencias requieren seguir las indicaciones locales y decidir con anticipación cómo trasladar a los animales.",
    sections: [
      {
        heading: "Acuerden responsabilidades y puntos de encuentro",
        paragraphs: [
          "Define quién buscará a la mascota, qué transportador o correa usará y a quién avisará. Si no estás en casa, deja instrucciones con una persona de confianza que tenga acceso seguro.",
          "Identifica opciones de alojamiento temporal y confirma de antemano si aceptan animales. La disponibilidad y las reglas pueden cambiar durante una emergencia.",
        ],
      },
      {
        heading: "Prepara lo necesario para un traslado",
        paragraphs: [
          "Considera tener un transportador o sistema de sujeción adecuado, correa, recipientes, alimento habitual, agua y copias de los documentos importantes. Revisa periódicamente que los elementos sigan utilizables.",
          "Las necesidades dependen de cada animal y del evento. Pide a un profesional veterinario orientación sobre artículos específicos de salud o cuidado.",
        ],
      },
      {
        heading: "Sigue la información oficial de tu localidad",
        paragraphs: [
          "Consulta las alertas y rutas de evacuación publicadas por las autoridades locales. Las recomendaciones, albergues y restricciones cambian según el país y el tipo de emergencia.",
          "No ingreses a una zona peligrosa para recuperar pertenencias o animales sin autorización. Informa a los equipos de emergencia si una mascota quedó en riesgo.",
        ],
      },
      {
        heading: "Mantén identificación y contactos accesibles",
        paragraphs: [
          "Procura que una identificación acompañe a la mascota y que los contactos elegidos puedan responder. Protege los datos personales y evita poner una dirección residencial en publicaciones abiertas.",
          "El registro de emergencia y las alertas de NALA no están activos en este prototipo; usa los canales oficiales y comunitarios disponibles en tu localidad.",
        ],
      },
    ],
    relatedHref: "/pet-id",
    relatedLabel: "Ver la propuesta de identidad digital",
  },
  {
    slug: "evitar-estafas-mascota-perdida",
    category: "SEGURIDAD",
    title: "Cómo protegerte de estafas durante la búsqueda de tu mascota",
    summary:
      "Cuando compartes un aviso, limita la información pública y verifica con calma a las personas que dicen tener noticias.",
    sections: [
      {
        heading: "Reserva datos para confirmar la identidad",
        paragraphs: [
          "No publiques todos los rasgos distintivos. Guarda alguno para comprobar por privado que quien responde tiene información directa y consistente.",
          "Pide una descripción o fotografía reciente sin enviar primero tus propios detalles. Si algo no coincide, pausa la conversación y busca apoyo de alguien de confianza.",
        ],
      },
      {
        heading: "Nunca compartas códigos ni credenciales",
        paragraphs: [
          "No envíes contraseñas, códigos de verificación, datos bancarios o copias de documentos para demostrar que eres la persona tutora. Un código de acceso no sirve para reunir a una mascota.",
          "Desconfía de solicitudes urgentes de transferencias, tarjetas de regalo o pagos por adelantado para revelar una ubicación. Confirma la información por un canal independiente.",
        ],
      },
      {
        heading: "Coordina reuniones con medidas de seguridad",
        paragraphs: [
          "Si acuerdas un encuentro, considera un sitio público, avisa a una persona de confianza y evita compartir tu domicilio. No acudas si sientes que la situación no es segura.",
          "Ante amenazas o intentos de fraude, guarda los mensajes relevantes y consulta los canales oficiales de denuncia de tu localidad.",
        ],
      },
      {
        heading: "Publica solo lo necesario",
        paragraphs: [
          "Usa una zona aproximada y un medio de contacto que estés dispuesto a revisar. Quita del aviso los datos personales que no ayuden a reconocer a la mascota.",
          "La app contiene flujos de reporte de pérdida; este landing no publica avisos. La entrega de notificaciones y difusión por terceros no está verificada en producción.",
        ],
      },
    ],
    relatedHref: "/security",
    relatedLabel: "Leer el enfoque de seguridad de NALA",
  },
  {
    slug: "elegir-cuidador-guarderia-mascota",
    category: "CUIDADO",
    title: "Qué preguntar antes de elegir un cuidador o guardería",
    summary:
      "Conocer rutinas, supervisión, contactos y protocolos ayuda a decidir si un servicio se ajusta a tu mascota y a tu familia.",
    sections: [
      {
        heading: "Pregunta por la rutina y la supervisión",
        paragraphs: [
          "Consulta cómo transcurre un día, cuántos animales comparten el espacio, quién los supervisa y cómo se manejan los descansos y las separaciones.",
          "Si es posible, conoce el espacio y observa cómo se comunican las personas responsables sobre entradas, salidas y cambios de rutina.",
        ],
      },
      {
        heading: "Aclara qué sucede ante una emergencia",
        paragraphs: [
          "Pregunta qué contacto usarían, cómo te informarían y cómo coordinan atención veterinaria si fuese necesario. Deja por escrito las personas autorizadas y las instrucciones acordadas.",
          "Comparte únicamente la información necesaria para el servicio y confirma cómo protegen los datos de contacto y documentos.",
        ],
      },
      {
        heading: "Verifica credenciales y condiciones locales",
        paragraphs: [
          "Los requisitos, licencias y acreditaciones varían según el país y la localidad. Consulta a las autoridades u organismos profesionales pertinentes y pide al proveedor explicar con claridad su experiencia y condiciones.",
          "Solicita información escrita sobre horarios, costos, cancelaciones, transporte y qué incluye el servicio antes de aceptar.",
        ],
      },
      {
        heading: "Evalúa si la adaptación es adecuada",
        paragraphs: [
          "Pregunta si hay una visita previa o una adaptación gradual y qué opciones existen si tu mascota no se siente cómoda. No todas las instalaciones o rutinas sirven para todos los animales.",
          "El producto contiene directorios y flujos de proveedores, pero no se ha verificado la afiliación ni la vigencia de un cuidador concreto. Comprueba credenciales y condiciones antes de contratar.",
        ],
      },
    ],
    relatedHref: "/marketplace",
    relatedLabel: "Conocer la propuesta para servicios locales",
  },
  {
    slug: "convivencia-segura-ninos-mascotas",
    category: "CONVIVENCIA",
    title: "Niños y mascotas: acuerdos sencillos para convivir con respeto",
    summary:
      "La convivencia se construye con supervisión adulta, espacios de descanso y reglas consistentes para todas las personas del hogar.",
    sections: [
      {
        heading: "Mantén supervisión adulta activa",
        paragraphs: [
          "Una persona adulta debe acompañar las interacciones y estar lo bastante cerca para intervenir con calma. No dejes a un niño pequeño a solas con una mascota, aunque ya se conozcan.",
          "Explica las reglas antes de jugar y ofrece actividades alternativas que no impliquen perseguir, cargar o sujetar al animal.",
        ],
      },
      {
        heading: "Respeta los momentos de descanso",
        paragraphs: [
          "Enseña a no molestar a la mascota cuando come, duerme, se esconde o se retira. Prepara un espacio donde pueda descansar sin que la sigan.",
          "Evita obligar a un animal a aceptar abrazos, disfraces o contacto. Permite que se aleje y termina la interacción si alguna persona no está cómoda.",
        ],
      },
      {
        heading: "Acuerden qué hacer con toda la familia",
        paragraphs: [
          "Usen instrucciones breves y consistentes para acercarse, ofrecer un juguete o llamar a una persona adulta. La responsabilidad del cuidado sigue siendo de las personas adultas.",
          "Si hay una interacción que preocupa o preguntas sobre comportamiento, consulta a profesionales cualificados de tu localidad; esta guía no evalúa casos individuales.",
        ],
      },
    ],
    relatedHref: "/features",
    relatedLabel: "Explorar la visión de cuidado compartido",
  },
  {
    slug: "apoyar-refugio-organizacion-animal",
    category: "COMUNIDAD",
    title: "Cómo apoyar una organización de bienestar animal con confianza",
    summary:
      "Antes de donar, compartir un caso o sumarte como voluntario, confirma qué necesita la organización y cómo usará tu aporte.",
    sections: [
      {
        heading: "Verifica que el canal sea auténtico",
        paragraphs: [
          "Busca los canales oficiales de la organización por más de una vía y confirma directamente los datos de pago o recepción de donaciones antes de transferir dinero.",
          "Los registros legales y requisitos para organizaciones varían entre países. Si necesitas verificar una entidad, consulta los registros y autoridades disponibles en tu jurisdicción.",
        ],
      },
      {
        heading: "Pregunta qué apoyo es más útil ahora",
        paragraphs: [
          "Una organización puede necesitar insumos, transporte, difusión responsable, tiempo profesional o aportes económicos. Pregunta antes de llevar artículos que quizá no pueda almacenar o utilizar.",
          "Si compartes una campaña, conserva el contexto y no edites imágenes o mensajes de forma que cambien lo que la organización informó.",
        ],
      },
      {
        heading: "Cuida la privacidad y el consentimiento",
        paragraphs: [
          "Pide permiso antes de publicar fotos, historias o datos de personas beneficiarias y voluntarias. Evita mostrar domicilios, documentos o ubicaciones sensibles.",
          "La app contiene perfiles aliados y flujos de refugios/adopción; no se ha verificado afiliación, convenio ni operación de una organización concreta.",
        ],
      },
      {
        heading: "Acuerda las condiciones si colaboras como voluntario",
        paragraphs: [
          "Confirma tareas, horarios, persona de contacto y medidas de seguridad antes de empezar. No realices procedimientos veterinarios ni tareas para las que no tengas preparación.",
          "Si una actividad implica transportar animales o manejar datos, acuerda cómo se autoriza y quién es responsable antes de aceptarla.",
        ],
      },
    ],
    relatedHref: "/shelters",
    relatedLabel: "Ver la propuesta para refugios y ONGs",
  },
  {
    slug: "antes-de-adoptar-mascota",
    category: "ADOPCIÓN",
    title: "Antes de adoptar: conversaciones que conviene tener en familia",
    summary:
      "La decisión funciona mejor cuando las personas del hogar entienden el compromiso, la rutina y las preguntas que aún faltan por resolver.",
    sections: [
      {
        heading: "Hablen sobre tiempo y responsabilidades",
        paragraphs: [
          "Acuerden quién participa en la rutina diaria, quién puede apoyar durante viajes o cambios de horario y cómo se tomarán las decisiones importantes.",
          "Consideren espacio, movilidad y las necesidades cotidianas del hogar. Eviten basar la decisión en una expectativa de que la mascota se adapte inmediatamente a cualquier situación.",
        ],
      },
      {
        heading: "Pregunten qué información está confirmada",
        paragraphs: [
          "Soliciten los datos y documentos que la organización pueda compartir, pregunten qué se conoce y qué sigue sin confirmarse, y anoten las dudas para hablarlas con un profesional cuando corresponda.",
          "Los procesos y acuerdos de adopción cambian entre organizaciones y países. Lee las condiciones antes de firmar o entregar información personal.",
        ],
      },
      {
        heading: "Preparar el hogar también es parte de adoptar",
        paragraphs: [
          "Define un espacio tranquilo, revisa accesos y acuerda las reglas del hogar antes de la llegada. Mantén objetos y productos potencialmente peligrosos fuera del alcance de animales y niños.",
          "Consulta con un profesional veterinario sobre prevención, controles y cualquier pregunta de salud basada en la historia que esté disponible.",
        ],
      },
      {
        heading: "Permite una transición gradual",
        paragraphs: [
          "Mantén rutinas previsibles y deja que la mascota explore a su ritmo. Acordar expectativas realistas ayuda a las personas del hogar a responder con paciencia.",
          "PawTrack CR contiene perfiles digitales, directorios y flujos de adopción; la disponibilidad y participación de una organización concreta deben confirmarse directamente.",
        ],
      },
    ],
    relatedHref: "/shelters",
    relatedLabel: "Conocer la propuesta para organizaciones",
  },
  {
    slug: "cerrar-aviso-mascota-reunida",
    category: "RECUPERACIÓN",
    title: "Después del reencuentro: cierra el aviso y protege tus datos",
    summary:
      "Actualizar la información que compartiste reduce confusiones y ayuda a recuperar la privacidad después de una búsqueda.",
    sections: [
      {
        heading: "Confirma el reencuentro de manera segura",
        paragraphs: [
          "Antes de entregar o recibir una mascota, acuerda cómo reconocerla y verifica la información con calma. Si hay dudas, pide apoyo a una organización o autoridad local adecuada.",
          "Evita publicar documentos personales o datos de contacto de otras personas para demostrar cómo se resolvió el caso.",
        ],
      },
      {
        heading: "Retira o actualiza las publicaciones",
        paragraphs: [
          "Marca el aviso como resuelto o elimínalo donde tengas control. Si no puedes borrarlo, publica una actualización breve sin exponer información privada.",
          "Revisa comentarios, fotos y datos que otras personas hayan republicado. Solicita que retiren información sensible cuando sea posible.",
        ],
      },
      {
        heading: "Vuelve a revisar contactos e identificadores",
        paragraphs: [
          "Comprueba que los contactos de confianza y datos de identificación sigan siendo correctos. Evita mantener pública una ubicación temporal o el domicilio de la familia.",
          "Si el animal necesita evaluación o tienes dudas sobre su bienestar, contacta a un profesional veterinario. No intentes interpretar lesiones a partir de una guía en línea.",
        ],
      },
    ],
    relatedHref: "/privacy",
    relatedLabel: "Revisar los principios de privacidad",
  },
];
