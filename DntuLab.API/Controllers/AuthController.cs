
using DntuLab.Api.Data;
using DntuLab.Api.Domain;
using DntuLab.Api.DTOs;
using DntuLab.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LabDbContext _context;
    private readonly MatKhauService _matKhauService;
    private readonly JwtService _jwtService;

    public AuthController(
        LabDbContext context,
        MatKhauService matKhauService,
        JwtService jwtService)
    {
        _context = context;
        _matKhauService = matKhauService;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> DangNhap(
        [FromBody] DangNhapRequest request)
    {
        var tenDangNhap = request.TenDangNhap.Trim();

        var nguoiDung = await _context.NguoiDungs
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenDangNhap == tenDangNhap);

        if (nguoiDung == null ||
            !nguoiDung.HoatDong ||
            !_matKhauService.KiemTra(
                nguoiDung, request.MatKhau))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Tên đăng nhập hoặc mật khẩu không đúng."
            });
        }

        var token = _jwtService.TaoToken(nguoiDung);

        return Ok(new
        {
            success = true,
            message = "Đăng nhập thành công.",
            accessToken = token,
            tokenType = "Bearer",
            expiresInMinutes = 60,
            user = new
            {
                nguoiDung.Id,
                nguoiDung.TenDangNhap,
                nguoiDung.HoTen,
                nguoiDung.VaiTro
            }
        });
    }
}
