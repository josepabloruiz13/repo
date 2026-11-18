-- Feriados de Costa Rica usados para contar días hábiles.
-- Verificado = 1 solo cuando la fecha se confirmó contra el calendario oficial del MTSS.
-- Las fechas de 2027 vienen de la tarea anterior y NO están verificadas: revisarlas contra el calendario oficial
-- y cambiar Verificado a 1 (o corregir la fecha). Mientras no haya feriados verificados del año en curso,
-- el resumen de cada ejecución lo avisa.

MERGE onb.Feriado AS t
USING (VALUES
    ('2026-12-01', N'Abolición del Ejército',          1),
    ('2026-12-25', N'Navidad',                         1),
    ('2027-01-01', N'Año Nuevo',                       0),
    ('2027-03-25', N'Jueves Santo',                    0),
    ('2027-03-26', N'Viernes Santo',                   0),
    ('2027-04-11', N'Día de Juan Santamaría',          0),
    ('2027-05-01', N'Día del Trabajador',              0),
    ('2027-07-25', N'Anexión del Partido de Nicoya',   0),
    ('2027-08-02', N'Virgen de los Ángeles',           0),
    ('2027-08-15', N'Día de la Madre',                 0),
    ('2027-08-31', N'Día de la Persona Negra',         0),
    ('2027-09-15', N'Día de la Independencia',         0),
    ('2027-12-01', N'Abolición del Ejército',          0),
    ('2027-12-25', N'Navidad',                         0)
) AS s (Fecha, Descripcion, Verificado)
ON t.Fecha = s.Fecha
WHEN NOT MATCHED THEN INSERT (Fecha, Descripcion, Verificado) VALUES (s.Fecha, s.Descripcion, s.Verificado);
GO
