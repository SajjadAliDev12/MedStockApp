-- V1: rename Items.MinStok -> MinStock (idempotent)
-- Run against HospitalInventoryDb via sqlcmd.
IF COL_LENGTH('dbo.Items', 'MinStok') IS NOT NULL AND COL_LENGTH('dbo.Items', 'MinStock') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Items.MinStok', 'MinStock', 'COLUMN';
END
GO
-- Verification: old must be NULL, new must exist
SELECT COL_LENGTH('dbo.Items', 'MinStok') AS OldMinStok, COL_LENGTH('dbo.Items', 'MinStock') AS NewMinStock;
GO
