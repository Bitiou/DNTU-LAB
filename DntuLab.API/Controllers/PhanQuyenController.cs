
using DntuLab.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhanQuyenController : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public IActionResult ThongTinTaiKhoan()
    {
        return Ok(new
        {
            success = true,
            id = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            tenDangNhap = User.Identity?.Name,
            vaiTro = User.FindFirst(
                System.Security.Claims.ClaimTypes.Role)?.Value
        });
    }

    [Authorize(Roles = Roles.GV)]
    [HttpGet("giang-vien")]
    public IActionResult QuyenGiangVien()
    {
        return Ok(new
        {
            success = true,
            message = "Bạn có quyền Giảng viên."
        });
    }

    [Authorize(Roles = Roles.KTV)]
    [HttpGet("ky-thuat-vien")]
    public IActionResult QuyenKyThuatVien()
    {
        return Ok(new
        {
            success = true,
            message = "Bạn có quyền Kỹ thuật viên."
        });
    }

    [Authorize(Roles = Roles.QL)]
    [HttpGet("quan-ly")]
    public IActionResult QuyenQuanLy()
    {
        return Ok(new
        {
            success = true,
            message = "Bạn có quyền Quản lý LAB."
        });
    }
}
