# DNTULab API - PostgreSQL edition

Backend ASP.NET Core 8 da duoc chuyen provider tu SQL Server sang PostgreSQL. Cac controller va route API duoc giu nguyen; cac thay doi tap trung o EF Core/provider, model mapping va cau hinh ket noi.

## Thay doi chinh

- Thay `Microsoft.EntityFrameworkCore.SqlServer` bang `Npgsql.EntityFrameworkCore.PostgreSQL` 8.x.
- `UseSqlServer` -> `UseNpgsql`, retry policy tuong thich Npgsql.
- Computed columns ma nghiep vu dung cu phap PostgreSQL (`||`, `lpad`, quoted identifiers).
- Filtered unique index dung predicate PostgreSQL.
- `dbo.GhiNhanTam` chuyen sang schema `public`.
- `datetime2(0)` chuyen sang `timestamp(0) without time zone`.
- Hai truong concurrency `RowVersion` chuyen sang `uint` va map voi PostgreSQL system column `xmin` qua `IsRowVersion()`.
- Seeder bat loi `NpgsqlException`; connection string local/production khong con hard-code trong appsettings.
- Script fix AIBOT da chuyen sang PostgreSQL.

## 1. Tao migration PostgreSQL tren may co .NET 8 SDK

Mo terminal tai thu muc `DntuLab.API`:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet restore
dotnet ef migrations add InitialPostgreSql
```

Lenh tren tao thu muc `Migrations/`. Hay commit thu muc nay cung source code. Migration duoc sinh tu model PostgreSQL; khong chay cac script SQL Server cu (`01_CreateDatabase.sql`, `02_SeedData.sql`, ... ) tren PostgreSQL.

## 2. Tao database PostgreSQL va ap dung schema

Tao database trong nha cung cap PostgreSQL (vi du Neon). Lay connection string dang PostgreSQL, thuong co dang:

```text
Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
```

PowerShell local (chi cho terminal hien tai):

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
dotnet ef database update
```

Khong commit connection string that len GitHub.

## 3. Cau hinh Render

Trong Render -> backend service -> Environment, them:

- `ConnectionStrings__DefaultConnection` = connection string PostgreSQL cua nha cung cap.
- `Jwt__Key` = chuoi bi mat ngau nhien toi thieu 32 bytes.
- `Jwt__Issuer` = `DntuLab.API`
- `Jwt__Audience` = `DntuLab.Client`
- `Database__SeedOnStartup` = `false` (chi bat `true` neu muon tao tai khoan demo va da co schema).

Render build bang Dockerfile co san. Frontend Vercel khong can doi API contract; chi can cap nhat URL backend neu URL Render khac.

## 4. Du lieu SQL Server hien tai

Chuyen provider KHONG tu dong chuyen du lieu. File `.bak` cua SQL Server khong the restore truc tiep vao PostgreSQL. Can export/import du lieu 9 bang theo thu tu khoa ngoai, giu nguyen ID va sau do reset sequence/identity. Khong xoa database SQL Server cu; giu backup lam ban du phong.

Thu tu import goi y:

1. `NguoiDung`, `Phong`
2. `ThietBi`, `LichDay`
3. `NhatKyCheckIn`, `TicketSuCo`, `YeuCauDoiPhong`
4. `ThongBao`, `GhiNhanTam`

Khong import cac cot computed `MaLich`, `MaNhatKy`, `MaTicket`, `MaYeuCau`, `MaThongBao`, `MaGhiNhan`; PostgreSQL tu sinh tu `Id`.

## 5. Kiem tra sau khi migration

```powershell
dotnet build
dotnet ef database update
dotnet run
```

Kiem tra `GET /api/Database/test`, dang nhap, check-in/out, doi phong, ticket, import Excel va bao cao. Can test rieng cac transaction `Serializable` tren PostgreSQL.

## Gioi han kiem thu cua goi source nay

Moi truong tao ban chuyen doi khong co .NET SDK, nen chua the chay `dotnet restore`, `dotnet build` hoac sinh migration. Phai chay cac lenh o muc 1-2 tren may co .NET 8 SDK truoc khi deploy.
