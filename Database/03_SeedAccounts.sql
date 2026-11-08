/*
===============================================================================
DNTULAB - 03_SeedAccounts.sql
Tao 4 tai khoan demo, hash tuong thich ASP.NET Core Identity PasswordHasher V3.
MatKhauHash KHONG luu mat khau dang ro.
Idempotent: tai khoan da ton tai thi khong ghi de mat khau/trang thai hien tai.
Chay sau 01_CreateDatabase.sql va 02_SeedData.sql.
===============================================================================
*/
USE [DntuLab];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @TaiKhoan TABLE
    (
        TenDangNhap NVARCHAR(50) NOT NULL PRIMARY KEY,
        MatKhauHash NVARCHAR(500) NOT NULL,
        HoTen NVARCHAR(100) NOT NULL,
        VaiTro VARCHAR(5) NOT NULL
    );

    INSERT INTO @TaiKhoan (TenDangNhap, MatKhauHash, HoTen, VaiTro)
    VALUES
    (N'gv01',
     N'AQAAAAIAAYagAAAAEGuSWbbqd6eD0jAwLW8Yiow7fiSjTB3l4aDNw6S1xHcnmkYtU61gdZspPD+QhTt5MQ==',
     N'Giảng viên 01', 'GV'),
    (N'gv02',
     N'AQAAAAIAAYagAAAAEJwjzBDiRtCeBwifoHrRP0NtkIItcBjG6HSTZCcETG2NpTW3w6mr3s1not9P+FPzJg==',
     N'Giảng viên 02', 'GV'),
    (N'ktv01',
     N'AQAAAAIAAYagAAAAECkq2k/MA5Cni9uET14dhnRaWQmCuhOeYh6wO7nViqttDLVca4EOsfbgWusz/or7sw==',
     N'Kỹ thuật viên 01', 'KTV'),
    (N'ql01',
     N'AQAAAAIAAYagAAAAEGt0PEVLvx3J8+biII/pSLXtSLA7Qd+kEMEQF83PoIeX8kXc7LzGaejCEFCZ2Z3GxQ==',
     N'Quản lý LAB', 'QL');

    INSERT INTO dbo.NguoiDung
        (TenDangNhap, MatKhauHash, HoTen, VaiTro, HoatDong)
    SELECT
        S.TenDangNhap,
        S.MatKhauHash,
        S.HoTen,
        S.VaiTro,
        1
    FROM @TaiKhoan AS S
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.NguoiDung AS D
        WHERE D.TenDangNhap = S.TenDangNhap
    );

    COMMIT TRANSACTION;
    PRINT N'Da kiem tra va tao cac tai khoan demo con thieu.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- Kiem tra: khong hien thi MatKhauHash.
SELECT Id, TenDangNhap, HoTen, VaiTro, HoatDong
FROM dbo.NguoiDung
WHERE TenDangNhap IN (N'gv01', N'gv02', N'ktv01', N'ql01')
ORDER BY CASE TenDangNhap
    WHEN N'gv01' THEN 1
    WHEN N'gv02' THEN 2
    WHEN N'ktv01' THEN 3
    WHEN N'ql01' THEN 4
END;
GO
