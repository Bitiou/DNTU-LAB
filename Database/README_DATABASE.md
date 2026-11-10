# DNTULAB – HƯỚNG DẪN CÀI ĐẶT DATABASE POSTGRESQL

Tài liệu này hướng dẫn tạo cấu trúc và nạp dữ liệu mẫu cho hệ thống quản lý phòng LAB Khoa CNTT – DNTU bằng PostgreSQL, EF Core Migrations và các script SQL đi kèm.

> **Lưu ý quan trọng:** Dự án sử dụng **PostgreSQL**, không phải Microsoft SQL Server. Các script trong bộ này dùng cú pháp PostgreSQL và phải chạy trên đúng database `DntuLab`.

---

## 1. Thông tin môi trường

| Thuộc tính | Giá trị |
|---|---|
| Database | `DntuLab` |
| Hệ quản trị | PostgreSQL |
| Backend | ASP.NET Core / Entity Framework Core |
| Provider | Npgsql |
| Schema | `public` |
| Cấu hình kết nối | `ConnectionStrings:DefaultConnection` |

Ví dụ connection string trong `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=DntuLab;Username=postgres;Password=<MAT_KHAU_POSTGRES>"
  }
}
```

Thay `<MAT_KHAU_POSTGRES>` bằng mật khẩu PostgreSQL của môi trường đang chạy. Không đưa mật khẩu thật hoặc JWT key Production lên GitHub.

## 2. Danh sách file Database

Đặt các file trong thư mục `Database/` (hoặc giữ nguyên vị trí hiện tại, miễn là chạy đúng file):

```text
Database/
├── 00_ResetDatabase_PostgreSQL.sql
├── 01_CreateSchema_PostgreSQL.ps1
├── 02_SeedData_PostgreSQL.sql
├── 03_SeedAccounts_PostgreSQL.sql
├── 04_SeedLichDay_PostgreSQL.sql
├── 05_Verify_PostgreSQL.sql
├── 06_SeedTickets_YeuCau_PostgreSQL.sql
└── README_DATABASE.md
```

| File | Chức năng |
|---|---|
| `00_ResetDatabase_PostgreSQL.sql` | Xóa và tạo lại toàn bộ schema `public` – chỉ dùng khi muốn làm mới database hoàn toàn |
| `01_CreateSchema_PostgreSQL.ps1` | Chạy `dotnet ef database update` để EF Core tạo/cập nhật schema từ thư mục `Migrations` |
| `02_SeedData_PostgreSQL.sql` | Tạo 13 phòng và dữ liệu thiết bị mẫu |
| `03_SeedAccounts_PostgreSQL.sql` | Tạo/cập nhật 4 tài khoản demo và mật khẩu mẫu |
| `04_SeedLichDay_PostgreSQL.sql` | Tạo lịch mẫu cho GV01 và GV02 |
| `05_Verify_PostgreSQL.sql` | Kiểm tra số lượng và nội dung dữ liệu chính |
| `06_SeedTickets_YeuCau_PostgreSQL.sql` | Tạo trạng thái thiết bị và Ticket mẫu đỏ/vàng, đồng thời tạo nhóm yêu cầu đổi phòng mẫu có kế hoạch |

---

## 3. Yêu cầu trước khi cài đặt

- PostgreSQL Server đang hoạt động.
- Có database tên `DntuLab`. Nếu chưa có, tạo database này trong pgAdmin hoặc bằng lệnh PostgreSQL:

```sql
CREATE DATABASE "DntuLab";
```

- .NET SDK tương thích với project.
- Công cụ EF Core CLI (`dotnet-ef`) tương thích với phiên bản EF Core của project.
- Project có thư mục `Migrations/` chứa migration PostgreSQL.
- Connection string `DefaultConnection` trỏ đúng database `DntuLab`.
- Tài khoản PostgreSQL có quyền tạo/sửa đối tượng trong schema `public`.

Cài EF Core CLI nếu máy chưa có:

```bash
dotnet tool install --global dotnet-ef
```

Nếu đã cài, có thể cập nhật phiên bản tương thích với project:

```bash
dotnet tool update --global dotnet-ef
```

> Không tạo migration mới chỉ để chạy lại database. Dùng các migration đã có trong source hiện tại. Chỉ tạo migration khi mô hình Entity/DbContext thực sự thay đổi và đã kiểm tra schema.

---

## 4. Quy trình cài đặt database mới

Thực hiện theo thứ tự dưới đây.

### Bước 1 – (Tùy chọn) Reset schema `public`

**Chỉ thực hiện khi muốn xóa toàn bộ dữ liệu và cấu trúc hiện có trong schema `public`.**

Mở `00_ResetDatabase_PostgreSQL.sql` bằng pgAdmin Query Tool hoặc công cụ PostgreSQL tương đương, chọn database `DntuLab`, sau đó thực thi script.

Script chạy các lệnh:

```sql
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;
```

**CẢNH BÁO – THAO TÁC PHÁ HỦY DỮ LIỆU**

Lệnh trên xóa tất cả bảng, dữ liệu, view, function và đối tượng khác trong schema `public`. Sao lưu database trước khi chạy nếu cần giữ dữ liệu. Không chạy bước này trên Production hoặc database đang sử dụng thật.

Nếu database mới hoàn toàn và schema `public` chưa có dữ liệu, có thể bỏ qua bước này.

### Bước 2 – Tạo/cập nhật schema bằng EF Core Migrations

Mở PowerShell tại thư mục chứa file `DntuLab.API.csproj`, sau đó chạy:

```powershell
.\Database\01_CreateSchema_PostgreSQL.ps1
```

Nếu file script đang nằm trong thư mục khác, điều chỉnh đường dẫn tương ứng.

Nội dung script thực thi:

```powershell
dotnet ef database update
```

Lệnh này sử dụng các migration đã có trong thư mục `Migrations/` để tạo hoặc cập nhật bảng, khóa, index và các cấu trúc được định nghĩa trong migration.

**Giữ lại thư mục `Migrations/` trong source project.** Không xóa thư mục này khi cài đặt database.

Nếu lệnh thất bại:
- Kiểm tra đang chạy tại đúng thư mục chứa `.csproj`.
- Kiểm tra `ConnectionStrings:DefaultConnection`.
- Kiểm tra PostgreSQL đang chạy và database `DntuLab` đã tồn tại.
- Kiểm tra thư mục `Migrations/` có migration phù hợp.
- Kiểm tra phiên bản `dotnet-ef` tương thích với EF Core của project.

### Bước 3 – Tạo phòng và thiết bị mẫu

Chạy file `02_SeedData_PostgreSQL.sql` trên database `DntuLab`.

Dữ liệu được định nghĩa trong script:

| Khu vực | Phòng | Thiết bị |
|---|---|---|
| Cụm F | F507, F508, F509 | 50 máy tính/phòng, tổng 150 máy |
| Cụm G | G201–G209 | Mỗi phòng có TV, Wi-Fi và tủ sạc laptop; tổng 27 thiết bị |
| AIBOT | AIBOT | 6 bộ Kit Robot/STEM |

Tổng cộng theo dữ liệu mẫu: **13 phòng và 183 thiết bị**.

Script sử dụng transaction và `ON CONFLICT` để cập nhật thông tin phòng đã có, đồng thời bỏ qua thiết bị có mã đã tồn tại. Script không xóa các thiết bị hiện có.

### Bước 4 – Tạo tài khoản demo

Chạy file `03_SeedAccounts_PostgreSQL.sql`.

Bốn tài khoản được thiết lập:

| Tên đăng nhập | Vai trò | Mật khẩu demo |
|---|---|---|
| `gv01` | Giảng viên | `Gv@123456` |
| `gv02` | Giảng viên | `Gv@123456` |
| `ktv01` | Kỹ thuật viên | `Ktv@123456` |
| `ql01` | Quản lý | `Ql@123456` |

Script dùng `INSERT ... ON CONFLICT DO UPDATE`. Vì vậy, nếu tài khoản đã tồn tại, script sẽ cập nhật hash mật khẩu, họ tên, vai trò và trạng thái hoạt động theo dữ liệu mẫu trong file.

> Đây là tài khoản phục vụ Demo. Đổi mật khẩu trước khi triển khai môi trường thật.

### Bước 5 – Tạo lịch giảng dạy mẫu

Chạy file `04_SeedLichDay_PostgreSQL.sql` sau khi đã có `gv01` và `gv02`.

Phạm vi ngày thực tế trong script: **20/10/2026 đến hết 30/11/2026**.

Script định nghĩa 42 ngày × 4 ca/ngày = tối đa **168 lịch**:
- GV01: Ca 1 (07:00–09:00), Ca 3 (13:00–15:00).
- GV02: Ca 2 (09:30–11:30), Ca 4 (15:30–17:30).

Môn học và phòng học được luân phiên theo danh sách trong script. Lịch mới có trạng thái `Chưa check-in`.

Cơ chế chạy lại:
- Bỏ qua lịch đã tồn tại cùng ngày, ca và giảng viên.
- Kiểm tra trùng phòng theo ngày và khoảng thời gian.
- Kiểm tra trùng giờ của cùng giảng viên.
- Nếu phát hiện xung đột, script phát sinh lỗi thay vì âm thầm thêm lịch không hợp lệ.

**Phạm vi lịch không bao gồm tháng 12/2026 hoặc tháng 01/2027.** Muốn có lịch ở các tháng đó phải cập nhật chính file SQL tạo lịch trước khi chạy; README này không tự mở rộng dữ liệu.

### Bước 6 – Tạo Ticket và yêu cầu đổi phòng mẫu

Chạy file `06_SeedTickets_YeuCau_PostgreSQL.sql` sau khi đã chạy các file:
- `02_SeedData_PostgreSQL.sql`
- `03_SeedAccounts_PostgreSQL.sql`
- `04_SeedLichDay_PostgreSQL.sql`

Script yêu cầu đủ bốn tài khoản demo. Dữ liệu mẫu được định nghĩa:

**Ticket đỏ – Chờ xử lý**
- F507-PC01
- F507-PC02
- F508-PC01
- G201-TV01

**Ticket vàng – Đang sửa chữa**
- F509-PC01
- G202-WIFI
- AI-KIT01

Script cập nhật trạng thái thiết bị tương ứng và tạo Ticket nếu thiết bị đã tồn tại, đồng thời chưa có Ticket chưa khắc phục cho thiết bị đó.

Script còn tạo tối đa 4 yêu cầu đổi phòng **có kế hoạch** từ các lịch phù hợp trong khoảng 11/11/2026–30/11/2026. Phòng mới được chọn trong cùng cụm F hoặc G, phải khác phòng cũ và không bị trùng lịch. Các yêu cầu mẫu có trạng thái chờ duyệt hoặc từ chối theo dữ liệu trong script.

**Lưu ý:** File SQL hiện tại chỉ tạo yêu cầu đổi phòng có kế hoạch. File này không tạo yêu cầu đổi phòng khẩn cấp và không tạo thông báo hậu kiểm. Các chức năng đó, nếu cần dữ liệu mẫu, phải được tạo bằng quy trình/API tương ứng của ứng dụng.

Script có transaction và có điều kiện chống tạo trùng Ticket đang mở cũng như nhóm yêu cầu mang nhãn `DEMO-POSTGRESQL`. Script không xóa dữ liệu hiện có.

### Bước 7 – Kiểm tra dữ liệu

Chạy file `05_Verify_PostgreSQL.sql`.

File kiểm tra số dòng của các bảng:
- `Phong`
- `ThietBi`
- `NguoiDung`
- `LichDay`
- `TicketSuCo`
- `YeuCauDoiPhong`

Đồng thời hiển thị danh sách phòng, thiết bị AIBOT, tài khoản demo và số lịch của GV01/GV02 trong phạm vi 20/10/2026–30/11/2026.

---

## 5. Mô tả các bảng chính

| Bảng | Chức năng |
|---|---|
| `NguoiDung` | Tài khoản, thông tin người dùng và vai trò |
| `Phong` | Danh sách phòng LAB, cụm phòng và sức chứa |
| `ThietBi` | Thiết bị thuộc phòng và trạng thái thiết bị |
| `LichDay` | Lịch giảng dạy, ca học, môn học, phòng và giảng viên |
| `NhatKyCheckIn` | Nhật ký Check-in/Check-out của ca học |
| `TicketSuCo` | Ticket sự cố thiết bị |
| `YeuCauDoiPhong` | Yêu cầu đổi phòng và thông tin duyệt |
| `ThongBao` | Thông báo gửi người dùng hoặc nhóm vai trò |
| `GhiNhanTam` | Ghi nhận sự cố tạm theo ca học |

Cấu trúc thực tế được quản lý bởi Entity Models, `LabDbContext` và các migration trong `Migrations/`. Danh sách trên là các bảng nghiệp vụ chính của hệ thống.

---

## 6. Chạy lại script và lưu ý về dữ liệu trùng

Các script seed không có cùng một chính sách chạy lại:

| File | Hành vi khi chạy lại |
|---|---|
| `02_SeedData_PostgreSQL.sql` | Cập nhật thông tin phòng đã có; bỏ qua thiết bị trùng mã |
| `03_SeedAccounts_PostgreSQL.sql` | Cập nhật mật khẩu hash, họ tên, vai trò và trạng thái tài khoản demo |
| `04_SeedLichDay_PostgreSQL.sql` | Bỏ qua lịch trùng ngày/ca/giảng viên; dừng nếu gặp xung đột lịch |
| `06_SeedTickets_YeuCau_PostgreSQL.sql` | Không tạo Ticket chưa khắc phục trùng thiết bị; nhóm yêu cầu DEMO chỉ được tạo nếu chưa tồn tại |

Nếu thông báo import lịch cho biết tổng 80, thêm mới 0, bỏ qua trùng 80 thì có nghĩa 80 dòng đã được nhận diện là trùng theo logic của chức năng import; không có dòng nào được thêm mới trong lần đó.

> Không chạy `00_ResetDatabase_PostgreSQL.sql` chỉ để xử lý dữ liệu trùng. Reset schema sẽ xóa toàn bộ dữ liệu trong `public`, không chỉ dữ liệu Demo.

---

## 7. EF Core Migrations và đồng bộ schema

- Giữ thư mục `Migrations/` trong source project.
- `01_CreateSchema_PostgreSQL.ps1` sử dụng `dotnet ef database update`.
- Khi thay đổi Entity hoặc `LabDbContext`, cần đánh giá thay đổi schema và tạo migration mới theo quy trình dự án.
- Không tự ý xóa migration đã được áp dụng trên database dùng chung.
- Không để script SQL thủ công và EF Core Migrations cùng quản lý một thay đổi schema theo hai cách khác nhau.
- Các file `02` đến `06` là script nạp/kiểm tra dữ liệu, không thay thế migration tạo cấu trúc bảng.

---

## 8. Xử lý lỗi thường gặp

| Hiện tượng | Hướng xử lý |
|---|---|
| Không kết nối được PostgreSQL | Kiểm tra PostgreSQL service, host, port, database, username và password |
| `database "DntuLab" does not exist` | Tạo database `DntuLab` trước khi chạy migration |
| Không tìm thấy migration | Kiểm tra thư mục `Migrations/`, project startup và lệnh `dotnet ef` |
| `dotnet ef` không được nhận diện | Cài hoặc cập nhật công cụ `dotnet-ef` tương thích |
| Không tìm thấy `gv01` hoặc `gv02` | Chạy `03_SeedAccounts_PostgreSQL.sql` trước file tạo lịch |
| Không tìm thấy tài khoản demo khi seed Ticket | Chạy `03_SeedAccounts_PostgreSQL.sql` và kiểm tra đủ bốn tài khoản |
| Import lịch thêm mới 0, bỏ qua 80 | Các dòng import đã trùng với dữ liệu hiện có; kiểm tra khóa/điều kiện nhận diện trùng |
| Seed lịch báo xung đột phòng | Kiểm tra lịch cùng ngày, phòng và khoảng thời gian |
| Seed lịch báo trùng giờ giảng viên | Kiểm tra lịch khác của cùng giảng viên trong ngày |
| Seed Ticket không tạo đủ Ticket | Kiểm tra mã thiết bị có tồn tại và thiết bị có Ticket chưa khắc phục hay không |
| Không tạo yêu cầu đổi phòng mẫu | Kiểm tra lịch trong khoảng 11/11/2026–30/11/2026, cụm phòng F/G và yêu cầu đã tồn tại |

---

## 9. Checklist cài đặt

- [ ] PostgreSQL đang hoạt động.
- [ ] Đã tạo database `DntuLab`.
- [ ] Connection string `DefaultConnection` trỏ đúng database.
- [ ] Source có thư mục `Migrations/`.
- [ ] Chạy `01_CreateSchema_PostgreSQL.ps1` thành công.
- [ ] Chạy `02_SeedData_PostgreSQL.sql` thành công.
- [ ] Kiểm tra có 13 phòng và 183 thiết bị theo bộ dữ liệu mẫu.
- [ ] Chạy `03_SeedAccounts_PostgreSQL.sql` thành công.
- [ ] Kiểm tra có đủ `gv01`, `gv02`, `ktv01`, `ql01`.
- [ ] Chạy `04_SeedLichDay_PostgreSQL.sql` thành công.
- [ ] Kiểm tra lịch trong khoảng 20/10/2026–30/11/2026.
- [ ] Chạy `06_SeedTickets_YeuCau_PostgreSQL.sql` thành công.
- [ ] Kiểm tra Ticket đỏ/vàng và yêu cầu đổi phòng có kế hoạch.
- [ ] Chạy `05_Verify_PostgreSQL.sql` và kiểm tra kết quả.
- [ ] Khởi động backend và kiểm tra API đọc được dữ liệu PostgreSQL.

---

## 10. Thứ tự chạy nhanh

Nếu cài đặt database mới và không cần giữ dữ liệu cũ:

1. Tạo database `DntuLab` nếu chưa có.
2. (Tùy chọn) Chạy `00_ResetDatabase_PostgreSQL.sql` nếu chủ động muốn xóa sạch schema `public`.
3. Chạy `01_CreateSchema_PostgreSQL.ps1` trong thư mục project.
4. Chạy `02_SeedData_PostgreSQL.sql`.
5. Chạy `03_SeedAccounts_PostgreSQL.sql`.
6. Chạy `04_SeedLichDay_PostgreSQL.sql`.
7. Chạy `06_SeedTickets_YeuCau_PostgreSQL.sql`.
8. Chạy `05_Verify_PostgreSQL.sql`.

**Khuyến nghị:** Với database đang có dữ liệu sử dụng, không chạy bước reset. Chỉ chạy migration khi cần cập nhật schema và chỉ chạy seed khi muốn bổ sung dữ liệu mẫu.
