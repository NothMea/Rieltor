-- Скрипт для проверки структуры таблицы Leases
-- Выполните в SQL Server Management Studio

-- 1. Проверяем структуру таблицы Leases
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Leases' AND COLUMN_NAME = 'ConsentDocumentPath';

-- 2. Проверяем все ограничения на таблице Leases
SELECT 
    OBJECT_NAME(parent_object_id) AS TableName,
    name AS ConstraintName,
    type_desc AS ConstraintType,
    definition
FROM sys.sql_expression_dependencies
JOIN sys.objects ON referenced_id = object_id
WHERE referenced_entity_name = 'Leases'
AND class = 1;

-- 3. Проверяем наличие триггеров на таблице Leases
SELECT 
    name AS TriggerName,
    is_disabled,
    is_instead_of_trigger
FROM sys.triggers
WHERE parent_id = OBJECT_ID('dbo.Leases');

-- 4. Альтернативная проверка структуры через sys.columns
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.Leases')
ORDER BY c.column_id;
