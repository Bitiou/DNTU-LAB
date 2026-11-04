using DntuLab.Api.Data;

using DntuLab.Api.Domain;
using DntuLab.Api.Services;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<LabDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
// Add services to the container.
builder.Services.AddAuthorization();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<LabDbContext>();

    var matKhauService = scope.ServiceProvider
        .GetRequiredService<MatKhauService>();

    var taiKhoans = new[]
    {
        new { TenDangNhap = "gv01", HoTen = "Giảng viên 01",
              VaiTro = Roles.GV, MatKhau = "Gv@123456" },

        new { TenDangNhap = "gv02", HoTen = "Giảng viên 02",
              VaiTro = Roles.GV, MatKhau = "Gv@123456" },

        new { TenDangNhap = "ktv01", HoTen = "Kỹ thuật viên 01",
              VaiTro = Roles.KTV, MatKhau = "Ktv@123456" },

        new { TenDangNhap = "ql01", HoTen = "Quản lý LAB",
              VaiTro = Roles.QL, MatKhau = "Ql@123456" }
    };

    foreach (var taiKhoan in taiKhoans)
    {
        bool daTonTai = await db.NguoiDungs
            .AnyAsync(x => x.TenDangNhap == taiKhoan.TenDangNhap);

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
            nguoiDung, taiKhoan.MatKhau);

        db.NguoiDungs.Add(nguoiDung);
    }

    await db.SaveChangesAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
