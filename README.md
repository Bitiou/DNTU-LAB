# DNTU LAB – Hướng dẫn Database PostgreSQL và dữ liệu Demo

Tài liệu hướng dẫn cài đặt PostgreSQL, tạo dữ liệu mẫu và kiểm thử các chức năng thời gian Demo của hệ thống quản lý phòng LAB Khoa CNTT – DNTU.

> **Công nghệ hiện tại:** ASP.NET Core, Entity Framework Core, PostgreSQL (Npgsql). Không sử dụng SQL Server/SSMS cho bộ script này.

## 1. Thông tin chung

| Thuộc tính | Giá trị |
|---|---|
| Database | `DntuLab` |
| DBMS | PostgreSQL |
| Schema | `public` |
| Backend | ASP.NET Core / Entity Framework Core |
| Provider | Npgsql |
| Múi giờ nghiệp vụ | Việt Nam – UTC+07:00 |
| Tài khoản Demo | `gv01`, `gv02`, `ktv01`, `ql01` |

Ví dụ cấu hình kết nối (thay mật khẩu bằng thông tin của môi trường đang chạy):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=DntuLab;Username=postgres;Password=<MAT_KHAU_POSTGRES>"
  }
}
```

Không commit mật khẩu thật, JWT key hoặc thông tin bí mật lên GitHub.

## 2. Cấu trúc thư mục Database

```text
Database/
├── 00_ResetDatabase_PostgreSQL.sql
├── 01_CreateSchema_PostgreSQL.ps1
├── 02_SeedData_PostgreSQL.sql
├── 03_SeedAccounts_PostgreSQL.sql
├── 04_SeedLichDay_PostgreSQL.sql
├── 05_Verify_PostgreSQL.sql
├── 06_SeedTickets_YeuCau_PostgreSQL.sql
└── README.md
```

| File | Chức năng |
|---|---|
| `00_ResetDatabase_PostgreSQL.sql` | Xóa và tạo lại schema `public`; chỉ dùng khi chủ động làm mới toàn bộ database |
| `01_CreateSchema_PostgreSQL.ps1` | Chạy EF Core Migration để tạo/cập nhật cấu trúc bảng |
| `02_SeedData_PostgreSQL.sql` | Tạo/cập nhật phòng và thiết bị mẫu |
| `03_SeedAccounts_PostgreSQL.sql` | Tạo/cập nhật bốn tài khoản Demo |
| `04_SeedLichDay_PostgreSQL.sql` | Tạo lịch giảng dạy mẫu GV01/GV02 |
| `05_Verify_PostgreSQL.sql` | Truy vấn kiểm tra dữ liệu |
| `06_SeedTickets_YeuCau_PostgreSQL.sql` | Tạo Ticket đỏ/vàng và yêu cầu đổi phòng mẫu có kế hoạch |

## 3. Yêu cầu trước khi cài đặt

- PostgreSQL Server đang hoạt động.
- Đã tạo database `DntuLab` (nếu chưa có, tạo trong pgAdmin hoặc chạy `CREATE DATABASE "DntuLab";`).
- Đã cài .NET SDK phù hợp với project.
- Project có thư mục `Migrations/` và cấu hình Npgsql.
- Connection string `DefaultConnection` trỏ đúng database.
- Tài khoản PostgreSQL có quyền thao tác trên schema `public`.

Cài EF Core CLI nếu máy chưa có:

```powershell
dotnet tool install --global dotnet-ef
```

Nếu đã cài, cập nhật công cụ tương thích với phiên bản EF Core của project:

```powershell
dotnet tool update --global dotnet-ef
```

**Giữ lại thư mục `Migrations/` trong source.** Đây là các migration dùng để tạo/cập nhật schema; không xóa thư mục này khi triển khai.

## 4. Thứ tự cài đặt database

### Bước 1 – Tạo database

Nếu database `DntuLab` chưa tồn tại, tạo database bằng pgAdmin hoặc công cụ PostgreSQL:

```sql
CREATE DATABASE "DntuLab";
```

Nếu database đã tồn tại và cần giữ dữ liệu, bỏ qua bước này.

### Bước 2 – (Tùy chọn) Reset schema `public`

Chỉ chạy `00_ResetDatabase_PostgreSQL.sql` khi muốn xóa toàn bộ bảng và dữ liệu trong schema `public`.

```sql
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;
```

**CẢNH BÁO – THAO TÁC PHÁ HỦY DỮ LIỆU:** Script này xóa tất cả bảng, dữ liệu, view, function và đối tượng khác trong schema `public`. Sao lưu trước khi chạy nếu cần giữ dữ liệu. Không chạy trên Production hoặc database đang được sử dụng thật.

Sau khi reset schema, bắt buộc thực hiện lại bước Migration ở Bước 3 rồi mới chạy các file seed.

### Bước 3 – Tạo/cập nhật schema bằng EF Core Migrations

Mở PowerShell tại thư mục chứa `DntuLab.API.csproj`, chạy:

```powershell
.\Database\01_CreateSchema_PostgreSQL.ps1
```

Nếu script nằm ở vị trí khác, điều chỉnh đường dẫn. Lệnh chính mà script thực hiện là:

```powershell
dotnet ef database update
```

Lệnh này áp dụng các migration hiện có trong `Migrations/` lên database PostgreSQL.

Nếu gặp lỗi:
- Kiểm tra đang đứng tại thư mục chứa file `.csproj`.
- Kiểm tra `DefaultConnection`, PostgreSQL service và database `DntuLab`.
- Kiểm tra thư mục `Migrations/` và phiên bản `dotnet-ef`.
- Không tự tạo migration mới nếu chỉ đang cài đặt database từ source hiện có.

### Bước 4 – Tạo phòng và thiết bị mẫu

Chạy `02_SeedData_PostgreSQL.sql` trên database `DntuLab`.

Dữ liệu theo script:

| Khu vực | Phòng | Dữ liệu thiết bị |
|---|---|---|
| Cụm F | F507, F508, F509 | 50 máy tính/phòng, tổng 150 máy |
| Cụm G | G201–G209 | Mỗi phòng có TV, Wi-Fi, tủ sạc laptop; tổng 27 thiết bị |
| AIBOT | AIBOT | 6 bộ Kit Robot/STEM |

Tổng cộng: **13 phòng và 183 thiết bị** theo danh sách seed.

Script có transaction. Phòng đã tồn tại được cập nhật theo dữ liệu định nghĩa; thiết bị có mã đã tồn tại được bỏ qua bằng `ON CONFLICT`. Script không xóa thiết bị hiện có.

### Bước 5 – Tạo tài khoản Demo

Chạy `03_SeedAccounts_PostgreSQL.sql`.

| Tên đăng nhập | Vai trò | Mật khẩu Demo |
|---|---|---|
| `gv01` | Giảng viên | `Gv@123456` |
| `gv02` | Giảng viên | `Gv@123456` |
| `ktv01` | Kỹ thuật viên | `Ktv@123456` |
| `ql01` | Quản lý LAB | `Ql@123456` |

Script dùng `INSERT ... ON CONFLICT DO UPDATE`, vì vậy tài khoản trùng tên sẽ được cập nhật hash mật khẩu, họ tên, vai trò và trạng thái hoạt động theo nội dung script.

Đây là tài khoản phục vụ Demo. Không dùng mật khẩu mẫu khi triển khai môi trường thật.

### Bước 6 – Tạo lịch giảng dạy mẫu

Chạy `04_SeedLichDay_PostgreSQL.sql` sau khi đã có `gv01` và `gv02`.

**Phạm vi ngày đúng theo nội dung SQL hiện tại: 20/10/2026 đến hết 30/11/2026.** Script tạo tối đa 42 ngày × 4 ca/ngày = **168 lịch**.

| Giảng viên | Ca | Thời gian |
|---|---|---|
| GV01 | Ca 1 | 07:00–09:00 |
| GV02 | Ca 2 | 09:30–11:30 |
| GV01 | Ca 3 | 13:00–15:00 |
| GV02 | Ca 4 | 15:30–17:30 |

Mỗi giảng viên có tối đa 84 ca. Môn học và phòng học luân phiên theo danh sách trong SQL. Lịch mới có trạng thái `Chưa check-in`.

Script:
- Bỏ qua lịch đã tồn tại cùng ngày, ca và giảng viên.
- Kiểm tra trùng phòng trong cùng khoảng thời gian.
- Kiểm tra trùng giờ của cùng giảng viên.
- Dùng transaction; lỗi xung đột sẽ làm transaction rollback.

Nếu chạy lại mà số lịch thêm mới bằng 0 và số dòng bỏ qua trùng bằng tổng số dòng import, điều đó có nghĩa các dòng được import đã khớp dữ liệu hiện có theo điều kiện chống trùng; không có lịch mới được thêm trong lần chạy đó.

**Không ghi phạm vi đến 31/01/2027:** file SQL hiện tại kết thúc ở 30/11/2026. README mô tả theo script thực tế, không tự mở rộng lịch sang tháng 12/2026 hoặc tháng 01/2027.

### Bước 7 – Tạo Ticket và yêu cầu đổi phòng mẫu

Chạy `06_SeedTickets_YeuCau_PostgreSQL.sql` sau các file seed phòng/thiết bị, tài khoản và lịch.

Ticket đỏ – `Chờ xử lý`:
- `F507-PC01`
- `F507-PC02`
- `F508-PC01`
- `G201-TV01`

Ticket vàng – `Đang sửa chữa`:
- `F509-PC01`
- `G202-WIFI`
- `AI-KIT01`

Script cập nhật trạng thái thiết bị tương ứng và tạo Ticket nếu thiết bị tồn tại, đồng thời chưa có Ticket chưa khắc phục cho thiết bị đó.

Script có thể tạo tối đa 4 yêu cầu đổi phòng **có kế hoạch** dựa trên lịch phù hợp từ 11/11/2026 đến 30/11/2026. Phòng đích phải cùng cụm F/G, khác phòng cũ và không trùng lịch. Dữ liệu mẫu có yêu cầu chờ duyệt và một yêu cầu từ chối.

Lưu ý: file SQL này không seed yêu cầu đổi phòng khẩn cấp hoặc thông báo hậu kiểm. Các dữ liệu đó thuộc luồng xử lý của ứng dụng/API hoặc chức năng Reset Demo, không nên ghi nhận là do file SQL này tạo ra.

### Bước 8 – Kiểm tra dữ liệu

Chạy `05_Verify_PostgreSQL.sql` để kiểm tra số lượng phòng, thiết bị, tài khoản, lịch, Ticket và yêu cầu đổi phòng; đồng thời xem thông tin phòng, thiết bị AIBOT và số lịch của GV01/GV02.

## 5. Quy tắc Check-in / Check-out theo giờ Demo

Phần này mô tả hành vi nghiệp vụ mong đợi của ứng dụng khi sử dụng thanh **Thời gian Demo**. Đây là logic do Backend và giao diện xử lý, không phải chức năng được thực thi bởi các file SQL seed.

### 5.1. Quy tắc Check-in

Với ca học có giờ bắt đầu `BatDau`:

- Mở Check-in từ **30 phút trước giờ bắt đầu**.
- Cho phép giảng viên Check-in thủ công đến trước mốc **30 phút sau giờ bắt đầu**.
- Check-in trước hoặc đúng giờ bắt đầu: đúng giờ.
- Check-in sau giờ bắt đầu nhưng trước mốc `BatDau + 30 phút`: ghi nhận muộn theo số phút thực tế.
- Đến đúng mốc `BatDau + 30 phút`, nếu chưa có Check-in, hệ thống tự tạo Check-in muộn tại mốc đó và chuyển sang trạng thái/nút Check-out.
- Nếu đã Check-in thủ công thì không tạo thêm Check-in tự động.

Ví dụ ca `07:00–09:00`:

| Thời gian Demo | Hành vi |
|---|---|
| Trước 06:30 | Chưa mở Check-in |
| 06:30–06:59 | Có thể Check-in sớm |
| 07:00–07:29 | Có thể Check-in; sau 07:00 được ghi nhận muộn |
| 07:30 | Nếu chưa Check-in, tự ghi nhận Check-in muộn và chuyển sang Check-out |

### 5.2. Quy tắc Check-out và kết thúc ca

- Sau khi Check-in, giảng viên có thể Check-out thủ công.
- Nếu đã Check-out thủ công, không tạo Check-out tự động lần nữa.
- Đến mốc `KetThuc + 30 phút`, nếu chưa Check-out, hệ thống tự Check-out và chuyển ca sang `Đã kết thúc`.
- Check-out tự động không đồng nghĩa đã xác nhận tắt điện hoặc khóa cửa. `DaTatDien` và `DaKhoaCua` phải giữ `NULL` nếu chưa có xác nhận thực tế.

Ví dụ ca `07:00–09:00`:

| Thời gian Demo | Hành vi |
|---|---|
| 07:30–09:29 | Có thể Check-out thủ công nếu đã Check-in |
| 09:30 | Nếu chưa Check-out, tự Check-out và chuyển `Đã kết thúc` |

### 5.3. Áp dụng cho mọi ca, chuyển ngày và tua giờ

- Luôn tính từ `Ngay`, `BatDau`, `KetThuc` của từng lịch; không hard-code riêng ca sáng hoặc ca chiều.
- Áp dụng cùng quy tắc cho các ca 07:00, 09:30, 13:00, 15:30 và các ca khác trong dữ liệu.
- Khi chuyển sang ngày khác hoặc tua tiến/tua lùi, trạng thái hiển thị phải được đánh giá lại theo thời điểm Demo mới.
- Không được giữ trạng thái tương lai rồi làm mất khả năng Check-in khi tua về thời điểm trước đó.
- Cơ chế hoàn tác sự kiện do mô phỏng tạo ra chỉ áp dụng trong Development/Demo; không xóa nhật ký nghiệp vụ thật ở Production.

**Ví dụ tình huống cần kiểm thử:** Ca 07:00–09:00 được Check-in thủ công lúc 07:05. Sau đó tua sang ngày khác, rồi quay lại đúng ngày lúc 07:05. Hệ thống phải khôi phục trạng thái tại 07:05 và hiển thị lại Check-in, không được giữ trạng thái Check-out từ lần tua trước. Nếu tua tới 07:30 thì chuyển sang Check-out; tới 09:30 thì tự kết thúc ca nếu chưa Check-out.

### 5.4. Các trường nhật ký liên quan

| Cột | Ý nghĩa |
|---|---|
| `LichDayId` | Ca học được ghi nhận |
| `ThoiGianCheckIn` | Thời điểm Check-in thủ công hoặc tự động |
| `CheckInMuon` | Đánh dấu Check-in muộn |
| `SoPhutTre` | Số phút trễ so với giờ bắt đầu |
| `ThoiGianCheckOut` | Thời điểm Check-out thủ công hoặc tự động |
| `DaTatDien` | Xác nhận tắt điện; `NULL` nếu chưa xác nhận |
| `DaKhoaCua` | Xác nhận khóa cửa; `NULL` nếu chưa xác nhận |

Không cần thêm cột chỉ để biểu diễn các mốc 30 phút; Backend phải tính từ thời gian bắt đầu/kết thúc của lịch.

## 6. Chức năng Reset Demo

Reset Demo là chức năng của ứng dụng/API, không phải thao tác chạy lại các file SQL seed. Theo yêu cầu nghiệp vụ của dự án, sau khi Reset Demo cần:

1. Khôi phục ngày và giờ Demo về thời gian thực tại Việt Nam (UTC+07:00).
2. Khôi phục trạng thái thiết bị mẫu.
3. Xóa Ticket cũ theo phạm vi dữ liệu Demo trước khi tạo lại Ticket mẫu đỏ/vàng.
4. Khôi phục các yêu cầu đổi phòng mẫu có kế hoạch và khẩn cấp.
5. Khôi phục thông báo hậu kiểm tương ứng với các yêu cầu khẩn cấp.

### 6.1. Thời gian trả về cho giao diện

Backend cần lấy thời gian thực tại Việt Nam từ thời gian UTC và chuyển sang múi giờ `Asia/Ho_Chi_Minh` (hoặc Windows timezone tương ứng `SE Asia Standard Time`).

Giá trị thời gian đầy đủ nên biểu diễn theo ISO 8601 có offset, ví dụ:

```text
2026-11-10T08:30:00+07:00
```

Trong đó `+07:00` thể hiện múi giờ Việt Nam. Giao diện cần cập nhật cả ngày Demo, giờ Demo và dữ liệu đang hiển thị theo kết quả Reset Demo; không chỉ đổi nhãn thời gian rồi giữ dữ liệu cũ trong localStorage.

### 6.2. Dữ liệu cần đồng bộ sau Reset Demo

Sau khi API reset thành công, giao diện cần tải lại dữ liệu từ PostgreSQL để cập nhật:
- Trạng thái thiết bị.
- Danh sách Ticket và các cột trạng thái Ticket.
- Yêu cầu đổi phòng có kế hoạch/khẩn cấp.
- Thông báo hậu kiểm.
- Nhật ký hoặc trạng thái ca học liên quan theo quy tắc Demo.

Không nên chỉ `reload` giao diện mà không đồng bộ dữ liệu từ API; cũng không nên chỉ xóa localStorage nếu dữ liệu thật đang được lưu ở PostgreSQL.

### 6.3. Phân biệt Reset Demo và seed SQL

- `00_ResetDatabase_PostgreSQL.sql`: xóa toàn bộ schema `public`; không phải nút Reset Demo.
- Các file `02`–`06`: khởi tạo dữ liệu ban đầu theo từng nhóm; chính sách chống trùng khác nhau tùy file.
- Reset Demo trong ứng dụng: khôi phục trạng thái dữ liệu mô phỏng theo yêu cầu nghiệp vụ mà không xóa toàn bộ schema/database.

**Lưu ý kiểm thử:** Sau khi reset, kiểm tra thời gian trả về có offset `+07:00`, số Ticket đỏ/vàng, số yêu cầu đổi phòng từng loại và thông báo hậu kiểm. Nếu API trả lỗi, không được thông báo reset thành công hoặc xóa trạng thái giao diện trước khi biết giao dịch phía server đã hoàn tất.

## 7. Mô tả các bảng nghiệp vụ chính

| Bảng | Chức năng |
|---|---|
| `NguoiDung` | Tài khoản, thông tin người dùng và vai trò |
| `Phong` | Danh sách phòng LAB, cụm phòng và sức chứa |
| `ThietBi` | Thiết bị thuộc phòng và trạng thái |
| `LichDay` | Lịch giảng dạy, ca học, môn học, phòng và giảng viên |
| `NhatKyCheckIn` | Nhật ký Check-in/Check-out của ca học |
| `TicketSuCo` | Ticket sự cố thiết bị |
| `YeuCauDoiPhong` | Yêu cầu đổi phòng và thông tin duyệt |
| `ThongBao` | Thông báo gửi người dùng hoặc nhóm vai trò |
| `GhiNhanTam` | Ghi nhận sự cố tạm theo ca học |

Cấu trúc thực tế được quản lý bởi Entity Models, `LabDbContext` và các migration trong `Migrations/`.

## 8. EF Core Migrations và đồng bộ schema

- Giữ thư mục `Migrations/` trong source project.
- `01_CreateSchema_PostgreSQL.ps1` dùng `dotnet ef database update`.
- Khi thay đổi Entity hoặc `LabDbContext`, cần đánh giá thay đổi schema và tạo migration theo quy trình dự án.
- Không xóa migration đã được áp dụng trên database dùng chung.
- Các file seed SQL không thay thế migration tạo cấu trúc bảng.
- Không để SQL thủ công và EF Core cùng quản lý một thay đổi schema theo hai hướng độc lập.

## 9. Xử lý lỗi thường gặp

| Hiện tượng | Cách kiểm tra |
|---|---|
| Không kết nối được PostgreSQL | Kiểm tra service, host, port, database, username, password và connection string |
| `database "DntuLab" does not exist` | Tạo database trước khi chạy migration |
| Không tìm thấy migration | Kiểm tra thư mục `Migrations/`, project startup và lệnh `dotnet ef` |
| Không tìm thấy tài khoản demo | Chạy `03_SeedAccounts_PostgreSQL.sql` |
| Seed lịch báo xung đột | Kiểm tra trùng phòng hoặc trùng giờ giảng viên |
| Import lịch thêm mới 0, bỏ qua trùng 80 | 80 dòng đã được nhận diện trùng; lần import đó không thêm lịch mới |
| Ticket mẫu không đủ | Kiểm tra mã thiết bị và Ticket chưa khắc phục hiện có |
| Không tạo được yêu cầu đổi phòng mẫu | Kiểm tra lịch phù hợp, cụm F/G và yêu cầu đã tồn tại |
| API trả HTTP 401 | Kiểm tra token đăng nhập, thời hạn token, header `Authorization` và quyền của endpoint; đây là lỗi xác thực/ủy quyền, không phải lỗi quy tắc thời gian Demo |
| Reset Demo báo thành công nhưng giao diện chưa đổi | Kiểm tra response API, cập nhật state ngày/giờ và gọi lại API tải Ticket/yêu cầu/thông báo |

## 10. Checklist triển khai và kiểm thử

### Cài đặt database
- [ ] PostgreSQL đang hoạt động.
- [ ] Database `DntuLab` đã được tạo.
- [ ] `DefaultConnection` trỏ đúng database.
- [ ] Source có thư mục `Migrations/`.
- [ ] Chạy `01_CreateSchema_PostgreSQL.ps1` thành công.
- [ ] Chạy `02_SeedData_PostgreSQL.sql` thành công.
- [ ] Kiểm tra 13 phòng và 183 thiết bị theo bộ seed.
- [ ] Chạy `03_SeedAccounts_PostgreSQL.sql` thành công.
- [ ] Chạy `04_SeedLichDay_PostgreSQL.sql` thành công.
- [ ] Chạy `06_SeedTickets_YeuCau_PostgreSQL.sql` thành công.
- [ ] Chạy `05_Verify_PostgreSQL.sql` và kiểm tra kết quả.

### Kiểm thử giờ Demo
- [ ] Trước giờ mở Check-in, chưa hiển thị thao tác Check-in.
- [ ] Từ `BatDau - 30 phút`, mở Check-in.
- [ ] Check-in sau giờ bắt đầu được tính trễ.
- [ ] Tại `BatDau + 30 phút`, tự Check-in muộn nếu chưa có nhật ký và chuyển sang Check-out.
- [ ] Tại `KetThuc + 30 phút`, tự Check-out và kết thúc ca nếu chưa Check-out.
- [ ] Tua sang ngày khác rồi quay lại đúng ngày/giờ đã Check-in thủ công: trạng thái phải được khôi phục đúng theo thời điểm tua.
- [ ] Tua tiến/lùi nhiều lần không tạo nhật ký Check-in/Check-out trùng.
- [ ] Check-out tự động không tự đánh dấu tắt điện/khóa cửa.

### Kiểm thử Reset Demo
- [ ] Ngày/giờ Demo trở về thời gian thực Việt Nam, định dạng ISO 8601 có `+07:00`.
- [ ] Trạng thái thiết bị được khôi phục.
- [ ] Ticket Demo cũ được xóa theo đúng phạm vi trước khi tạo lại Ticket đỏ/vàng.
- [ ] Yêu cầu đổi phòng có kế hoạch và khẩn cấp được khôi phục.
- [ ] Thông báo hậu kiểm tương ứng được khôi phục.
- [ ] Giao diện tải lại dữ liệu từ API/PostgreSQL, không chỉ dựa vào localStorage.
- [ ] Reset lỗi thì giao diện không hiển thị trạng thái thành công giả.

---

**Ghi chú phạm vi:** README này mô tả bộ script PostgreSQL được cung cấp và các quy tắc nghiệp vụ Demo đã thống nhất trong quá trình trao đổi. Các file SQL seed không tự thực thi logic thời gian Demo hoặc toàn bộ quy trình Reset Demo; các chức năng đó phải được triển khai và kiểm thử ở Backend/API và giao diện.
