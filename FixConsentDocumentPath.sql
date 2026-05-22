-- Скрипт для исправления проблемы с полем ConsentDocumentPath
-- Выполните в SQL Server Management Studio

-- ВАРИАНТ 1: Если поле существует, но имеет ограничение NOT NULL - изменяем на NULL
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Leases]') AND name = N'ConsentDocumentPath')
BEGIN
    -- Проверяем, является ли поле NOT NULL
    IF (SELECT is_nullable FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Leases]') AND name = N'ConsentDocumentPath') = 0
    BEGIN
        PRINT 'Поле ConsentDocumentPath имеет ограничение NOT NULL. Изменяем на NULL...';
        ALTER TABLE [dbo].[Leases] ALTER COLUMN [ConsentDocumentPath] NVARCHAR(500) NULL;
        PRINT 'Поле ConsentDocumentPath изменено на NULLABLE';
    END
    ELSE
    BEGIN
        PRINT 'Поле ConsentDocumentPath уже имеет тип NVARCHAR(500) NULL';
    END
END
ELSE
BEGIN
    PRINT 'Поле ConsentDocumentPath не существует. Создаем...';
    ALTER TABLE [dbo].[Leases] ADD [ConsentDocumentPath] NVARCHAR(500) NULL;
    PRINT 'Поле ConsentDocumentPath создано';
END

-- Проверяем длину поля (должна быть минимум 500 символов)
DECLARE @maxLength INT;
SELECT @maxLength = max_length FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Leases]') AND name = N'ConsentDocumentPath';

IF @maxLength < 500
BEGIN
    PRINT 'Длина поля ConsentDocumentPath меньше 500. Исправляем...';
    ALTER TABLE [dbo].[Leases] ALTER COLUMN [ConsentDocumentPath] NVARCHAR(1000) NULL;
    PRINT 'Поле ConsentDocumentPath изменено на NVARCHAR(1000)';
END
ELSE
BEGIN
    PRINT 'Длина поля ConsentDocumentPath достаточна: ' + CAST(@maxLength AS VARCHAR);
END

PRINT 'Проверка и исправление поля ConsentDocumentPath завершены';
