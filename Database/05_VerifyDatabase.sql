/*
DNTULAB - 05_VerifyDatabase.sql
Kiem tra phong, thiet bi, tai khoan va lich mau.
*/
USE [DntuLab];
GO

PRINT N'=== DATABASE ===';
SELECT DB_NAME() AS DatabaseHienTai;

PRINT N'=== CHECK CONSTRAINT PHONG ===';
SELECT name, definition, is_disabled, is_not_trusted
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.Phong')
  AND name IN (N'CK_Phong_Cum', N'CK_Phong_Loai');

PRINT N'=== PHONG ===';
SELECT MaPhong, LoaiPhong, Cum, SucChua
FROM dbo.Phong
ORDER BY CASE Cum WHEN 'F' THEN 1 WHEN 'G' THEN 2 WHEN 'AIBOT' THEN 3 ELSE 4 END, MaPhong;

PRINT N'=== THIET BI AIBOT ===';
SELECT MaThietBi, MaPhong, Loai, Ten, TrangThai
FROM dbo.ThietBi
WHERE MaPhong = N'AIBOT'
ORDER BY MaThietBi;

PRINT N'=== 4 TAI KHOAN ===';
SELECT Id, TenDangNhap, HoTen, VaiTro, HoatDong
FROM dbo.NguoiDung
WHERE TenDangNhap IN (N'gv01', N'gv02', N'ktv01', N'ql01')
ORDER BY TenDangNhap;

PRINT N'=== THONG KE LICH DAY ===';
SELECT ND.TenDangNhap, COUNT(LD.Id) AS TongCa
FROM dbo.NguoiDung ND
LEFT JOIN dbo.LichDay LD ON LD.GiangVienId = ND.Id
    AND LD.Ngay BETWEEN '20261020' AND '20261130'
WHERE ND.TenDangNhap IN (N'gv01', N'gv02')
GROUP BY ND.TenDangNhap;
GO
