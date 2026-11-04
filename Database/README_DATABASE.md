# README_DATABASE -- DNTU LAB

Tài liệu hướng dẫn cài đặt và sử dụng bộ script SQL cho hệ thống quản lý
phòng LAB Khoa CNTT -- DNTU.

## 1. Thông tin database

  Thuộc tính     Giá trị
  -------------- --------------------------------------
  Tên database   `DntuLab`
  Hệ quản trị    Microsoft SQL Server
  Collation      `Vietnamese_CI_AS`
  Backend        ASP.NET Core / Entity Framework Core

## 2. Cấu trúc thư mục

``` text
Database/
├── 01_CreateDatabase.sql
├── 02_SeedData.sql
├── 03_SeedLichDay.sql
└── README_DATABASE.md
```

## 3. Yêu cầu

-   Microsoft SQL Server.
-   SQL Server Management Studio (SSMS) hoặc công cụ tương đương.
-   Tài khoản SQL Server có quyền tạo database, bảng, index và thêm/cập
    nhật dữ liệu.
-   Backend DNTU LAB đã có cấu hình kết nối đến database `DntuLab`.
-   Backend `DevSeeder` phải tạo tài khoản `gv01` và `gv02` trước khi
    chạy file `03_SeedLichDay.sql`.

> **Lưu ý:** `01_CreateDatabase.sql` dành cho database mới, chưa có các
> bảng tương ứng. Không chạy đồng thời bộ script này với EF Core
> Migrations.

## 4. Thứ tự cài đặt

### Bước 1 -- Tạo database và cấu trúc

1.  Mở SQL Server Management Studio.
2.  Kết nối đúng SQL Server instance.
3.  Mở `01_CreateDatabase.sql`.
4.  Nhấn **Execute** hoặc `F5`.
5.  Kiểm tra database `DntuLab` và các bảng đã được tạo.

File này tạo database nếu chưa tồn tại và tạo 9 bảng: `NguoiDung`,
`Phong`, `ThietBi`, `LichDay`, `NhatKyCheckIn`, `TicketSuCo`,
`YeuCauDoiPhong`, `ThongBao`, `GhiNhanTam`.

Ngoài bảng, file còn tạo primary key, foreign key, CHECK constraint,
default constraint và index. File không thêm dữ liệu phòng, thiết bị
hoặc lịch mẫu.

### Bước 2 -- Khởi tạo phòng và thiết bị

Mở `02_SeedData.sql` và nhấn **Execute**.

Dữ liệu được khởi tạo:

  -----------------------------------------------------------------------
  Khu vực                 Phòng                   Dữ liệu
  ----------------------- ----------------------- -----------------------
  Nhà F                   F507, F508, F509        50 máy tính/phòng, tổng
                                                  150 máy

  Nhà G                   G201--G209              Sức chứa 60
                                                  người/phòng; mỗi phòng
                                                  có TV, Wi-Fi, tủ sạc
                                                  laptop

  AIBOT                   AIBOT                   Sức chứa 80 người; 6 bộ
                                                  Kit Robot/STEM
  -----------------------------------------------------------------------

Script có transaction, cập nhật thông tin phòng theo danh sách định
nghĩa và kiểm tra mã phòng/thiết bị đã tồn tại để hạn chế chèn trùng khi
chạy lại. Script không xóa thiết bị đã có.

### Bước 3 -- Tạo tài khoản mẫu bằng Backend DevSeeder

Khởi chạy backend trong môi trường Development để `DevSeeder` tạo dữ
liệu tài khoản mẫu, trong đó cần có:

-   `gv01`
-   `gv02`

Kiểm tra tài khoản:

``` sql
USE DntuLab;
GO

SELECT Id, TenDangNhap, HoTen, VaiTro, HoatDong
FROM dbo.NguoiDung
WHERE LOWER(TenDangNhap) IN (N'gv01', N'gv02');
GO
```

Nếu thiếu một trong hai tài khoản, hãy kiểm tra cấu hình và kết quả chạy
`DevSeeder` trước khi tiếp tục.

### Bước 4 -- Tạo lịch giảng dạy mẫu

Mở `03_SeedLichDay.sql` và nhấn **Execute**.

Khoảng thời gian: **20/10/2026 đến hết 30/11/2026**.

  Giảng viên   Ca     Thời gian
  ------------ ------ --------------
  GV01         Ca 1   07:00--09:00
  GV01         Ca 3   13:00--15:00
  GV02         Ca 2   09:30--11:30
  GV02         Ca 4   15:30--17:30

-   42 ngày × 4 ca/ngày = 168 ca.
-   Mỗi giảng viên có 84 ca.
-   Môn học và phòng học luân phiên theo danh sách trong file SQL.
-   Lịch mới có trạng thái `Chưa check-in`.
-   Script kiểm tra xung đột phòng và trùng giờ giảng viên trước khi
    thêm lịch.
-   Script sử dụng transaction; nếu phát sinh lỗi, transaction được
    rollback.
-   Có thể chạy lại; lịch đã tồn tại cùng ngày, ca và giảng viên sẽ
    không được thêm lần nữa.

**Lưu ý:** 168 ca là số lịch dự kiến khi chưa có lịch trùng với các điều
kiện kiểm tra. Nếu database đã có lịch từ trước, số lượng thực tế có thể
khác. Hãy kiểm tra kết quả truy vấn ở cuối file.

## 5. Mô tả 9 bảng

  Bảng               Chức năng
  ------------------ ------------------------------------------------------
  `NguoiDung`        Tài khoản, thông tin người dùng và vai trò
  `Phong`            Danh sách phòng LAB, khu vực và sức chứa
  `ThietBi`          Thiết bị thuộc từng phòng và trạng thái
  `LichDay`          Lịch giảng dạy, ca học, môn học, phòng và giảng viên
  `NhatKyCheckIn`    Nhật ký check-in/check-out của ca học
  `TicketSuCo`       Ticket sự cố thiết bị
  `YeuCauDoiPhong`   Yêu cầu đổi phòng và thông tin duyệt
  `ThongBao`         Thông báo gửi người dùng hoặc nhóm vai trò
  `GhiNhanTam`       Ghi nhận sự cố tạm theo ca học

## 6. Truy vấn kiểm tra sau cài đặt

### 6.1. Kiểm tra danh sách bảng

``` sql
USE DntuLab;
GO

SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME;
GO
```

Kết quả cần có đủ 9 bảng ở mục 5.

### 6.2. Kiểm tra phòng và thiết bị

``` sql
SELECT MaPhong, LoaiPhong, Cum, SucChua
FROM dbo.Phong
ORDER BY Cum, MaPhong;

SELECT
    P.MaPhong,
    COUNT(T.MaThietBi) AS TongThietBi,
    SUM(CASE WHEN T.Loai = 'PC' THEN 1 ELSE 0 END) AS SoMayTinh,
    SUM(CASE WHEN T.Loai = 'TV' THEN 1 ELSE 0 END) AS SoTV,
    SUM(CASE WHEN T.Loai = 'WIFI' THEN 1 ELSE 0 END) AS SoWifi,
    SUM(CASE WHEN T.Loai = 'TUSAC' THEN 1 ELSE 0 END) AS SoTuSac,
    SUM(CASE WHEN T.Loai = 'KIT' THEN 1 ELSE 0 END) AS SoKit
FROM dbo.Phong P
LEFT JOIN dbo.ThietBi T ON T.MaPhong = P.MaPhong
GROUP BY P.MaPhong
ORDER BY P.MaPhong;
GO
```

### 6.3. Kiểm tra số lượng lịch theo giảng viên

``` sql
SELECT
    ND.TenDangNhap,
    COUNT(*) AS TongCa,
    COUNT(DISTINCT LD.Ngay) AS SoNgayCoLich,
    COUNT(*) * 2 AS TongTiet
FROM dbo.LichDay LD
INNER JOIN dbo.NguoiDung ND ON ND.Id = LD.GiangVienId
WHERE LOWER(ND.TenDangNhap) IN (N'gv01', N'gv02')
  AND LD.Ngay BETWEEN '20261020' AND '20261130'
GROUP BY ND.TenDangNhap
ORDER BY ND.TenDangNhap;
GO
```

Nếu database chưa có lịch trùng từ trước và script tạo đủ lịch, kết quả
mong đợi cho mỗi giảng viên là 84 ca, 42 ngày có lịch và 168 tiết (quy
ước 2 tiết/ca theo script).

### 6.4. Xem chi tiết lịch

``` sql
SELECT
    ND.TenDangNhap,
    LD.Ngay,
    LD.Ca,
    LD.BatDau,
    LD.KetThuc,
    LD.MonHoc,
    LD.MaPhong,
    LD.TrangThai
FROM dbo.LichDay LD
INNER JOIN dbo.NguoiDung ND ON ND.Id = LD.GiangVienId
WHERE LOWER(ND.TenDangNhap) IN (N'gv01', N'gv02')
  AND LD.Ngay BETWEEN '20261020' AND '20261130'
ORDER BY LD.Ngay, LD.BatDau, ND.TenDangNhap;
GO
```

## 7. Lưu ý với Entity Framework Core

Cần giữ đồng bộ SQL schema với Entity Models và `LabDbContext`.

Khi thay đổi cấu trúc, kiểm tra: - Tên bảng, tên cột và kiểu dữ liệu. -
Primary key, foreign key và delete behavior. - Computed columns cho mã
tự sinh. - `RowVersion` / concurrency token. - Index thường và filtered
unique index. - Default constraint và CHECK constraint.

Không tự ý chạy migration trên database này nếu chưa xác định quy trình
quản lý schema của dự án. Tránh để SQL script và EF Core cùng thay đổi
schema theo hai hướng độc lập.

## 8. Xử lý lỗi thường gặp

  -----------------------------------------------------------------------
  Lỗi / hiện tượng                    Hướng xử lý
  ----------------------------------- -----------------------------------
  Không tạo được database             Kiểm tra SQL Server instance và
                                      quyền tạo database

  Bảng đã tồn tại khi chạy file 01    File 01 dành cho database mới; kiểm
                                      tra database trước khi chạy lại

  Không tìm thấy `gv01` hoặc `gv02`   Chạy backend Development và xác
                                      nhận `DevSeeder` đã tạo tài khoản

  Xung đột phòng khi tạo lịch         Kiểm tra lịch cùng ngày, phòng và
                                      khoảng thời gian

  Trùng giờ giảng viên                Kiểm tra lịch khác của cùng giảng
                                      viên trong ngày

  Chạy lại nhưng số lịch không tăng   Có thể lịch đã tồn tại theo
                                      ngày/ca/giảng viên nên script bỏ
                                      qua

  Backend không kết nối database      Kiểm tra server, instance,
                                      database, authentication và
                                      connection string
  -----------------------------------------------------------------------

## 9. Checklist triển khai

-   [ ] Chạy thành công `01_CreateDatabase.sql`.
-   [ ] Xác nhận database có đủ 9 bảng.
-   [ ] Chạy thành công `02_SeedData.sql`.
-   [ ] Xác nhận có 13 phòng.
-   [ ] Xác nhận nhà F có 150 máy tính.
-   [ ] Xác nhận nhà G có TV, Wi-Fi và tủ sạc laptop theo từng phòng.
-   [ ] Xác nhận AIBOT có 6 bộ Kit Robot/STEM.
-   [ ] Chạy backend Development và xác nhận có `gv01`, `gv02`.
-   [ ] Chạy thành công `03_SeedLichDay.sql`.
-   [ ] Kiểm tra thống kê lịch theo từng giảng viên.
-   [ ] Kiểm tra API backend đọc được phòng, thiết bị và lịch dạy.

------------------------------------------------------------------------

**Ghi chú:** Tài liệu này mô tả bộ ba script SQL được cung cấp. Tài
khoản `gv01`, `gv02` do Backend `DevSeeder` tạo, không được tạo bởi ba
file SQL.
