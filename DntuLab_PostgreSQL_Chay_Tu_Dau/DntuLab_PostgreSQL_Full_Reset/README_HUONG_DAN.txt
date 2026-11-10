DNTULAB - BO DATABASE POSTGRESQL CHAY LAI TU DAU

QUAN TRONG
- Buoc 00 xoa toan bo schema public trong database DntuLab. Hay backup truoc.
- Khong chay cac file SQL Server cu tren PostgreSQL.
- Buoc 01 tao bang bang EF Core Migration, khong phai SQL thu cong.
- Phai dung source DntuLab.API PostgreSQL va co thu muc Migrations/.

THU TU THUC HIEN

A. RESET DATABASE
1. Mo pgAdmin -> Servers -> PostgreSQL -> Databases -> DntuLab.
2. Mo Query Tool, mo file 00_ResetDatabase_PostgreSQL.sql va Execute.

B. TAO LAI CAC BANG
3. Mo PowerShell tai thu muc DntuLab.API (noi co DntuLab.API.csproj).
4. Dam bao appsettings.json / user-secrets co ConnectionStrings:DefaultConnection tro den database DntuLab.
5. Chay file 01 bang lenh:
   .\01_CreateSchema_PostgreSQL.ps1
   Hoac chay truc tiep: dotnet ef database update
   Neu chua co migration PostgreSQL: dotnet ef migrations add InitialPostgreSql
   sau do chay lai dotnet ef database update.

C. NAP DU LIEU DEMO (pgAdmin Query Tool, database DntuLab)
6. 02_SeedData_PostgreSQL.sql       - 13 phong va thiet bi
7. 03_SeedAccounts_PostgreSQL.sql  - 4 tai khoan demo
8. 04_SeedLichDay_PostgreSQL.sql   - lich giang day GV01/GV02
9. 06_SeedTickets_YeuCau_PostgreSQL.sql - ticket va yeu cau doi phong mau
10. 05_Verify_PostgreSQL.sql       - kiem tra ket qua

TAI KHOAN DEMO
- gv01 / Gv@123456
- gv02 / Gv@123456
- ktv01 / Ktv@123456
- ql01 / Ql@123456

GHI CHU
- Buoc 00 la buoc nguy hiem, chi chay khi muon lam sach database.
- Buoc 01 can migration trong source code. Neu thu muc Migrations chua duoc commit/khong co, tao migration theo lenh o tren.
- Cac file 02-06 phai chay sau khi schema da duoc tao.
- 05 la file kiem tra, nen chay cuoi cung.
- Khong chay 00_ResetDatabase_PostgreSQL.sql sau khi da nap du lieu demo, vi se xoa sach du lieu.
