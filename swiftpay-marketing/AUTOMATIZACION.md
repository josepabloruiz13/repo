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

## Informe de cada lunes

Con las herramientas de analítica de Metricool (Facebook e Instagram, últimos 7 días): alcance, interacciones, seguidores nuevos y la mejor publicación. Comparar con la semana anterior y dar 1 o 2 recomendaciones concretas para las próximas piezas. Enviar el resumen a José Pablo.
