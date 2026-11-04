
using System.Data;
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
public class CheckInController : ControllerBase
{
    private readonly LabDbContext _context;

    public CheckInController(LabDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request)
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

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

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

        var hienTai = DateTime.Now;
        var ngayHienTai = DateOnly.FromDateTime(hienTai);
        var gioHienTai = TimeOnly.FromDateTime(hienTai);

        if (lich.Ngay != ngayHienTai)
        {
            return BadRequest(new
            {
                success = false,
                message = "Lịch dạy không thuộc ngày hôm nay."
            });
        }

        if (!string.Equals(
            lich.MaPhong,
            request.MaPhong.Trim(),
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                success = false,
                message = "QR không khớp với phòng được phân công."
            });
        }

        var gioChoCheckIn = lich.BatDau.AddMinutes(-15);

        if (gioHienTai < gioChoCheckIn ||
            gioHienTai >= lich.KetThuc)
        {
            return BadRequest(new
            {
                success = false,
                message = "Chưa đến thời gian check-in hoặc lịch đã kết thúc."
            });
        }

        if (lich.TrangThai != TrangThaiLich.ChuaCheckIn)
        {
            return Conflict(new
            {
                success = false,
                message = "Lịch không ở trạng thái cho phép check-in."
            });
        }

        var daCheckIn = await _context.NhatKys
            .AnyAsync(x => x.LichDayId == lich.Id);

        if (daCheckIn)
        {
            return Conflict(new
            {
                success = false,
                message = "Lịch này đã có nhật ký check-in."
            });
        }

        var batDauDateTime = lich.Ngay.ToDateTime(lich.BatDau);
        var soPhutTre = Math.Max(
            0,
            (int)Math.Floor(
                (hienTai - batDauDateTime).TotalMinutes));

        var checkInMuon = soPhutTre > 15;

        var nhatKy = new NhatKyCheckIn
        {
            LichDayId = lich.Id,
            ThoiGianCheckIn = hienTai,
            CheckInMuon = checkInMuon,
            SoPhutTre = soPhutTre
        };

        _context.NhatKys.Add(nhatKy);

        // Chỉ cập nhật lịch sau khi lưu nhật ký thành công.
        await _context.SaveChangesAsync();

        lich.TrangThai = TrangThaiLich.DangDienRa;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            success = true,
            message = checkInMuon
                ? "Check-in thành công, giảng viên đến muộn."
                : "Check-in thành công.",
            data = new
            {
                lich.Id,
                lich.MaLich,
                lich.MaPhong,
                nhatKy.ThoiGianCheckIn,
                nhatKy.SoPhutTre,
                nhatKy.CheckInMuon,
                lich.TrangThai
            }
        });
    }
}
