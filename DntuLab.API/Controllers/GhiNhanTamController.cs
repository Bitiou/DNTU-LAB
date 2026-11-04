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
public class GhiNhanTamController : ControllerBase
{
    private readonly LabDbContext _context;

    public GhiNhanTamController(LabDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Tao(
        [FromBody] TaoGhiNhanTamRequest request)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var nguoiGhiId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Không xác định được tài khoản."
            });
        }

        var lich = await _context.LichDays
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.LichDayId);

        if (lich == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy lịch dạy."
            });
        }

        if (lich.GiangVienId != nguoiGhiId)
        {
            return Forbid();
        }

        if (lich.TrangThai != TrangThaiLich.DangDienRa)
        {
            return Conflict(new
            {
                success = false,
                message = "Chỉ được ghi nhận sự cố khi buổi học đang diễn ra."
            });
        }

        // Chỉ áp dụng cho phòng thuộc cụm F.
        if (!new[] { "F507", "F508", "F509" }
            .Contains(lich.MaPhong))
        {
            return BadRequest(new
            {
                success = false,
                message = "Ghi nhận sự cố tạm chỉ áp dụng cho phòng F507–F509."
            });
        }

        var thietBi = await _context.ThietBis
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.MaThietBi == request.MaThietBi &&
                x.MaPhong == lich.MaPhong);

        if (thietBi == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy thiết bị trong phòng của lịch dạy."
            });
        }

        var ghiNhan = new GhiNhanTam
        {
            LichDayId = lich.Id,
            MaThietBi = thietBi.MaThietBi,
            MoTa = request.MoTa.Trim(),
            NguoiGhiId = nguoiGhiId,
            ThoiGian = DateTime.Now,
            TrangThai = "Chưa tổng hợp",
            TicketId = null
        };

        _context.GhiNhanTams.Add(ghiNhan);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetTheoLich),
            new { lichDayId = lich.Id },
            new
            {
                success = true,
                message = "Đã lưu ghi nhận sự cố tạm.",
                data = new
                {
                    ghiNhan.Id,
                    ghiNhan.LichDayId,
                    ghiNhan.MaThietBi,
                    ghiNhan.MoTa,
                    ghiNhan.ThoiGian,
                    ghiNhan.TrangThai,
                    ghiNhan.TicketId
                }
            });
    }


    [HttpPost("{id:int}/tong-hop")]
    public async Task<IActionResult> TongHop(int id)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var nguoiGhiId)) return Unauthorized();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var ghiNhan = await _context.GhiNhanTams.FirstOrDefaultAsync(x => x.Id == id);
        if (ghiNhan == null) return NotFound(new { success = false, message = "Không tìm thấy ghi nhận tạm." });
        var lich = await _context.LichDays.FirstOrDefaultAsync(x => x.Id == ghiNhan.LichDayId);
        if (lich == null) return NotFound(new { success = false, message = "Không tìm thấy lịch dạy." });
        if (lich.GiangVienId != nguoiGhiId) return Forbid();
        if (ghiNhan.TicketId.HasValue || ghiNhan.TrangThai == "Đã tạo Ticket")
            return Conflict(new { success = false, message = "Ghi nhận này đã được tổng hợp thành Ticket." });

        var thietBi = await _context.ThietBis.FirstOrDefaultAsync(x => x.MaThietBi == ghiNhan.MaThietBi);
        if (thietBi == null) return NotFound(new { success = false, message = "Không tìm thấy thiết bị." });
        var ticket = new TicketSuCo
        {
            MaThietBi = ghiNhan.MaThietBi,
            LoaiLoi = "Sự cố thiết bị",
            GhiChu = ghiNhan.MoTa,
            NguoiBaoId = nguoiGhiId,
            ThoiGianTao = DateTime.Now,
            TrangThai = TrangThaiTicket.ChoXuLy
        };
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();
        ghiNhan.TicketId = ticket.Id;
        ghiNhan.TrangThai = "Đã tạo Ticket";
        thietBi.TrangThai = TrangThaiThietBi.Hong;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { success = true, message = "Đã tổng hợp thành Ticket.", ticketId = ticket.Id });
    }

    [HttpGet("theo-lich/{lichDayId:int}")]
    public async Task<IActionResult> GetTheoLich(int lichDayId)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var nguoiGhiId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Không xác định được tài khoản."
            });
        }

        var lich = await _context.LichDays
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == lichDayId);

        if (lich == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy lịch dạy."
            });
        }

        if (lich.GiangVienId != nguoiGhiId)
        {
            return Forbid();
        }

        var data = await _context.GhiNhanTams
            .AsNoTracking()
            .Where(x => x.LichDayId == lichDayId)
            .OrderByDescending(x => x.ThoiGian)
            .Select(x => new
            {
                x.Id,
                x.MaGhiNhan,
                x.MaThietBi,
                TenThietBi = x.MaThietBi,
                x.MoTa,
                x.ThoiGian,
                x.TrangThai,
                x.TicketId
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

}
