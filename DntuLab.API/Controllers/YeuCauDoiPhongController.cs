
using System.Security.Claims;
using DntuLab.Api.Data;
using DntuLab.Api.Domain;
using DntuLab.Api.DTOs;
using DntuLab.Api.Services;
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
    private readonly IWebHostEnvironment _environment;

    public YeuCauDoiPhongController(
        LabDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // GET: Giang vien chi xem cac yeu cau do chinh minh tao.
    [HttpGet]
    public async Task<IActionResult> GetCuaToi()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var giangVienId))
        {
            return Unauthorized(new { success = false, message = "Không xác định được tài khoản." });
        }

        var data = await _context.YeuCaus
            .AsNoTracking()
            .Where(x => x.GiangVienId == giangVienId)
            .OrderByDescending(x => x.ThoiGianTao)
            .Select(x => new
            {
                x.Id,
                x.MaYeuCau,
                x.LichDayId,
                x.GiangVienId,
                TenGiangVien = x.GiangVien.HoTen,
                MonHoc = x.LichDay.MonHoc,
                Ngay = x.LichDay.Ngay,
                Ca = x.LichDay.Ca,
                x.PhongCu,
                x.PhongMoi,
                x.LyDo,
                x.Loai,
                x.TrangThai,
                x.ThoiGianTao,
                x.ThoiGianDuyet,
                x.GhiChuDuyet
            })
            .ToListAsync();

        return Ok(new { success = true, count = data.Count, data });
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

        var hienTai = DemoClock.GetNow(Request, _environment);
        if (lich.Ngay < DateOnly.FromDateTime(hienTai))
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

        var yeuCauTonTai = await _context.YeuCaus
            .AsNoTracking()
            .Where(x =>
                x.LichDayId == lich.Id &&
                (x.TrangThai == "Chờ duyệt" ||
                 x.TrangThai == "Đã duyệt" ||
                 x.TrangThai == "Tự động duyệt"))
            .OrderByDescending(x => x.ThoiGianTao)
            .Select(x => new
            {
                x.Id,
                x.Loai,
                x.TrangThai,
                x.PhongCu,
                x.PhongMoi,
                x.GiangVienId
            })
            .FirstOrDefaultAsync();

        if (yeuCauTonTai != null)
        {
            return Conflict(new
            {
                success = false,
                message = $"Ca học đã có yêu cầu {yeuCauTonTai.Loai} {yeuCauTonTai.Id} ở trạng thái '{yeuCauTonTai.TrangThai}' ({yeuCauTonTai.PhongCu} → {yeuCauTonTai.PhongMoi}). Không thể tạo yêu cầu trùng cho cùng ca học.",
                yeuCauTonTai
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
            ThoiGianTao = hienTai
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
