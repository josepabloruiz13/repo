-- Esquema de estado y bitácora del programa SwiftPay.Onboarding.
-- Ejecutar una vez en la base de datos indicada en Sql:ConnectionString (por defecto: SwiftPay).

IF SCHEMA_ID('onb') IS NULL EXEC('CREATE SCHEMA onb');
GO

IF OBJECT_ID('onb.Feriado') IS NULL
CREATE TABLE onb.Feriado (
    Fecha        date          NOT NULL PRIMARY KEY,
    Descripcion  nvarchar(100) NOT NULL,
    Verificado   bit           NOT NULL DEFAULT 0   -- 1 = confirmado contra el calendario oficial del MTSS
);
GO

IF OBJECT_ID('onb.Onboarding') IS NULL
CREATE TABLE onb.Onboarding (
    Id               int IDENTITY PRIMARY KEY,
    ThreadId         varchar(64)   NOT NULL UNIQUE,   -- hilo de Gmail con el banco
    Comercio         nvarchar(200) NOT NULL,
    Dia0             date          NOT NULL,
    N                int           NOT NULL,
    Etapa            nvarchar(80)  NOT NULL,
    ContactoNombre   nvarchar(100) NULL,
    ContactoEmail    nvarchar(200) NULL,
    Merchant         varchar(40)   NULL,
    Cedula           varchar(40)   NULL,
    Completado       bit           NOT NULL DEFAULT 0,
    FechaCreado      datetime2     NOT NULL DEFAULT SYSDATETIME(),
    FechaActualizado datetime2     NOT NULL DEFAULT SYSDATETIME()
);
GO

IF OBJECT_ID('onb.Envio') IS NULL
CREATE TABLE onb.Envio (
    Id             int IDENTITY PRIMARY KEY,
    ThreadId       varchar(64)   NOT NULL,
    Comercio       nvarchar(200) NOT NULL,
    Tipo           varchar(40)   NOT NULL,   -- INICIO, SEGUIMIENTO_COMERCIO, RECORDATORIO_BANCO, SEGUIMIENTO_BANCO, BIENVENIDA, ALERTA_*
    Clave          varchar(20)   NOT NULL,   -- N, fecha (alertas) o '-' (una sola vez)
    Destinatarios  nvarchar(500) NOT NULL,
    Asunto         nvarchar(300) NOT NULL,
    GmailId        varchar(64)   NULL,
    EsBorrador     bit           NOT NULL,
    Fecha          datetime2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_Envio UNIQUE (ThreadId, Tipo, Clave)   -- evita correos duplicados
);
GO

IF OBJECT_ID('onb.LlamadaApi') IS NULL
CREATE TABLE onb.LlamadaApi (
    Id            int IDENTITY PRIMARY KEY,
    ThreadId      varchar(64)   NOT NULL,
    Comercio      nvarchar(200) NOT NULL,
    Cedula        varchar(40)   NOT NULL,
    Terminal      varchar(40)   NOT NULL,
    TipoTerminal  varchar(20)   NOT NULL,
    CodigoHttp    int           NULL,      -- NULL = timeout o error de conexión
    Exito         bit           NOT NULL,
    Respuesta     nvarchar(400) NULL,
    Fecha         datetime2     NOT NULL DEFAULT SYSDATETIME()
);
GO

IF OBJECT_ID('onb.Ejecucion') IS NULL
CREATE TABLE onb.Ejecucion (
    Id      bigint IDENTITY PRIMARY KEY,
    Inicio  datetime2     NOT NULL DEFAULT SYSDATETIME(),
    Fin     datetime2     NULL,
    DryRun  bit           NOT NULL,
    Resumen nvarchar(max) NULL,
    Error   nvarchar(max) NULL
);
GO
