-- Скрипт обновления базы данных для добавления истории договоров
-- Выполнить в SQL Server Management Studio

-- 1. Создаем таблицу LeaseHistory для хранения завершенных/расторгнутых договоров
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LeaseHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LeaseHistory](
        [HistoryID] INT IDENTITY(1,1) PRIMARY KEY,
        [LeaseID] INT NOT NULL,
        [LeaseNumber] NVARCHAR(50) NOT NULL,
        [PropertyID] INT NOT NULL,
        [TenantID] INT NOT NULL,
        [StartDate] DATETIME NOT NULL,
        [EndDate] DATETIME NOT NULL,
        [MonthlyAmount] DECIMAL(18,2) NOT NULL,
        [OriginalStatus] NVARCHAR(50) NOT NULL,
        [TerminationDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [TerminationReason] NVARCHAR(500) NULL,
        [TerminatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
    );
    
    PRINT 'Таблица LeaseHistory создана успешно';
END
ELSE
BEGIN
    PRINT 'Таблица LeaseHistory уже существует';
END

-- 2. Добавляем поле IsArchived в таблицу Leases для пометки архивированных договоров
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Leases]') AND name = N'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Leases] ADD [IsArchived] BIT NOT NULL DEFAULT 0;
    PRINT 'Поле IsArchived добавлено в таблицу Leases';
END
ELSE
BEGIN
    PRINT 'Поле IsArchived уже существует в таблице Leases';
END

-- 3. Добавляем поле TerminationReason в таблицу Leases для причины расторжения
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Leases]') AND name = N'TerminationReason')
BEGIN
    ALTER TABLE [dbo].[Leases] ADD [TerminationReason] NVARCHAR(500) NULL;
    PRINT 'Поле TerminationReason добавлено в таблицу Leases';
END
ELSE
BEGIN
    PRINT 'Поле TerminationReason уже существует в таблице Leases';
END

-- 4. Добавляем поле ConsentDocumentPath в таблицу Leases для пути к документу согласия
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Leases]') AND name = N'ConsentDocumentPath')
BEGIN
    ALTER TABLE [dbo].[Leases] ADD [ConsentDocumentPath] NVARCHAR(500) NULL;
    PRINT 'Поле ConsentDocumentPath добавлено в таблицу Leases';
END
ELSE
BEGIN
    PRINT 'Поле ConsentDocumentPath уже существует в таблице Leases';
END

-- 5. Добавляем поле ConsentDocumentPath в таблицу LeaseHistory для пути к документу согласия
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaseHistory]') AND name = N'ConsentDocumentPath')
BEGIN
    ALTER TABLE [dbo].[LeaseHistory] ADD [ConsentDocumentPath] NVARCHAR(500) NULL;
    PRINT 'Поле ConsentDocumentPath добавлено в таблицу LeaseHistory';
END
ELSE
BEGIN
    PRINT 'Поле ConsentDocumentPath уже существует в таблице LeaseHistory';
END

-- 6. Создаем хранимую процедуру для переноса договора в историю
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_ArchiveLease')
    DROP PROCEDURE [dbo].[sp_ArchiveLease];
GO

CREATE PROCEDURE [dbo].[sp_ArchiveLease]
    @LeaseID INT,
    @TerminationReason NVARCHAR(500) = NULL,
    @TerminatedBy NVARCHAR(100) = NULL,
    @ConsentDocumentPath NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Получаем данные договора
        DECLARE @LeaseNumber NVARCHAR(50),
                @PropertyID INT,
                @TenantID INT,
                @StartDate DATETIME,
                @EndDate DATETIME,
                @MonthlyAmount DECIMAL(18,2),
                @Status NVARCHAR(50);
        
        SELECT 
            @LeaseNumber = LeaseNumber,
            @PropertyID = PropertyID,
            @TenantID = TenantID,
            @StartDate = StartDate,
            @EndDate = EndDate,
            @MonthlyAmount = MonthlyAmount,
            @Status = Status
        FROM [dbo].[Leases]
        WHERE LeaseID = @LeaseID;
        
        IF @LeaseNumber IS NULL
        BEGIN
            RAISERROR('Договор не найден', 16, 1);
            RETURN;
        END
        
        -- Вставляем запись в историю
        INSERT INTO [dbo].[LeaseHistory] (
            LeaseID, LeaseNumber, PropertyID, TenantID, 
            StartDate, EndDate, MonthlyAmount, OriginalStatus,
            TerminationDate, TerminationReason, TerminatedBy, ConsentDocumentPath
        )
        VALUES (
            @LeaseID, @LeaseNumber, @PropertyID, @TenantID,
            @StartDate, @EndDate, @MonthlyAmount, @Status,
            GETDATE(), @TerminationReason, @TerminatedBy, @ConsentDocumentPath
        );
        
        -- Помечаем договор как архивированный в основной таблице
        UPDATE [dbo].[Leases]
        SET IsArchived = 1,
            TerminationReason = @TerminationReason,
            ConsentDocumentPath = @ConsentDocumentPath
        WHERE LeaseID = @LeaseID;
        
        COMMIT TRANSACTION;
        PRINT 'Договор успешно перенесен в историю';
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
GO

PRINT 'Хранимая процедура sp_ArchiveLease создана';

-- 5. Создаем представление для активных договоров (не архивированных)
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_ActiveLeases')
    DROP VIEW [dbo].[vw_ActiveLeases];
GO

CREATE VIEW [dbo].[vw_ActiveLeases]
AS
SELECT * FROM [dbo].[Leases]
WHERE IsArchived = 0;
GO

PRINT 'Представление vw_ActiveLeases создано';

-- 6. Создаем триггер для автоматического обновления статуса при окончании срока
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'tr_CheckLeaseExpiration')
    DROP TRIGGER [dbo].[tr_CheckLeaseExpiration];
GO

CREATE TRIGGER [dbo].[tr_CheckLeaseExpiration]
ON [dbo].[Leases]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Обновляем статус на "Завершен" если дата окончания прошла и договор еще активен
    UPDATE l
    SET Status = 'Завершен'
    FROM [dbo].[Leases] l
    INNER JOIN inserted i ON l.LeaseID = i.LeaseID
    WHERE l.Status = 'Активен'
      AND l.EndDate < GETDATE()
      AND l.IsArchived = 0;
END
GO

PRINT 'Триггер tr_CheckLeaseExpiration создан';

PRINT 'Обновление базы данных завершено успешно!';
