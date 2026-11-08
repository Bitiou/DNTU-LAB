/*
===============================================================================
DNTULAB - 00_ResetDatabase.sql
CANH BAO: XOA TOAN BO DATABASE DntuLab VA TAT CA DU LIEU BEN TRONG.
Chi chay khi ban muon lam lai tu dau va da chap nhan mat du lieu cu.
===============================================================================
*/
USE [master];
GO

IF DB_ID(N'DntuLab') IS NOT NULL
BEGIN
    ALTER DATABASE [DntuLab] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [DntuLab];
    PRINT N'Da xoa database DntuLab cu.';
END
ELSE
BEGIN
    PRINT N'Database DntuLab chua ton tai; bo qua buoc xoa.';
END;
GO
