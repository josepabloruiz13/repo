# Mercadeo automático de SwiftMart (Facebook + Instagram)

Instrucciones para la tarea programada de cada jueves. Dueño: José Pablo (SwiftPay, Costa Rica).

## Qué hacer cada jueves

1. Leer `base_conocimiento.md`. Es la ÚNICA fuente de datos (precios, funciones, plazos). Nunca inventar datos.
2. Ver en `calendario.md` qué toca la semana siguiente (lunes, miércoles y viernes). Si el calendario ya no tiene esa semana, proponer 3 temas nuevos de SwiftMart con la base de conocimiento, sin repetir temas de `publicadas/`, y agregarlos a `calendario.md`.
3. Diseñar las piezas con `herramientas/lib.py` (ver `herramientas/semana2.py` como ejemplo): mismo estilo siempre.
   - Formatos: publicación 1080×1350 (4:5), carrusel de 3 a 4 láminas 1080×1350, reel o historia 1080×1920.
   - Reels: generar 5 o 6 escenas PNG acumulativas y unirlas con ffmpeg `xfade` (fade 0,35 s, unos 3 s por escena, `-preset veryfast -crf 20 -pix_fmt yuv420p -movflags +faststart`).
   - Fondos: `assets/` (fotos e ilustración) o el degradado azul (`page(..., None, ...)`). No usar fotos con texto o logos mal escritos.
   - Revisar cada PNG con la vista de imagen antes de publicar: nada encimado, texto legible, logo correcto.
4. Guardar en `publicadas/AAAA-MM-semanaN/` con nombre `AAAA-MM-DD_tema.png|mp4`, hacer commit y push a `master` (autor `Claude <noreply@anthropic.com>`).
5. Programar en Metricool (marca `blogId` 7943734, zona `America/Guatemala`, igual a Costa Rica) a las 12:00 m., Facebook + Instagram, `autoPublish: true`. Media = URL raw de GitHub:
   `https://raw.githubusercontent.com/josepabloruiz13/repo/master/swiftpay-marketing/publicadas/<carpeta>/<archivo>`
   - Texto: de tú, cercano, 2 a 4 líneas + cierre fijo + hashtags (abajo). Sin emojis en exceso.
   - Reels: `facebookData.type = REEL`, `instagramData.type = REEL`.
   - `instagramData.isAiGenerated = true` solo si la pieza usa `foto_cafe.jpg`, `foto_restaurante.jpg` o `ilus_swiftmart.jpg` (generadas con IA).
6. Avisar a José Pablo con un resumen: fecha, tema y enlace de Metricool de cada publicación, y las imágenes. Si algo falló, decirlo claramente.

Cierre fijo de cada texto: "Pruébalo 30 días gratis, sin tarjeta. Escríbenos al WhatsApp +506 6180 1322."
Hashtags base: #SwiftMart #SwiftPay #PuntoDeVenta #PymesCR #EmprendedoresCR #CostaRica (+1 o 2 del tema).

## Reglas

- Nunca afirmar algo que no esté en `base_conocimiento.md`.
- No gastar dinero ni tocar campañas pagadas desde esta tarea.
- No publicar nada para el mismo día: programar siempre con al menos 3 días de margen.
- Si Metricool o GitHub fallan, no reintentar en bucle: avisar y detenerse.

## Anuncios pagados (Meta Ads)

Origen: el 7 nov 2026 se creó la campaña "SwiftMart - WhatsApp - Nov 2026" con un anuncio SIN imagen (la imagen se subió a la cuenta pero no quedó conectada al anuncio) y se dio por lista sin revisar la vista previa. José Pablo lo corrigió a mano el 10 nov. Estas reglas existen para que no se repita.

1. Solo se crean o cambian anuncios pagados cuando José Pablo lo pide explícitamente en una conversación. Las tareas programadas nunca crean, activan, pausan ni cambian anuncios ni presupuestos.
2. Todo anuncio nuevo se deja PAUSADO (campaña, conjunto y anuncio). Lo enciende José Pablo después de ver la vista previa.
3. Antes de decir que un anuncio está listo, verificar en la cuenta de Meta (Adspirer, solo lectura) y confirmarle por escrito, punto por punto:
   - el anuncio tiene imagen o video conectado (no basta con haberlo subido a la cuenta);
   - texto principal, título y número de WhatsApp (+506 6180 1322) correctos;
   - presupuesto total y fechas de inicio y fin;
   - a quién se muestra (país, edades, intereses);
   - mejoras automáticas de Advantage+ que cambian imagen o texto: apagadas (retoques visuales, superposiciones, mejoras en el texto). "Comentarios relevantes" puede quedar encendido.
4. Pedirle que abra la Vista previa del anuncio en el Administrador de anuncios (Facebook e Instagram) antes de encenderlo. Si la vista previa dice "Please add an image to your ad" o muestra advertencias, no está listo.
5. Si algo de lo anterior no se puede verificar, decirlo claramente; nunca darlo por bueno.
6. Formato que funciona con Adspirer: anuncio a sitio web (objetivo Tráfico), destino https://swiftpay.one/precios, botón SIGN_UP. Adspirer NO permite el botón "Enviar mensaje" (Messenger/Instagram) y el WhatsApp del bot (Twilio, +506 6180 1322) está en otro portfolio comercial ("Swiftpay" verificado) sin conexión a la página: no usar enlaces wa.me como destino (Meta los rechaza).
7. Cuota de Adspirer: 15 llamadas al mes (se renueva el día 7). Usar las mínimas; agotada el 10 nov 2026.

Estado al 10 nov 2026: campaña "SwiftMart - WhatsApp - Nov 2026" (120248461830200788). Anuncio activo: "SwiftMart - Registro web - Nov 2026" (120248506335010788) en el conjunto original 120248461830290788 (₡20.000 total, hasta 5 dic). Anuncio viejo con wa.me pausado (120248461832000788). Conjunto "Ad Set - Copia" (120248506167120788) pausado y sin anuncios.

## Informe de cada lunes

Con las herramientas de analítica de Metricool (Facebook e Instagram, últimos 7 días): alcance, interacciones, seguidores nuevos y la mejor publicación. Comparar con la semana anterior y dar 1 o 2 recomendaciones concretas para las próximas piezas. Enviar el resumen a José Pablo.

Además, revisión de anuncios pagados (solo lectura, sin cambiar nada): con Adspirer (`meta_ads_read`, cuenta 357578211765215) listar las campañas activas o en revisión y, para cada una, informar: estado (activa, en revisión, rechazada, pausada), monto gastado y presupuesto restante, resultados (clics a WhatsApp) y costo por resultado, y si el anuncio tiene imagen o video. Avisar de inmediato si hay un anuncio sin imagen, rechazado o con advertencias. Usar el mínimo de llamadas (la cuota de Adspirer es limitada).
