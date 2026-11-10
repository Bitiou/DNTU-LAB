
using System.Security.Claims;
using DntuLab.Api.Data;
using DntuLab.Api.Domain;
using DntuLab.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "GV")]
public class YeuCauDoiPhongController : ControllerBase
{
    private readonly LabDbContext _context;

    public YeuCauDoiPhongController(LabDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Tao(
        [FromBody] TaoYeuCauDoiPhongRequest request)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var giangVienId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Không xác định được tài khoản."
            });
        }

        var lich = await _context.LichDays
            .FirstOrDefaultAsync(x => x.Id == request.LichDayId);

        if (lich == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy lịch dạy."
            });
        }

        if (lich.GiangVienId != giangVienId)
        {
            return Forbid();
        }

        if (lich.Ngay < DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest(new
            {
                success = false,
                message = "Không thể yêu cầu đổi phòng cho lịch đã qua."
            });
        }

        var phongCu = lich.MaPhong;
        var phongMoi = request.PhongMoi.Trim();

        if (phongCu == phongMoi)
        {
            return BadRequest(new
            {
                success = false,
                message = "Phòng mới phải khác phòng hiện tại."
            });
        }

        var phongCuHopLe =
            phongCu.StartsWith("F") || phongCu.StartsWith("G");

        var phongMoiHopLe =
            phongMoi.StartsWith("F") || phongMoi.StartsWith("G");

        if (!phongCuHopLe || !phongMoiHopLe)
        {
            return BadRequest(new
            {
                success = false,
                message = "Chỉ hỗ trợ đổi phòng giữa các phòng thuộc khu F và G."
            });
        }

        var phongTonTai = await _context.Phongs
            .AnyAsync(x => x.MaPhong == phongMoi);

        if (!phongTonTai)
        {
            return NotFound(new
            {
                success = false,
                message = "Phòng mới không tồn tại."
            });
        }

        var daCoYeuCau = await _context.YeuCaus
            .AnyAsync(x =>
                x.LichDayId == lich.Id &&
                (x.TrangThai == "Chờ duyệt" ||
                 x.TrangThai == "Đã duyệt"));

        if (daCoYeuCau)
        {
            return Conflict(new
            {
                success = false,
                message = "Lịch dạy đã có yêu cầu đổi phòng đang được xử lý hoặc đã được duyệt."
            });
        }

        var yeuCau = new YeuCauDoiPhong
        {
            LichDayId = lich.Id,
            GiangVienId = giangVienId,
            PhongCu = phongCu,
            PhongMoi = phongMoi,
            LyDo = request.LyDo.Trim(),
            Loai = "Có kế hoạch",
            TrangThai = "Chờ duyệt",
            ThoiGianTao = DateTime.Now
        };

        _context.YeuCaus.Add(yeuCau);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Đã gửi yêu cầu đổi phòng.",
            id = yeuCau.Id,
            phongCu = yeuCau.PhongCu,
            phongMoi = yeuCau.PhongMoi,
            trangThai = yeuCau.TrangThai
        });
    }
}
