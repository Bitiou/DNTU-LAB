using DntuLab.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DntuLab.Api.Domain;
using DntuLab.Api.Services;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "QL")]
public class ThongBaoController : ControllerBase
{
    private readonly LabDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ThongBaoController(LabDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _context.ThongBaos.AsNoTracking()
            .Where(x => x.VaiTroNhan == "QL")
            .OrderByDescending(x => x.ThoiGian)
            .Select(x => new { x.Id, x.MaThongBao, x.NguoiNhan, x.NoiDung, x.ThoiGian, x.TrangThai, x.YeuCauId })
            .ToListAsync();
        return Ok(new { success = true, count = data.Count, data });
    }
    /// <summary>QL xác nhận đã hậu kiểm yêu cầu đổi phòng khẩn cấp sau 08:00.</summary>
    [HttpPost("{id:int}/hau-kiem")]
    public async Task<IActionResult> XacNhanHauKiem(int id)
    {
        var now = DemoClock.GetNow(Request, _environment);
        if (TimeOnly.FromDateTime(now) < new TimeOnly(8, 0))
        {
            return BadRequest(new
            {
                success = false,
                message = "Chỉ xác nhận hậu kiểm từ 08:00 trở đi."
            });
        }

        var notification = await _context.ThongBaos
            .Include(x => x.YeuCau)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (notification == null)
            return NotFound(new { success = false, message = "Không tìm thấy thông báo." });

        if (notification.TrangThai != TrangThaiThongBao.ChoHauKiem)
            return Conflict(new { success = false, message = "Thông báo không còn ở trạng thái chờ hậu kiểm." });

        if (notification.YeuCau == null || notification.YeuCau.Loai != LoaiYeuCau.KhanCap)
            return BadRequest(new { success = false, message = "Thông báo không gắn với yêu cầu đổi phòng khẩn cấp." });

        notification.TrangThai = TrangThaiThongBao.DaHauKiem;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Đã xác nhận hậu kiểm yêu cầu đổi phòng khẩn cấp.",
            notificationId = notification.Id,
            trangThai = notification.TrangThai,
            thoiGianHauKiem = now
        });
    }

}
