# SwiftPay.Onboarding

Programa en C# .NET 8 que reemplaza a la tarea programada "Onboarding SwiftPay" de Claude.
Corre en tu servidor desde un job de SQL Server Agent, así que la llamada al API de SwiftPortals
sale desde tu propia red.

Hace lo mismo que la tarea anterior, cada día hábil:

- Busca en `afiliacion@swiftpay.one` los hilos "Solicitud de onboarding <COMERCIO>" enviados al banco (últimos 60 días).
- Cuenta N (días hábiles desde el día 0, sin fines de semana ni feriados de `onb.Feriado`).
- **Sin datos de creación:** etiquetas 1, 2 y 3; correo de INICIO al comercio; recordatorio al banco en N = 3;
  seguimiento al banco en N = 4, 6, 8…; seguimiento al comercio en N = 2 y 4, y con disculpa en N = 6, 8…
- **Con datos de creación (Merchant / Terminal):** etiqueta 4; lee la cédula del Excel adjunto al correo de día 0;
  llama al API de activación una vez por terminal; si todo responde 2xx envía la BIENVENIDA y mueve el hilo a
  "Afiliaciones completadas"; si falla, envía la ALERTA interna (una por comercio por día) y reintenta en la siguiente ejecución.
- Al final envía un **resumen** a `afiliacion@swiftpay.one` (configurable) y lo guarda en `onb.Ejecucion`.

Reglas que se mantienen: confidencialidad del banco (el programa bloquea cualquier correo al comercio que vaya
a `@lafise.com` o mencione al banco), nada se borra ni archiva, no se duplican correos (revisa `onb.Envio` y
también Enviados de Gmail, para no repetir lo que ya mandó la tarea anterior), y si no se tiene con seguridad
el correo del comercio queda un borrador sin destinatario.

## Qué hace con reglas fijas y qué con la API de Claude

| Parte | Cómo |
|---|---|
| Datos de creación (SP\*, Merchant, Terminal) | Reglas (expresiones regulares) |
| Cédula del Excel | Reglas (hoja "Costa Rica", celda debajo de "Cédula física/jurídica") |
| Días hábiles, plantillas, etiquetas, duplicados | Reglas |
| ¿El banco pide información? ¿Confirmó la aceptación? Número de solicitud | API de Claude (si no hay llave, se reporta para revisión manual) |
| Contacto del comercio cuando el formulario no sigue el formato esperado | API de Claude |

## Instalación

### 1. Cuenta de servicio de Google (Gmail)

Requiere que `swiftpay.one` esté en Google Workspace y acceso de administrador.

1. En Google Cloud Console: crea un proyecto, habilita **Gmail API** y crea una **cuenta de servicio**. Descarga su llave JSON.
2. En la consola de administración de Google Workspace: *Seguridad → Control de acceso y datos → Controles de API →
   Delegación de todo el dominio*. Agrega el **Client ID** de la cuenta de servicio con el alcance:
   `https://www.googleapis.com/auth/gmail.modify`
3. Copia el JSON al servidor (por ejemplo `C:\SwiftPay\Onboarding\secrets\gmail-service-account.json`) y deja que solo
   la cuenta que corre el job pueda leerlo.

### 2. Base de datos

Ejecuta en orden, en la base indicada en `Sql:ConnectionString`:

1. `sql/01_esquema.sql`
2. `sql/02_feriados.sql` (los feriados de 2027 vienen marcados como **no verificados**: confírmalos con el MTSS)

### 3. Publicar el programa

```bash
dotnet publish src/SwiftPay.Onboarding -c Release -r win-x64 --self-contained false -o C:\SwiftPay\Onboarding
```

El servidor necesita el runtime de .NET 8.

### 4. Configuración

`appsettings.json` (junto al .exe) trae los valores no secretos. Los secretos van en **variables de entorno**
de la cuenta que corre el job, nunca en el repositorio:

| Variable | Para qué |
|---|---|
| `SWIFTPAY_SwiftPortals__ApiKey` | Llave X-Api-Key de SwiftPortals |
| `SWIFTPAY_Claude__ApiKey` | Llave de la API de Anthropic (console.anthropic.com) |
| `SWIFTPAY_Sql__ConnectionString` | Opcional, si no usas la de appsettings.json |

`DryRun` viene en `true`: en ese modo no envía correos, no cambia etiquetas, no crea borradores, no llama al API
ni escribe en SQL; solo imprime lo que haría.

### 5. Probar

```bash
C:\SwiftPay\Onboarding\SwiftPay.Onboarding.exe --simular
```

Revisa el resumen que imprime. Cuando se vea bien, prueba la conexión a SwiftPortals desde el servidor y pasa a `--real`.

### 6. Job de SQL Server Agent

Ejecuta `sql/03_job_sql_agent.sql` (ajusta la ruta del .exe, el modo y la hora si el servidor no está en hora de Costa Rica).
El historial del job guarda la salida del programa; un código de salida 1 marca el job como fallido.

### 7. Cambio desde la tarea de Claude

Cuando el job corra en modo `--real`, **desactiva la tarea programada "Onboarding SwiftPay" de Claude** para que no
corran las dos. El programa revisa Enviados antes de mandar, pero tener dos procesos activos a la vez no es recomendable.

## Pruebas

```bash
dotnet test
```

Cubren días hábiles, lectura de datos de creación, cédula del Excel, asuntos y formulario, y que las plantillas al
comercio no mencionen al banco. Lo que habla con Gmail, SQL y SwiftPortals se valida con `--simular` en el servidor.
