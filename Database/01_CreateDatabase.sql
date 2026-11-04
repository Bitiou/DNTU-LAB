/* =====================================================================
   HE THONG QUAN LY PHONG LAB KHOA CNTT - DNTU
   SQL Server - DATABASE SCHEMA
   EF Core: LabDbContext

   LUU Y:
   - Chi chay tren Database DntuLab moi, chua co cac bang.
   - Khong chay dong thoi voi EF Core Migrations.
   - Du lieu phong va thiet bi duoc nap trong 02_SeedData.sql.
   ===================================================================== */

IF DB_ID(N'DntuLab') IS NULL
BEGIN
    CREATE DATABASE DntuLab COLLATE Vietnamese_CI_AS;
END;
GO

USE DntuLab;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* =========================================================
   1. NGUOIDUNG
   ========================================================= */

CREATE TABLE dbo.NguoiDung
(
    Id INT IDENTITY(1,1) NOT NULL,

    TenDangNhap NVARCHAR(50) NOT NULL,
    MatKhauHash NVARCHAR(500) NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    VaiTro VARCHAR(5) NOT NULL,

    HoatDong BIT NOT NULL
        CONSTRAINT DF_NguoiDung_HoatDong DEFAULT (1),

    CONSTRAINT PK_NguoiDung
        PRIMARY KEY (Id),

    CONSTRAINT UQ_NguoiDung_TenDangNhap
        UNIQUE (TenDangNhap),

    CONSTRAINT CK_NguoiDung_VaiTro
        CHECK (VaiTro IN ('GV', 'KTV', 'QL'))
);
GO


/* =========================================================
   2. PHONG
   ========================================================= */

CREATE TABLE dbo.Phong
(
    MaPhong NVARCHAR(20) NOT NULL,
    LoaiPhong NVARCHAR(20) NOT NULL,
    Cum VARCHAR(10) NOT NULL,
    SucChua INT NOT NULL,

    CONSTRAINT PK_Phong
        PRIMARY KEY (MaPhong),

    CONSTRAINT CK_Phong_Loai
        CHECK (LoaiPhong IN (N'PC', N'BYOD', N'Lab AI')),

    CONSTRAINT CK_Phong_Cum
        CHECK (Cum IN ('F', 'G', 'AIBOT')),

    CONSTRAINT CK_Phong_SucChua
        CHECK (SucChua > 0)
);
GO


/* =========================================================
   3. THIETBI
   ========================================================= */

CREATE TABLE dbo.ThietBi
(
    MaThietBi NVARCHAR(30) NOT NULL,
    MaPhong NVARCHAR(20) NOT NULL,
    Loai VARCHAR(10) NOT NULL,
    Ten NVARCHAR(150) NOT NULL,

    TrangThai NVARCHAR(20) NOT NULL
        CONSTRAINT DF_ThietBi_TrangThai DEFAULT (N'Sẵn sàng'),

    CONSTRAINT PK_ThietBi
        PRIMARY KEY (MaThietBi),

    CONSTRAINT FK_ThietBi_Phong
        FOREIGN KEY (MaPhong)
        REFERENCES dbo.Phong(MaPhong),

    CONSTRAINT CK_ThietBi_Loai
        CHECK (Loai IN ('PC', 'TV', 'WIFI', 'TUSAC', 'KIT')),

    CONSTRAINT CK_ThietBi_TrangThai
        CHECK (
            TrangThai IN (
                N'Sẵn sàng',
                N'Hỏng',
                N'Đang bảo trì'
            )
        )
);
GO

CREATE INDEX IX_ThietBi_MaPhong
ON dbo.ThietBi(MaPhong);
GO


/* =========================================================
   4. LICHDAY
   ========================================================= */

CREATE TABLE dbo.LichDay
(
    Id INT IDENTITY(1,1) NOT NULL,

    MaLich AS
    (
        N'L' +
        CASE
            WHEN Id < 1000
                THEN RIGHT(N'000' + CAST(Id AS NVARCHAR(10)), 3)
            ELSE CAST(Id AS NVARCHAR(10))
        END
    ) PERSISTED,

    Ngay DATE NOT NULL,
    Ca NVARCHAR(10) NOT NULL,

    BatDau TIME(0) NOT NULL,
    KetThuc TIME(0) NOT NULL,

    MonHoc NVARCHAR(150) NOT NULL,
    MaPhong NVARCHAR(20) NOT NULL,
    GiangVienId INT NOT NULL,

    TrangThai NVARCHAR(20) NOT NULL
        CONSTRAINT DF_LichDay_TrangThai
        DEFAULT (N'Chưa check-in'),

    SoBuoi INT NOT NULL
        CONSTRAINT DF_LichDay_SoBuoi
        DEFAULT (1),

    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_LichDay
        PRIMARY KEY (Id),

    CONSTRAINT FK_LichDay_Phong
        FOREIGN KEY (MaPhong)
        REFERENCES dbo.Phong(MaPhong),

    CONSTRAINT FK_LichDay_GiangVien
        FOREIGN KEY (GiangVienId)
        REFERENCES dbo.NguoiDung(Id),

    CONSTRAINT CK_LichDay_Gio
        CHECK (BatDau < KetThuc),

    CONSTRAINT CK_LichDay_TrangThai
        CHECK (
            TrangThai IN (
                N'Chưa check-in',
                N'Đang diễn ra',
                N'Đã kết thúc'
            )
        )
);
GO

CREATE INDEX IX_LichDay_Phong_Ngay
ON dbo.LichDay(MaPhong, Ngay)
INCLUDE (BatDau, KetThuc, TrangThai);
GO

CREATE INDEX IX_LichDay_GV_Ngay
ON dbo.LichDay(GiangVienId, Ngay);
GO


/* =========================================================
   5. NHATKYCHECKIN
   Quan he 1-1 voi LichDay
   ========================================================= */

CREATE TABLE dbo.NhatKyCheckIn
(
    Id INT IDENTITY(1,1) NOT NULL,

    MaNhatKy AS
    (
        N'NK' +
        CASE
            WHEN Id < 1000
                THEN RIGHT(N'000' + CAST(Id AS NVARCHAR(10)), 3)
            ELSE CAST(Id AS NVARCHAR(10))
        END
    ) PERSISTED,

    LichDayId INT NOT NULL,

    ThoiGianCheckIn DATETIME2(0) NOT NULL,
    CheckInMuon BIT NOT NULL,

    SoPhutTre INT NOT NULL
        CONSTRAINT DF_NhatKy_SoPhutTre DEFAULT (0),

    ThoiGianCheckOut DATETIME2(0) NULL,

    DaTatDien BIT NULL,
    DaKhoaCua BIT NULL,

    CONSTRAINT PK_NhatKyCheckIn
        PRIMARY KEY (Id),

    CONSTRAINT FK_NhatKy_LichDay
        FOREIGN KEY (LichDayId)
        REFERENCES dbo.LichDay(Id),

    CONSTRAINT UQ_NhatKy_LichDay
        UNIQUE (LichDayId),

    CONSTRAINT CK_NhatKy_Gio
        CHECK (
            ThoiGianCheckOut IS NULL
            OR ThoiGianCheckOut >= ThoiGianCheckIn
        )
);
GO


/* =========================================================
   6. TICKETSUCO
   ========================================================= */

CREATE TABLE dbo.TicketSuCo
(
    Id INT IDENTITY(1,1) NOT NULL,

    MaTicket AS
    (
        N'TK' +
        CASE
            WHEN Id < 1000
                THEN RIGHT(N'000' + CAST(Id AS NVARCHAR(10)), 3)
            ELSE CAST(Id AS NVARCHAR(10))
        END
    ) PERSISTED,

    MaThietBi NVARCHAR(30) NOT NULL,

    LoaiLoi NVARCHAR(100) NOT NULL
        CONSTRAINT DF_Ticket_LoaiLoi
        DEFAULT (N'Sự cố thiết bị'),

    GhiChu NVARCHAR(1000) NULL,

    NguoiBaoId INT NOT NULL,

    ThoiGianTao DATETIME2(0) NOT NULL,

    TrangThai NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Ticket_TrangThai
        DEFAULT (N'Chờ xử lý'),

    KtvXuLyId INT NULL,

    ThoiGianCapNhat DATETIME2(0) NULL,
    ThoiGianHoanTat DATETIME2(0) NULL,

    CONSTRAINT PK_TicketSuCo
        PRIMARY KEY (Id),

    CONSTRAINT FK_Ticket_ThietBi
        FOREIGN KEY (MaThietBi)
        REFERENCES dbo.ThietBi(MaThietBi),

    CONSTRAINT FK_Ticket_NguoiBao
        FOREIGN KEY (NguoiBaoId)
        REFERENCES dbo.NguoiDung(Id),

    CONSTRAINT FK_Ticket_Ktv
        FOREIGN KEY (KtvXuLyId)
        REFERENCES dbo.NguoiDung(Id),

    CONSTRAINT CK_Ticket_TrangThai
        CHECK (
            TrangThai IN (
                N'Chờ xử lý',
                N'Đang sửa chữa',
                N'Báo cáo cấp trên',
                N'Đã khắc phục'
            )
        )
);
GO

CREATE INDEX IX_Ticket_TrangThai
ON dbo.TicketSuCo(TrangThai);
GO

CREATE INDEX IX_Ticket_MaThietBi
ON dbo.TicketSuCo(MaThietBi);
GO


/* =========================================================
   7. YEUCAUDOIPHONG
   ========================================================= */

CREATE TABLE dbo.YeuCauDoiPhong
(
    Id INT IDENTITY(1,1) NOT NULL,

    MaYeuCau AS
    (
        N'YC' +
        CASE
            WHEN Id < 1000
                THEN RIGHT(N'000' + CAST(Id AS NVARCHAR(10)), 3)
            ELSE CAST(Id AS NVARCHAR(10))
        END
    ) PERSISTED,

    LichDayId INT NOT NULL,
    GiangVienId INT NOT NULL,

    PhongCu NVARCHAR(20) NOT NULL,
    PhongMoi NVARCHAR(20) NOT NULL,

    LyDo NVARCHAR(500) NOT NULL,

    Loai NVARCHAR(20) NOT NULL,
    TrangThai NVARCHAR(20) NOT NULL,

    ThoiGianTao DATETIME2(0) NOT NULL,

    NguoiDuyetId INT NULL,
    ThoiGianDuyet DATETIME2(0) NULL,

    GhiChuDuyet NVARCHAR(500) NULL,

    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_YeuCauDoiPhong
        PRIMARY KEY (Id),

    CONSTRAINT FK_YeuCau_LichDay
        FOREIGN KEY (LichDayId)
        REFERENCES dbo.LichDay(Id),

    CONSTRAINT FK_YeuCau_GiangVien
        FOREIGN KEY (GiangVienId)
        REFERENCES dbo.NguoiDung(Id),

    CONSTRAINT FK_YeuCau_PhongCu
        FOREIGN KEY (PhongCu)
        REFERENCES dbo.Phong(MaPhong),

    CONSTRAINT FK_YeuCau_PhongMoi
        FOREIGN KEY (PhongMoi)
        REFERENCES dbo.Phong(MaPhong),

    CONSTRAINT FK_YeuCau_NguoiDuyet
        FOREIGN KEY (NguoiDuyetId)
        REFERENCES dbo.NguoiDung(Id),

    CONSTRAINT CK_YeuCau_Loai
        CHECK (Loai IN (N'Có kế hoạch', N'Khẩn cấp')),

    CONSTRAINT CK_YeuCau_TrangThai
        CHECK (
            TrangThai IN (
                N'Chờ duyệt',
                N'Tự động duyệt',
                N'Đã duyệt',
                N'Từ chối'
            )
        ),

    CONSTRAINT CK_YeuCau_Phong
        CHECK (PhongCu <> PhongMoi)
);
GO

CREATE UNIQUE INDEX UX_YeuCau_ChoDuyet
ON dbo.YeuCauDoiPhong(LichDayId)
WHERE TrangThai = N'Chờ duyệt';
GO


/* =========================================================
   8. THONGBAO
   ========================================================= */

CREATE TABLE dbo.ThongBao
(
    Id INT IDENTITY(1,1) NOT NULL,

    MaThongBao AS
    (
        N'TB' +
        CASE
            WHEN Id < 1000
                THEN RIGHT(N'000' + CAST(Id AS NVARCHAR(10)), 3)
            ELSE CAST(Id AS NVARCHAR(10))
        END
    ) PERSISTED,

    VaiTroNhan VARCHAR(5) NOT NULL,

    NguoiNhan NVARCHAR(200) NOT NULL,
    NoiDung NVARCHAR(1000) NOT NULL,

    ThoiGian DATETIME2(0) NOT NULL,
    TrangThai NVARCHAR(30) NOT NULL,

    YeuCauId INT NULL,
    NguoiNhanId INT NULL,

    CONSTRAINT PK_ThongBao
        PRIMARY KEY (Id),

    CONSTRAINT FK_ThongBao_YeuCau
        FOREIGN KEY (YeuCauId)
        REFERENCES dbo.YeuCauDoiPhong(Id),

    CONSTRAINT FK_ThongBao_NguoiNhan
        FOREIGN KEY (NguoiNhanId)
        REFERENCES dbo.NguoiDung(Id)
);
GO

CREATE INDEX IX_ThongBao_VaiTro_TrangThai
ON dbo.ThongBao(VaiTroNhan, TrangThai);
GO

CREATE INDEX IX_ThongBao_NguoiNhanId
ON dbo.ThongBao(NguoiNhanId);
GO


/* =========================================================
   9. GHINHANTAM
   Ghi nhan su co tam theo ca - cum F
   ========================================================= */

CREATE TABLE dbo.GhiNhanTam
(
    Id INT IDENTITY(1,1) NOT NULL,

    MaGhiNhan AS
    (
        N'GN' +
        CASE
            WHEN Id < 1000
                THEN RIGHT(N'000' + CAST(Id AS NVARCHAR(10)), 3)
            ELSE CAST(Id AS NVARCHAR(10))
        END
    ) PERSISTED,

    LichDayId INT NOT NULL,
    MaThietBi NVARCHAR(30) NOT NULL,

    MoTa NVARCHAR(1000) NOT NULL,

    NguoiGhiId INT NOT NULL,

    ThoiGian DATETIME2(0) NOT NULL,

    TrangThai NVARCHAR(20) NOT NULL
        CONSTRAINT DF_GhiNhan_TrangThai
        DEFAULT (N'Chưa tổng hợp'),

    TicketId INT NULL,

    CONSTRAINT PK_GhiNhanTam
        PRIMARY KEY (Id),

    CONSTRAINT FK_GhiNhan_LichDay
        FOREIGN KEY (LichDayId)
        REFERENCES dbo.LichDay(Id),

    CONSTRAINT FK_GhiNhan_ThietBi
        FOREIGN KEY (MaThietBi)
        REFERENCES dbo.ThietBi(MaThietBi),

    CONSTRAINT FK_GhiNhan_NguoiGhi
        FOREIGN KEY (NguoiGhiId)
        REFERENCES dbo.NguoiDung(Id),

    CONSTRAINT FK_GhiNhan_Ticket
        FOREIGN KEY (TicketId)
        REFERENCES dbo.TicketSuCo(Id),

    CONSTRAINT CK_GhiNhan_TrangThai
        CHECK (
            TrangThai IN (
                N'Chưa tổng hợp',
                N'Đã tạo Ticket'
            )
        )
);
GO

CREATE INDEX IX_GhiNhan_Lich_TrangThai
ON dbo.GhiNhanTam(LichDayId, TrangThai);
GO


/* =========================================================
   10. KIEM TRA CAC BANG DA TAO
   ========================================================= */

SELECT
    TABLE_NAME AS TenBang
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME;
GO

PRINT N'Hoan tat tao cau truc Database DntuLab.';
PRINT N'Tiep theo hay chay file 02_SeedData.sql.';
GO