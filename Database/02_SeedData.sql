/*
===============================================================
 HỆ THỐNG QUẢN LÝ PHÒNG LAB KHOA CNTT - DNTU
 File: 02_SeedData.sql
 Database: DntuLab
 DBMS: Microsoft SQL Server

 NỘI DUNG:
 1. Khởi tạo danh sách phòng LAB.
 2. Cập nhật sức chứa phòng.
 3. Khởi tạo 50 máy tính cho mỗi phòng nhà F.
 4. Khởi tạo thiết bị nhà G.
 5. Khởi tạo bộ Kit Robot cho phòng AIBOT.

 QUY MÔ:
 - Nhà F: F507, F508, F509.
   50 máy tính/phòng, tổng 150 máy.
 - Nhà G: G201 đến G209.
   Sức chứa 60 người/phòng.
 - AIBOT: sức chứa 80 người.

 LƯU Ý:
 - Chạy sau 01_CreateDatabase.sql.
 - Có thể chạy lại, không thêm trùng phòng và thiết bị.
 - Không xóa dữ liệu thiết bị đã tồn tại.
===============================================================
*/

USE DntuLab;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* =======================================================
       1. KHỞI TẠO VÀ CẬP NHẬT DANH SÁCH PHÒNG
    ======================================================= */

    DECLARE @DanhSachPhong TABLE
    (
        MaPhong NVARCHAR(20) PRIMARY KEY,
        LoaiPhong NVARCHAR(20),
        Cum VARCHAR(10),
        SucChua INT
    );

    INSERT INTO @DanhSachPhong
        (MaPhong, LoaiPhong, Cum, SucChua)
    VALUES
        -- Nhà F: Phòng thực hành máy tính
        (N'F507', N'PC', 'F', 50),
        (N'F508', N'PC', 'F', 50),
        (N'F509', N'PC', 'F', 50),

        -- Nhà G: Phòng học lý thuyết / BYOD
        (N'G201', N'BYOD', 'G', 60),
        (N'G202', N'BYOD', 'G', 60),
        (N'G203', N'BYOD', 'G', 60),
        (N'G204', N'BYOD', 'G', 60),
        (N'G205', N'BYOD', 'G', 60),
        (N'G206', N'BYOD', 'G', 60),
        (N'G207', N'BYOD', 'G', 60),
        (N'G208', N'BYOD', 'G', 60),
        (N'G209', N'BYOD', 'G', 60),

        -- Trung tâm AI và Robotics
        (N'AIBOT', N'Lab AI', 'AIBOT', 80);

    -- Cập nhật thông tin các phòng đã tồn tại
    UPDATE P
    SET
        P.LoaiPhong = D.LoaiPhong,
        P.Cum = D.Cum,
        P.SucChua = D.SucChua
    FROM dbo.Phong P
    INNER JOIN @DanhSachPhong D
        ON P.MaPhong = D.MaPhong;

    -- Thêm các phòng chưa tồn tại
    INSERT INTO dbo.Phong
        (MaPhong, LoaiPhong, Cum, SucChua)
    SELECT
        D.MaPhong,
        D.LoaiPhong,
        D.Cum,
        D.SucChua
    FROM @DanhSachPhong D
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Phong P
        WHERE P.MaPhong = D.MaPhong
    );


    /* =======================================================
       2. KHỞI TẠO THIẾT BỊ NHÀ F
       50 máy tính/phòng
       Tổng cộng: 150 máy tính
    ======================================================= */

    ;WITH SoThuTu AS
    (
        SELECT 1 AS i

        UNION ALL

        SELECT i + 1
        FROM SoThuTu
        WHERE i < 50
    )
    INSERT INTO dbo.ThietBi
        (MaThietBi, MaPhong, Loai, Ten)
    SELECT
        P.MaPhong + N'-PC' +
            RIGHT(N'0' + CAST(S.i AS NVARCHAR(2)), 2),
        P.MaPhong,
        'PC',
        N'Máy tính ' +
            RIGHT(N'0' + CAST(S.i AS NVARCHAR(2)), 2)
    FROM dbo.Phong P
    CROSS JOIN SoThuTu S
    WHERE P.Cum = 'F'
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.ThietBi T
          WHERE T.MaThietBi =
              P.MaPhong + N'-PC' +
              RIGHT(N'0' + CAST(S.i AS NVARCHAR(2)), 2)
      )
    OPTION (MAXRECURSION 50);


    /* =======================================================
       3. KHỞI TẠO THIẾT BỊ NHÀ G

       Mỗi phòng có:
       - 01 TV trình chiếu
       - 01 hệ thống Wi-Fi
       - 01 tủ sạc laptop

       Tổng cộng:
       - 09 TV
       - 09 thiết bị Wi-Fi
       - 09 tủ sạc laptop
    ======================================================= */

    INSERT INTO dbo.ThietBi
        (MaThietBi, MaPhong, Loai, Ten)
    SELECT
        P.MaPhong + V.Hau,
        P.MaPhong,
        V.Loai,
        V.Ten
    FROM dbo.Phong P
    CROSS JOIN
    (
        VALUES
        (
            N'-TV01',
            'TV',
            N'Màn hình Tivi trình chiếu'
        ),
        (
            N'-WIFI',
            'WIFI',
            N'Hệ thống mạng không dây'
        ),
        (
            N'-TUSAC01',
            'TUSAC',
            N'Tủ sạc laptop'
        )
    ) V(Hau, Loai, Ten)
    WHERE P.Cum = 'G'
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.ThietBi T
          WHERE T.MaThietBi = P.MaPhong + V.Hau
      );


    /* =======================================================
       4. KHỞI TẠO THIẾT BỊ PHÒNG AIBOT

       06 bộ Kit Robot và mô hình STEM
    ======================================================= */

    ;WITH SoThuTu AS
    (
        SELECT 1 AS i

        UNION ALL

        SELECT i + 1
        FROM SoThuTu
        WHERE i < 6
    )
    INSERT INTO dbo.ThietBi
        (MaThietBi, MaPhong, Loai, Ten)
    SELECT
        N'AI-KIT' +
            RIGHT(N'0' + CAST(S.i AS NVARCHAR(2)), 2),
        N'AIBOT',
        'KIT',
        N'Bộ Kit Robot và mô hình STEM ' +
            RIGHT(N'0' + CAST(S.i AS NVARCHAR(2)), 2)
    FROM SoThuTu S
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.ThietBi T
        WHERE T.MaThietBi =
            N'AI-KIT' +
            RIGHT(N'0' + CAST(S.i AS NVARCHAR(2)), 2)
    )
    OPTION (MAXRECURSION 6);


    /* =======================================================
       5. HOÀN TẤT TRANSACTION
    ======================================================= */

    COMMIT TRANSACTION;

    PRINT N'Đã khởi tạo dữ liệu phòng và thiết bị thành công.';

END TRY
BEGIN CATCH

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;

END CATCH;


/* ===========================================================
   6. KIỂM TRA DANH SÁCH PHÒNG VÀ SỨC CHỨA
=========================================================== */

PRINT N'===== THỐNG KÊ PHÒNG LAB =====';

SELECT
    MaPhong,
    LoaiPhong,
    Cum,
    SucChua
FROM dbo.Phong
ORDER BY
    CASE Cum
        WHEN 'F' THEN 1
        WHEN 'G' THEN 2
        WHEN 'AIBOT' THEN 3
    END,
    MaPhong;


/* ===========================================================
   7. KIỂM TRA SỐ LƯỢNG THIẾT BỊ THEO PHÒNG
=========================================================== */

PRINT N'===== THỐNG KÊ THIẾT BỊ THEO PHÒNG =====';

SELECT
    P.MaPhong,
    P.Cum,
    P.SucChua,
    COUNT(T.MaThietBi) AS TongThietBi,

    SUM(CASE WHEN T.Loai = 'PC' THEN 1 ELSE 0 END)
        AS SoMayTinh,

    SUM(CASE WHEN T.Loai = 'TV' THEN 1 ELSE 0 END)
        AS SoTV,

    SUM(CASE WHEN T.Loai = 'WIFI' THEN 1 ELSE 0 END)
        AS SoWifi,

    SUM(CASE WHEN T.Loai = 'TUSAC' THEN 1 ELSE 0 END)
        AS SoTuSac,

    SUM(CASE WHEN T.Loai = 'KIT' THEN 1 ELSE 0 END)
        AS SoKit

FROM dbo.Phong P
LEFT JOIN dbo.ThietBi T
    ON P.MaPhong = T.MaPhong

GROUP BY
    P.MaPhong,
    P.Cum,
    P.SucChua

ORDER BY
    CASE P.Cum
        WHEN 'F' THEN 1
        WHEN 'G' THEN 2
        WHEN 'AIBOT' THEN 3
    END,
    P.MaPhong;


/* ===========================================================
   8. TỔNG KẾT QUY MÔ HỆ THỐNG
=========================================================== */

PRINT N'===== TỔNG KẾT QUY MÔ DNTU LAB =====';

SELECT
    Cum,
    COUNT(*) AS SoPhong,
    SUM(SucChua) AS TongSucChua
FROM dbo.Phong
GROUP BY Cum
ORDER BY Cum;

SELECT
    Cum,
    Loai,
    COUNT(*) AS SoLuong
FROM dbo.ThietBi T
INNER JOIN dbo.Phong P
    ON T.MaPhong = P.MaPhong
GROUP BY
    Cum,
    Loai
ORDER BY
    Cum,
    Loai;

GO
