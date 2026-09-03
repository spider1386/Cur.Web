/*
    Carta de presentacion (una por usuario).

    Tabla nueva sobre BD_Curriculums. El proyecto no genera migraciones para las tablas
    de negocio, asi que este script se ejecuta a mano antes de desplegar la funcionalidad.
    Es idempotente: se puede correr varias veces sin efecto.
*/

IF OBJECT_ID(N'dbo.Carta_Presentacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Carta_Presentacion
    (
        CartaID             INT             IDENTITY(1,1) NOT NULL,
        UserId              NVARCHAR(450)   NOT NULL,
        CargoObjetivo       NVARCHAR(150)   NOT NULL,
        Empresa             NVARCHAR(250)   NULL,
        Tono                INT             NOT NULL CONSTRAINT DF_Carta_Presentacion_Tono DEFAULT (0),
        Texto               NVARCHAR(MAX)   NOT NULL,
        IncluirEnHojaDeVida BIT             NOT NULL CONSTRAINT DF_Carta_Presentacion_Incluir DEFAULT (0),
        ActualizadaEn       DATETIME2(7)    NOT NULL CONSTRAINT DF_Carta_Presentacion_Fecha DEFAULT (SYSDATETIME()),

        CONSTRAINT PK_Carta_Presentacion PRIMARY KEY CLUSTERED (CartaID)
    );

    -- Una sola carta por usuario: el guardado hace upsert sobre esta clave.
    CREATE UNIQUE INDEX IX_Carta_Presentacion_UserId
        ON dbo.Carta_Presentacion (UserId);

    -- Si el usuario se borra de Identity, su carta se va con el.
    ALTER TABLE dbo.Carta_Presentacion
        ADD CONSTRAINT FK_Carta_Presentacion_AspNetUsers
        FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE;
END
GO
