-- V2: user lockout columns (additive, idempotent)
-- Run against HospitalInventoryDb via sqlcmd.
IF COL_LENGTH('dbo.Users', 'FailedAttempts') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD FailedAttempts INT NOT NULL CONSTRAINT DF_Users_FailedAttempts DEFAULT (0);
END
GO
IF COL_LENGTH('dbo.Users', 'LockedUntil') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD LockedUntil DATETIME2(0) NULL;
END
GO
