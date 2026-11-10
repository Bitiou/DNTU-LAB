
using DntuLab.Api.Data;
using DntuLab.Api.Domain;
using DntuLab.Api.Services;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 1. CẤU HÌNH CORS CHO FRONTEND VERCEL
// ======================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("VercelFrontend", policy =>
    {
        policy
            .WithOrigins("https://dntu-lab.vercel.app")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ======================================================
// 2. CẤU HÌNH JWT AUTHENTICATION
// ======================================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Chưa cấu hình Jwt:Key.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.Zero
            };
    });

// ======================================================
// 3. CẤU HÌNH DATABASE POSTGRESQL
// ======================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<LabDbContext>(options =>
        options.UseNpgsql(
    connectionString,
    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorCodesToAdd: null
    )
    )
);
}
else
{
    throw new InvalidOperationException(
        "Chưa cấu hình ConnectionStrings:DefaultConnection.");
}

// ======================================================
// 4. ĐĂNG KÝ SERVICES
// ======================================================

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token vào đây."
    });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

builder.Services.AddScoped<MatKhauService>();
builder.Services.AddScoped<JwtService>();

// ======================================================
// 5. BUILD APPLICATION
// ======================================================

var app = builder.Build();

// ======================================================
// 6. TẠO TÀI KHOẢN MẪU
// ======================================================

// Chỉ thực hiện Seeder khi cấu hình bật.
// Chỉ bật khi database PostgreSQL đã được tạo schema và cần seed tài khoản mẫu.

var seedEnabled = builder.Configuration.GetValue<bool>(
    "Database:SeedOnStartup");

if (seedEnabled)
{
    try
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<LabDbContext>();

        var matKhauService = scope.ServiceProvider
            .GetRequiredService<MatKhauService>();

        var taiKhoans = new[]
        {
            new
            {
                TenDangNhap = "gv01",
                HoTen = "Giảng viên 01",
                VaiTro = Roles.GV,
                MatKhau = "Gv@123456"
            },

            new
            {
                TenDangNhap = "gv02",
                HoTen = "Giảng viên 02",
                VaiTro = Roles.GV,
                MatKhau = "Gv@123456"
            },

            new
            {
                TenDangNhap = "ktv01",
                HoTen = "Kỹ thuật viên 01",
                VaiTro = Roles.KTV,
                MatKhau = "Ktv@123456"
            },

            new
            {
                TenDangNhap = "ql01",
                HoTen = "Quản lý LAB",
                VaiTro = Roles.QL,
                MatKhau = "Ql@123456"
            }
        };

        foreach (var taiKhoan in taiKhoans)
        {
            bool daTonTai = await db.NguoiDungs
                .AnyAsync(x =>
                    x.TenDangNhap == taiKhoan.TenDangNhap);

            if (daTonTai)
                continue;

            var nguoiDung = new NguoiDung
            {
                TenDangNhap = taiKhoan.TenDangNhap,
                HoTen = taiKhoan.HoTen,
                VaiTro = taiKhoan.VaiTro,
                HoatDong = true
            };

            nguoiDung.MatKhauHash = matKhauService.TaoHash(
                nguoiDung,
                taiKhoan.MatKhau);

            db.NguoiDungs.Add(nguoiDung);
        }

        await db.SaveChangesAsync();

        app.Logger.LogInformation(
            "Khởi tạo tài khoản mẫu thành công.");
    }
    catch (NpgsqlException ex)
    {
        app.Logger.LogError(
            ex,
            "Không thể kết nối PostgreSQL để chạy Seeder.");
    }
}

// ======================================================
// 7. CONFIGURE HTTP REQUEST PIPELINE
// ======================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Render sử dụng HTTPS ở lớp proxy phía trước.
// Không chuyển hướng HTTP sang HTTPS tại ứng dụng.
app.UseDefaultFiles();
app.UseStaticFiles();

// CORS phải đứng trước Authentication và Authorization.
app.UseCors("VercelFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ======================================================
// 8. RUN APPLICATION
// ======================================================

app.Run();
