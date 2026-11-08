/*
    DNTULAB - FIX AIBOT + 6 KIT
    Chạy một lần trong SQL Server Management Studio trên database DntuLab.
    Script idempotent: có thể chạy lại an toàn.
*/
USE [DntuLab];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Nới CHECK constraint cũ để chấp nhận cụm AIBOT.
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Phong')
      AND name = N'CK_Phong_Cum'
)
    ALTER TABLE dbo.Phong DROP CONSTRAINT CK_Phong_Cum;

ALTER TABLE dbo.Phong WITH CHECK
ADD CONSTRAINT CK_Phong_Cum
CHECK ([Cum] IN ('F', 'G', 'AIBOT'));

-- Đảm bảo loại phòng Lab AI được chấp nhận.
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Phong')
      AND name = N'CK_Phong_Loai'
)
    ALTER TABLE dbo.Phong DROP CONSTRAINT CK_Phong_Loai;

ALTER TABLE dbo.Phong WITH CHECK
ADD CONSTRAINT CK_Phong_Loai
CHECK ([LoaiPhong] IN (N'PC', N'BYOD', N'Lab AI'));

-- Tạo phòng AIBOT nếu chưa có.
IF NOT EXISTS (SELECT 1 FROM dbo.Phong WHERE MaPhong = 'AIBOT')
BEGIN
    INSERT INTO dbo.Phong (MaPhong, LoaiPhong, Cum, SucChua)
    VALUES ('AIBOT', N'Lab AI', 'AIBOT', 80);
END
ELSE
BEGIN
    UPDATE dbo.Phong
    SET LoaiPhong = N'Lab AI',
        Cum = 'AIBOT',
        SucChua = CASE WHEN SucChua <= 0 THEN 80 ELSE SucChua END
    WHERE MaPhong = 'AIBOT';
END;

-- Chuẩn hóa các mã KIT đã tồn tại nhưng khai báo nhầm phòng/loại.
-- Không ghi đè trạng thái thực tế của thiết bị.
UPDATE d
SET d.MaPhong = 'AIBOT',
    d.Loai = 'KIT',
    d.Ten = k.Ten
FROM dbo.ThietBi d
JOIN (VALUES
    ('AI-KIT01', N'Bộ Kit Robot & mô hình STEM 01'),
    ('AI-KIT02', N'Bộ Kit Robot & mô hình STEM 02'),
    ('AI-KIT03', N'Bộ Kit Robot & mô hình STEM 03'),
    ('AI-KIT04', N'Bộ Kit Robot & mô hình STEM 04'),
    ('AI-KIT05', N'Bộ Kit Robot & mô hình STEM 05'),
    ('AI-KIT06', N'Bộ Kit Robot & mô hình STEM 06')
) k(MaThietBi, Ten) ON k.MaThietBi = d.MaThietBi;

-- Thêm các KIT còn thiếu; trạng thái ban đầu là Sẵn sàng.
INSERT INTO dbo.ThietBi (MaThietBi, MaPhong, Loai, Ten, TrangThai)
SELECT k.MaThietBi, 'AIBOT', 'KIT', k.Ten, N'Sẵn sàng'
FROM (VALUES
    ('AI-KIT01', N'Bộ Kit Robot & mô hình STEM 01'),
    ('AI-KIT02', N'Bộ Kit Robot & mô hình STEM 02'),
    ('AI-KIT03', N'Bộ Kit Robot & mô hình STEM 03'),
    ('AI-KIT04', N'Bộ Kit Robot & mô hình STEM 04'),
    ('AI-KIT05', N'Bộ Kit Robot & mô hình STEM 05'),
    ('AI-KIT06', N'Bộ Kit Robot & mô hình STEM 06')
) k(MaThietBi, Ten)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ThietBi d WHERE d.MaThietBi = k.MaThietBi
);

COMMIT TRANSACTION;
GO

-- Kiểm tra kết quả:
SELECT MaPhong, LoaiPhong, Cum, SucChua
FROM dbo.Phong
WHERE MaPhong = 'AIBOT';

SELECT MaThietBi, MaPhong, Loai, Ten, TrangThai
FROM dbo.ThietBi
WHERE MaPhong = 'AIBOT'
ORDER BY MaThietBi;
GO
