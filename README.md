# DNTU LAB -- Database

Tài liệu hướng dẫn khởi tạo database, dữ liệu phòng/thiết bị và lịch
giảng dạy mẫu cho hệ thống quản lý phòng LAB Khoa CNTT -- DNTU.

## 1. Thông tin chung

-   **Database:** `DntuLab`
-   **DBMS:** Microsoft SQL Server
-   **Collation:** `Vietnamese_CI_AS`
-   **Công nghệ backend:** ASP.NET Core / Entity Framework Core
-   **Bộ script:** 3 file SQL, phải chạy theo đúng thứ tự.

## 2. Cấu trúc thư mục

``` text
Database/
├── 01_CreateDatabase.sql
├── 02_SeedData.sql
├── 03_SeedLichDay.sql
└── README.md
```

## 3. Yêu cầu trước khi chạy

-   Đã cài Microsoft SQL Server.
-   Có SQL Server Management Studio (SSMS) hoặc công cụ tương đương.
-   Tài khoản kết nối SQL Server có quyền tạo database, bảng, index và
    thêm/cập nhật dữ liệu.
-   Backend DNTU LAB có cấu hình kết nối đến database `DntuLab`.
-   Tài khoản `gv01` và `gv02` phải được Backend `DevSeeder` tạo trước
    khi chạy file `03_SeedLichDay.sql`.

> **Lưu ý:** Các script này dùng để tạo schema và dữ liệu mẫu. Không
> chạy đồng thời với EF Core Migrations. `01_CreateDatabase.sql` được
> thiết kế cho database mới, chưa có các bảng tương ứng.

## 4. Thứ tự cài đặt

### Bước 1 -- Tạo database và cấu trúc bảng

Mở `01_CreateDatabase.sql` trong SSMS, chọn đúng SQL Server instance rồi
nhấn **Execute** hoặc `F5`.

File này: - Tạo database `DntuLab` nếu database chưa tồn tại. - Tạo 9
bảng: `NguoiDung`, `Phong`, `ThietBi`, `LichDay`, `NhatKyCheckIn`,
`TicketSuCo`, `YeuCauDoiPhong`, `ThongBao`, `GhiNhanTam`. - Tạo primary
key, foreign key, CHECK constraint, default constraint và index. - Không
thêm dữ liệu phòng, thiết bị hoặc lịch mẫu.

### Bước 2 -- Khởi tạo phòng và thiết bị

Mở `02_SeedData.sql` và nhấn **Execute**.

File này tạo/cập nhật danh sách phòng và bổ sung thiết bị:

  -----------------------------------------------------------------------
  Khu vực                 Phòng                   Quy mô dữ liệu
  ----------------------- ----------------------- -----------------------
  Nhà F                   F507, F508, F509        50 máy tính/phòng, tổng
                                                  150 máy

  Nhà G                   G201--G209              Sức chứa 60
                                                  người/phòng; mỗi phòng
                                                  có TV, Wi-Fi và tủ sạc
                                                  laptop

  AIBOT                   AIBOT                   Sức chứa 80 người; 6 bộ
                                                  Kit Robot/STEM
  -----------------------------------------------------------------------

Script có transaction và kiểm tra mã phòng/thiết bị đã tồn tại để hạn
chế chèn trùng khi chạy lại. Script cập nhật thông tin phòng theo danh
sách định nghĩa và không xóa thiết bị đã có.

### Bước 3 -- Tạo tài khoản mẫu bằng Backend DevSeeder

Khởi chạy backend trong môi trường Development để `DevSeeder` tạo dữ
liệu tài khoản mẫu, trong đó phải có hai tài khoản:

-   `gv01`
-   `gv02`

Sau đó kiểm tra hai tài khoản đã tồn tại trong `dbo.NguoiDung` trước khi
tiếp tục.

Ví dụ kiểm tra:

``` sql
USE DntuLab;
GO

SELECT Id, TenDangNhap, HoTen, VaiTro, HoatDong
FROM dbo.NguoiDung
WHERE LOWER(TenDangNhap) IN (N'gv01', N'gv02');
GO
```

Nếu thiếu một trong hai tài khoản, không chạy bước 4; hãy kiểm tra cấu
hình và kết quả chạy `DevSeeder`.

### Bước 4 -- Tạo lịch giảng dạy mẫu

Mở `03_SeedLichDay.sql` và nhấn **Execute**.

File tạo lịch trong khoảng **20/10/2026 đến hết 30/11/2026**:

  Giảng viên   Ca dạy mỗi ngày   Thời gian
  ------------ ----------------- ----------------------------
  GV01         Ca 1 và Ca 3      07:00--09:00; 13:00--15:00
  GV02         Ca 2 và Ca 4      09:30--11:30; 15:30--17:30

-   42 ngày × 4 ca/ngày = **168 ca**.
-   Mỗi giảng viên có **84 ca**.
-   Môn học và phòng học được luân phiên theo danh sách trong file SQL.
-   Lịch mới có trạng thái `Chưa check-in`.
-   Script kiểm tra xung đột phòng và trùng giờ của giảng viên trước khi
    thêm lịch.
-   Có transaction; nếu phát sinh lỗi, transaction được rollback.
-   Có thể chạy lại mà không thêm lại lịch đã tồn tại cùng ngày, ca và
    giảng viên.

**Lưu ý về số lượng:** Con số 168 ca là số lịch dự kiến được tạo khi
database chưa có lịch trùng với các khóa kiểm tra. Nếu database đã có
lịch trước đó, số lượng thực tế có thể khác. Hãy xem truy vấn thống kê
cuối file để xác nhận.

## 5. Tóm tắt chức năng 9 bảng

  Bảng               Chức năng
  ------------------ ------------------------------------------------------
  `NguoiDung`        Tài khoản, thông tin người dùng và vai trò
  `Phong`            Danh sách phòng LAB, khu vực và sức chứa
  `ThietBi`          Thiết bị thuộc từng phòng và trạng thái thiết bị
  `LichDay`          Lịch giảng dạy, ca học, môn học, phòng và giảng viên
  `NhatKyCheckIn`    Nhật ký check-in/check-out của ca học
  `TicketSuCo`       Ticket sự cố thiết bị
  `YeuCauDoiPhong`   Yêu cầu đổi phòng và thông tin duyệt
  `ThongBao`         Thông báo gửi người dùng hoặc nhóm vai trò
  `GhiNhanTam`       Ghi nhận sự cố tạm theo ca học

## 6. Kiểm tra dữ liệu sau khi cài đặt

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

Cần có đủ 9 bảng nêu tại mục 5.

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

### 6.3. Kiểm tra lịch theo giảng viên

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

Nếu chưa có lịch nào khác và script tạo đủ lịch, kết quả mong đợi là mỗi
giảng viên có 84 ca, 42 ngày có lịch và 168 tiết (theo cách tính 2
tiết/ca trong script).

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

## 7. Quy tắc nghiệp vụ Check-in / Check-out theo giờ Demo

Phần này mô tả quy tắc vận hành của ứng dụng DNTU LAB khi sử dụng **Thời
gian Demo**. Đây là logic do Backend và giao diện xử lý; ba file SQL ở
mục 2 chỉ tạo cấu trúc và dữ liệu mẫu, không tự triển khai bộ đếm thời
gian.

### 7.1. Quy tắc Check-in

Với một ca học có giờ bắt đầu là `BatDau`:

-   Giảng viên được phép Check-in thủ công từ **30 phút trước giờ bắt
    đầu** đến trước mốc **30 phút sau giờ bắt đầu**.
-   Check-in trước hoặc đúng giờ bắt đầu được ghi nhận là đúng giờ
    (`CheckInMuon = 0`, `SoPhutTre = 0`).
-   Check-in sau giờ bắt đầu nhưng trước mốc `BatDau + 30 phút` được ghi
    nhận là muộn; số phút trễ được tính theo thời điểm Check-in thực tế.
-   Nếu đến đúng mốc `BatDau + 30 phút` mà chưa có nhật ký Check-in, hệ
    thống tự tạo Check-in muộn tại mốc đó (`CheckInMuon = 1`,
    `SoPhutTre = 30`). Sau đó giao diện chuyển sang trạng thái/nút
    Check-out.
-   Nếu giảng viên đã Check-in thủ công, hệ thống không tạo thêm nhật ký
    Check-in tự động.

Ví dụ ca `07:00–09:00`:

  -----------------------------------------------------------------------
  Thời gian Demo                      Hành vi
  ----------------------------------- -----------------------------------
  Trước 06:30                         Chưa mở Check-in

  06:30--06:59                        Giảng viên có thể Check-in sớm

  07:00--07:29                        Giảng viên có thể Check-in; nếu sau
                                      07:00 thì ghi nhận muộn

  07:30                               Nếu chưa Check-in, hệ thống tự ghi
                                      nhận Check-in muộn và chuyển sang
                                      Check-out
  -----------------------------------------------------------------------

### 7.2. Quy tắc Check-out

Sau khi ca học có nhật ký Check-in:

-   Giảng viên có thể chủ động Check-out; không phải đợi đến giờ kết
    thúc ca.
-   Nếu giảng viên Check-out trước hạn tự động, hệ thống lưu thời điểm
    Check-out thực tế và không tạo Check-out tự động lần nữa.
-   Nếu đến đúng mốc `KetThuc + 30 phút` mà nhật ký vẫn chưa có
    Check-out, hệ thống tự động Check-out tại mốc đó và chuyển ca sang
    `Đã kết thúc`.
-   Check-out tự động **không đồng nghĩa** với việc đã xác nhận tắt điện
    hoặc khóa cửa. Hai trường `DaTatDien` và `DaKhoaCua` phải để `NULL`
    nếu chưa có xác nhận thực tế.
-   Sau khi đã Check-out, không cho tạo thêm nhật ký Check-out thứ hai
    cho cùng ca.

Ví dụ ca `07:00–09:00`:

  -----------------------------------------------------------------------
  Thời gian Demo                      Hành vi
  ----------------------------------- -----------------------------------
  07:30--09:29                        Có thể Check-out thủ công nếu ca đã
                                      Check-in

  09:30                               Nếu chưa Check-out, hệ thống tự
                                      Check-out và chuyển `Đã kết thúc`
  -----------------------------------------------------------------------

### 7.3. Áp dụng cho mọi ca và chuyển giờ Demo

-   Tính theo `Ngay`, `BatDau` và `KetThuc` của từng bản ghi `LichDay`;
    không hard-code riêng giờ sáng hoặc giờ chiều.
-   Cùng một quy tắc áp dụng cho ca sáng, chiều và các ngày khác.
-   Khi người dùng thay đổi ngày/giờ Demo rồi nhấn **Áp dụng giờ**, ứng
    dụng phải đánh giá lại các ca thuộc ngày Demo theo thời điểm mới.
-   Khi tua giờ Demo từ buổi chiều về buổi sáng hoặc từ ngày sau về ngày
    trước, không được để trạng thái của ca ở thời điểm tương lai làm sai
    trạng thái của ca tại thời điểm Demo hiện tại.
-   Cơ chế hoàn tác khi tua ngược chỉ phục vụ Development/Demo đối với
    các sự kiện tự động do mô phỏng tạo ra; không dùng việc tua giờ Demo
    để xóa nhật ký nghiệp vụ thật trên Production.

### 7.4. Ánh xạ với bảng `NhatKyCheckIn`

Bảng `NhatKyCheckIn` hiện có các cột phục vụ quy trình:

  Cột                  Ý nghĩa
  -------------------- -------------------------------------------------
  `LichDayId`          Ca học được ghi nhận; mỗi ca chỉ có một nhật ký
  `ThoiGianCheckIn`    Thời điểm Check-in thủ công hoặc tự động
  `CheckInMuon`        Đánh dấu Check-in muộn
  `SoPhutTre`          Số phút trễ so với `LichDay.BatDau`
  `ThoiGianCheckOut`   Thời điểm Check-out thủ công hoặc tự động
  `DaTatDien`          Xác nhận tắt điện; `NULL` nếu chưa xác nhận
  `DaKhoaCua`          Xác nhận khóa cửa; `NULL` nếu chưa xác nhận

**Lưu ý:** Quy tắc thời gian ở mục này là yêu cầu nghiệp vụ của ứng
dụng. Không cần thêm cột hoặc thay đổi schema chỉ để biểu diễn các mốc
30 phút; Backend phải thực hiện kiểm tra và cập nhật nhật ký đúng quy
tắc.

## 8. Lưu ý khi làm việc với Entity Framework Core

Database được tạo bằng SQL script nên cần giữ đồng bộ với Entity Models
và `LabDbContext` của backend.

Khi thay đổi model hoặc schema, cần kiểm tra: - Tên bảng, tên cột và
kiểu dữ liệu. - Primary key, foreign key và delete behavior. - Computed
columns cho các mã tự sinh. - `RowVersion` / concurrency token. - Index
thường và filtered unique index. - Default constraint và CHECK
constraint.

Không tự ý chạy migration trên database này nếu chưa xác định rõ quy
trình quản lý schema của dự án. Tránh để SQL script và EF Core cùng thay
đổi schema theo hai hướng độc lập.

## 9. Xử lý lỗi thường gặp

  ---------------------------------------------------------------------
  Hiện tượng                         Cách kiểm tra
  ---------------------------------- ----------------------------------
  Không tạo được database            Kiểm tra SQL Server instance và
                                     quyền tạo database

  Bảng đã tồn tại khi chạy file 01   File 01 dành cho database mới;
                                     kiểm tra database hiện tại trước
                                     khi chạy lại

  Không tìm thấy `gv01` hoặc `gv02`  Chạy backend Development và xác
                                     nhận `DevSeeder` đã tạo tài khoản

  Lỗi xung đột phòng khi tạo lịch    Kiểm tra lịch hiện có cùng ngày,
                                     phòng và khoảng thời gian

  Lỗi trùng giờ giảng viên           Kiểm tra lịch khác của cùng giảng
                                     viên trong ngày

  Chạy lại nhưng số lịch không tăng  Đây có thể là hành vi đúng vì
                                     script bỏ qua lịch đã tồn tại theo
                                     ngày/ca/giảng viên

  Backend không kết nối được         Kiểm tra server, instance,
                                     database, authentication và
                                     connection string
  ---------------------------------------------------------------------

## 10. Checklist triển khai

-   [ ] Chạy thành công `01_CreateDatabase.sql`.
-   [ ] Xác nhận database có đủ 9 bảng.
-   [ ] Chạy thành công `02_SeedData.sql`.
-   [ ] Xác nhận có 13 phòng, 150 máy tính nhà F, thiết bị nhà G và 6
    Kit AIBOT.
-   [ ] Chạy backend Development và xác nhận có `gv01`, `gv02`.
-   [ ] Chạy thành công `03_SeedLichDay.sql`.
-   [ ] Kiểm tra thống kê lịch theo từng giảng viên.
-   [ ] Kiểm tra API backend đọc được phòng, thiết bị và lịch dạy.
-   [ ] Kiểm thử quy tắc Check-in/Check-out theo giờ Demo tại mục 7, gồm
    tua giờ giữa sáng/chiều và tua ngược trong Development.

------------------------------------------------------------------------

**Ghi chú:** README này mô tả đúng phạm vi của ba script
`01_CreateDatabase.sql`, `02_SeedData.sql` và `03_SeedLichDay.sql` được
cung cấp. Các tài khoản mẫu `gv01`, `gv02` do Backend `DevSeeder` tạo,
không được tạo bởi ba file SQL này.
