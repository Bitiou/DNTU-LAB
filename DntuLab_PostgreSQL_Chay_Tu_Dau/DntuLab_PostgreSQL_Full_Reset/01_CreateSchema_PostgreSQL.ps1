# DNTULAB POSTGRESQL - TAO CAC BANG BANG EF CORE MIGRATION
# Chay PowerShell tai thu muc chua file DntuLab.API.csproj.
# Yeu cau: da co thu muc Migrations/ voi migration PostgreSQL InitialPostgreSql.
# Neu chua co migration, tao migration truoc bang lenh:
# dotnet ef migrations add InitialPostgreSql

$ErrorActionPreference = 'Stop'

dotnet ef database update
if ($LASTEXITCODE -ne 0) {
    throw 'EF Core database update that bai. Kiem tra connection string va thu muc Migrations.'
}
Write-Host 'Da tao/cap nhat schema PostgreSQL thanh cong.' -ForegroundColor Green
