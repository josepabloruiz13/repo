-- Job de SQL Server Agent: corre el programa de lunes a viernes a las 8:41 a. m.
-- IMPORTANTE:
--  * La hora es la del SERVIDOR. Si el servidor no está en hora de Costa Rica (UTC-6), ajusta @active_start_time.
--  * Cambia @exe a la ruta real donde publicaste el programa.
--  * El paso corre con la cuenta del servicio SQL Server Agent (o con un proxy de CmdExec si lo configuras).
--    Esa cuenta necesita: leer la carpeta del programa y el JSON de la cuenta de servicio de Google,
--    y tener definidas las variables de entorno SWIFTPAY_SwiftPortals__ApiKey y SWIFTPAY_Claude__ApiKey.
--  * Primero prueba con --simular. Cuando el resumen se vea bien, cambia a --real.

USE msdb;
GO

DECLARE @exe nvarchar(400) = N'C:\SwiftPay\Onboarding\SwiftPay.Onboarding.exe';
DECLARE @modo nvarchar(20) = N'--simular';   -- cambiar a N'--real' cuando esté validado

IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'SwiftPay - Onboarding comercios')
    EXEC msdb.dbo.sp_delete_job @job_name = N'SwiftPay - Onboarding comercios';

EXEC msdb.dbo.sp_add_job
    @job_name = N'SwiftPay - Onboarding comercios',
    @description = N'Seguimiento de onboardings con el banco, correos al comercio y activación en SwiftPortals.',
    @enabled = 1;

DECLARE @cmd nvarchar(1000) = N'"' + @exe + N'" ' + @modo;
EXEC msdb.dbo.sp_add_jobstep
    @job_name = N'SwiftPay - Onboarding comercios',
    @step_name = N'Ejecutar SwiftPay.Onboarding',
    @subsystem = N'CmdExec',
    @command = @cmd,
    @cmdexec_success_code = 0,
    @retry_attempts = 0;   -- sin reintentos: el programa reintenta en la siguiente ejecución

EXEC msdb.dbo.sp_add_schedule
    @schedule_name = N'SwiftPay - L a V 8:41',
    @freq_type = 8,                 -- semanal
    @freq_interval = 62,            -- lunes(2)+martes(4)+miércoles(8)+jueves(16)+viernes(32)
    @freq_recurrence_factor = 1,
    @active_start_time = 084100;    -- 08:41:00 hora del servidor

EXEC msdb.dbo.sp_attach_schedule
    @job_name = N'SwiftPay - Onboarding comercios',
    @schedule_name = N'SwiftPay - L a V 8:41';

EXEC msdb.dbo.sp_add_jobserver @job_name = N'SwiftPay - Onboarding comercios';
GO
