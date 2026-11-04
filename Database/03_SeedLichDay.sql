/*
===============================================================
 DNTU LAB - 03_SeedLichDay.sql
 Tạo lịch mẫu GV01 và GV02 từ 20/10/2026 đến hết 30/11/2026.
 GV01: Ca 1 + Ca 3. GV02: Ca 2 + Ca 4.
 42 ngày x 4 ca = 168 ca; mỗi giảng viên 84 ca.

 ĐIỀU KIỆN:
 - Đã chạy 01_CreateDatabase.sql và 02_SeedData.sql.
 - Backend DevSeeder đã tạo dbo.NguoiDung với TenDangNhap gv01, gv02.
 - Chạy sau khi DevSeeder đã hoàn tất.
 - Có thể chạy lại; không thêm lịch cùng ngày/ca/giảng viên lần hai.
===============================================================
*/
USE DntuLab;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @GV01 INT = (SELECT Id FROM dbo.NguoiDung WHERE LOWER(TenDangNhap) = N'gv01');
DECLARE @GV02 INT = (SELECT Id FROM dbo.NguoiDung WHERE LOWER(TenDangNhap) = N'gv02');

IF @GV01 IS NULL OR @GV02 IS NULL
    THROW 50101, N'Không tìm thấy tài khoản gv01 và/hoặc gv02. Hãy chạy Backend DevSeeder trước.', 1;

DECLARE @BatDau DATE = '20261020';
DECLARE @KetThuc DATE = '20261130';
DECLARE @Ngay DATE = @BatDau;
DECLARE @NgayIndex INT;

DECLARE @MonGV01 TABLE (ThuTu INT PRIMARY KEY, MonHoc NVARCHAR(150), MaPhong NVARCHAR(20));
INSERT INTO @MonGV01 VALUES
(0, N'Lập trình Web API', N'F507'),
(1, N'An toàn thông tin', N'F509'),
(2, N'Phát triển ứng dụng di động', N'F508'),
(3, N'Học máy ứng dụng', N'G203'),
(4, N'Trí tuệ nhân tạo và Robotics', N'AIBOT');

DECLARE @MonGV02 TABLE (ThuTu INT PRIMARY KEY, MonHoc NVARCHAR(150), MaPhong NVARCHAR(20));
INSERT INTO @MonGV02 VALUES
(0, N'Cơ sở dữ liệu', N'G201'),
(1, N'Trí tuệ nhân tạo và Robotics', N'AIBOT'),
(2, N'Mạng máy tính', N'G202'),
(3, N'Cơ sở dữ liệu nâng cao', N'F508'),
(4, N'Phân tích thiết kế hệ thống', N'G204');

BEGIN TRY
    BEGIN TRANSACTION;

    WHILE @Ngay <= @KetThuc
    BEGIN
        SET @NgayIndex = DATEDIFF(DAY, @BatDau, @Ngay);

        /* GV01 - Ca 1: 07:00 đến 09:00 */
        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.LichDay
            WHERE Ngay = @Ngay AND Ca = N'Ca 1' AND GiangVienId = @GV01
        )
        BEGIN
            IF EXISTS
            (
                SELECT 1 FROM dbo.LichDay
                WHERE Ngay = @Ngay
                  AND MaPhong = (SELECT MaPhong FROM @MonGV01 WHERE ThuTu = @NgayIndex % 5)
                  AND BatDau < CAST('09:00' AS TIME)
                  AND KetThuc > CAST('07:00' AS TIME)
            )
                THROW 50102, N'Xung đột phòng GV01 Ca 1. Hãy xử lý lịch hiện có.', 1;

            IF EXISTS
            (
                SELECT 1 FROM dbo.LichDay
                WHERE Ngay = @Ngay AND GiangVienId = @GV01
                  AND BatDau < CAST('09:00' AS TIME)
                  AND KetThuc > CAST('07:00' AS TIME)
            )
                THROW 50106, N'GV01 bị trùng giờ Ca 1 với lịch khác.', 1;

            INSERT dbo.LichDay (Ngay, Ca, BatDau, KetThuc, MonHoc, MaPhong, GiangVienId, TrangThai, SoBuoi)
            SELECT @Ngay, N'Ca 1', '07:00', '09:00', MonHoc, MaPhong, @GV01, N'Chưa check-in', 1
            FROM @MonGV01 WHERE ThuTu = @NgayIndex % 5;
        END;

        /* GV02 - Ca 2: 09:30 đến 11:30 */
        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.LichDay
            WHERE Ngay = @Ngay AND Ca = N'Ca 2' AND GiangVienId = @GV02
        )
        BEGIN
            IF EXISTS
            (
                SELECT 1 FROM dbo.LichDay
                WHERE Ngay = @Ngay
                  AND MaPhong = (SELECT MaPhong FROM @MonGV02 WHERE ThuTu = @NgayIndex % 5)
                  AND BatDau < CAST('11:30' AS TIME)
                  AND KetThuc > CAST('09:30' AS TIME)
            )
                THROW 50103, N'Xung đột phòng GV02 Ca 2. Hãy xử lý lịch hiện có.', 1;

            IF EXISTS
            (
                SELECT 1 FROM dbo.LichDay
                WHERE Ngay = @Ngay AND GiangVienId = @GV02
                  AND BatDau < CAST('11:30' AS TIME)
                  AND KetThuc > CAST('09:30' AS TIME)
            )
                THROW 50107, N'GV02 bị trùng giờ Ca 2 với lịch khác.', 1;

            INSERT dbo.LichDay (Ngay, Ca, BatDau, KetThuc, MonHoc, MaPhong, GiangVienId, TrangThai, SoBuoi)
            SELECT @Ngay, N'Ca 2', '09:30', '11:30', MonHoc, MaPhong, @GV02, N'Chưa check-in', 1
            FROM @MonGV02 WHERE ThuTu = @NgayIndex % 5;
        END;

        /* GV01 - Ca 3: 13:00 đến 15:00 */
        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.LichDay
            WHERE Ngay = @Ngay AND Ca = N'Ca 3' AND GiangVienId = @GV01
        )
        BEGIN
            IF EXISTS
            (
                SELECT 1 FROM dbo.LichDay
                WHERE Ngay = @Ngay
                  AND MaPhong = (SELECT MaPhong FROM @MonGV01 WHERE ThuTu = (@NgayIndex + 1) % 5)
                  AND BatDau < CAST('15:00' AS TIME)
                  AND KetThuc > CAST('13:00' AS TIME)
            )
                THROW 50104, N'Xung đột phòng GV01 Ca 3. Hãy xử lý lịch hiện có.', 1;

            IF EXISTS
            (
                SELECT 1 FROM dbo.LichDay
                WHERE Ngay = @Ngay AND GiangVienId = @GV01
                  AND BatDau < CAST('15:00' AS TIME)
                  AND KetThuc > CAST('13:00' AS TIME)
            )
                THROW 50108, N'GV01 bị trùng giờ Ca 3 với lịch khác.', 1;

            INSERT dbo.LichDay (Ngay, Ca, BatDau, KetThuc, MonHoc, MaPhong, GiangVienId, TrangThai, SoBuoi)
            SELECT @Ngay, N'Ca 3', '13:00', '15:00', MonHoc, MaPhong, @GV01, N'Chưa check-in', 1
            FROM @MonGV01 WHERE ThuTu = (@NgayIndex + 1) % 5;
        END;

        /* GV02 - Ca 4: 15:30 đến 17:30 */
        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.LichDay
            WHERE Ngay = @Ngay AND Ca = N'Ca 4' AND GiangVienId = @GV02
        )
        BEGIN
            IF EXISTS
            (
                SELECT 1 FROM dbo.LichDay
                WHERE Ngay = @Ngay
                  AND MaPhong = (SELECT MaPhong FROM @MonGV02 WHERE ThuTu = (@NgayIndex + 1) % 5)
                  AND BatDau < CAST('17:30' AS TIME)
                  AND KetThuc > CAST('15:30' AS TIME)
            )
                THROW 50105, N'Xung đột phòng GV02 Ca 4. Hãy xử lý lịch hiện có.', 1;

            IF EXISTS
            (
                SELECT 1 FROM dbo.LichDay
                WHERE Ngay = @Ngay AND GiangVienId = @GV02
                  AND BatDau < CAST('17:30' AS TIME)
                  AND KetThuc > CAST('15:30' AS TIME)
            )
                THROW 50109, N'GV02 bị trùng giờ Ca 4 với lịch khác.', 1;

            INSERT dbo.LichDay (Ngay, Ca, BatDau, KetThuc, MonHoc, MaPhong, GiangVienId, TrangThai, SoBuoi)
            SELECT @Ngay, N'Ca 4', '15:30', '17:30', MonHoc, MaPhong, @GV02, N'Chưa check-in', 1
            FROM @MonGV02 WHERE ThuTu = (@NgayIndex + 1) % 5;
        END;

        SET @Ngay = DATEADD(DAY, 1, @Ngay);
    END;

    COMMIT TRANSACTION;
    PRINT N'Đã tạo lịch mẫu thành công.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Kết quả theo giảng viên trong toàn bộ giai đoạn. */
DECLARE @TuNgay DATE = '20261020';
DECLARE @DenNgay DATE = '20261130';

SELECT
    ND.TenDangNhap,
    COUNT(*) AS TongCa,
    COUNT(DISTINCT LD.Ngay) AS SoNgayCoLich,
    COUNT(*) * 2 AS TongTiet
FROM dbo.LichDay AS LD
INNER JOIN dbo.NguoiDung AS ND ON ND.Id = LD.GiangVienId
WHERE LOWER(ND.TenDangNhap) IN (N'gv01', N'gv02')
  AND LD.Ngay BETWEEN @TuNgay AND @DenNgay
GROUP BY ND.TenDangNhap
ORDER BY ND.TenDangNhap;
GO
