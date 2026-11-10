using System;
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

        // Can tracked entity vi endpoint nay cap nhat TrangThai thiet bi.
        var thietBi = await _context.ThietBis
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
            ThoiGian = DateTime.UtcNow,
            TrangThai = "Chưa tổng hợp",
            TicketId = null
        };

        _context.GhiNhanTams.Add(ghiNhan);

        // Ghi nhận tạm tại cụm F phải làm thiết bị chuyển đỏ ngay trên sơ đồ.
        // Ticket chính thức vẫn chỉ được tạo khi giảng viên bấm "Tổng hợp & gửi Ticket".
        thietBi.TrangThai = TrangThaiThietBi.Hong;

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
        if (!int.TryParse(userIdValue, out var nguoiGhiId))
            return Unauthorized(new
            {
                success = false,
                message = "Không xác định được tài khoản."
            });

        var strategy = _context.Database.CreateExecutionStrategy();

        // Khai báo kiểu rõ ràng để ExecuteAsync chọn đúng overload Task<IActionResult>.
        Func<Task<IActionResult>> operation = async () =>
        {
            // Mỗi lần retry phải đọc lại dữ liệu mới từ database.
            _context.ChangeTracker.Clear();

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            var ghiNhan = await _context.GhiNhanTams
                .FirstOrDefaultAsync(x => x.Id == id);

            if (ghiNhan == null)
                return new NotFoundObjectResult(new
                {
                    success = false,
                    message = "Không tìm thấy ghi nhận tạm."
                });

            var lich = await _context.LichDays
                .FirstOrDefaultAsync(x => x.Id == ghiNhan.LichDayId);

            if (lich == null)
                return new NotFoundObjectResult(new
                {
                    success = false,
                    message = "Không tìm thấy lịch dạy."
                });

            if (lich.GiangVienId != nguoiGhiId)
                return new ForbidResult();

            var thietBi = await _context.ThietBis
                .FirstOrDefaultAsync(x => x.MaThietBi == ghiNhan.MaThietBi);

            if (thietBi == null)
                return new NotFoundObjectResult(new
                {
                    success = false,
                    message = "Không tìm thấy thiết bị."
                });

            TicketSuCo? ticket = null;
            var daTongHopTruocDo = ghiNhan.TicketId.HasValue ||
                                   ghiNhan.TrangThai == "Đã tạo Ticket";

            // Nếu đã có TicketId thì ưu tiên lấy đúng Ticket đã liên kết.
            if (ghiNhan.TicketId.HasValue)
            {
                ticket = await _context.Tickets
                    .FirstOrDefaultAsync(x => x.Id == ghiNhan.TicketId.Value);
            }

            // Phục hồi liên kết cho dữ liệu cũ đã đánh dấu tổng hợp nhưng thiếu TicketId.
            if (ticket == null && daTongHopTruocDo)
            {
                ticket = await _context.Tickets
                    .Where(x => x.MaThietBi == ghiNhan.MaThietBi &&
                                x.NguoiBaoId == nguoiGhiId &&
                                x.GhiChu == ghiNhan.MoTa)
                    .OrderByDescending(x => x.ThoiGianTao)
                    .FirstOrDefaultAsync();
            }

            if (ticket == null)
            {
                ticket = new TicketSuCo
                {
                    MaThietBi = ghiNhan.MaThietBi,
                    LoaiLoi = "Sự cố thiết bị",
                    GhiChu = ghiNhan.MoTa,
                    NguoiBaoId = nguoiGhiId,
                    ThoiGianTao = DateTime.Now,
                    TrangThai = TrangThaiTicket.ChoXuLy
                };

                _context.Tickets.Add(ticket);

                // Cần lưu lần đầu để lấy ID Ticket do PostgreSQL sinh.
                await _context.SaveChangesAsync();
            }

            ghiNhan.TicketId = ticket.Id;
            ghiNhan.TrangThai = "Đã tạo Ticket";

            // Giữ nguyên trạng thái nghiệp vụ của KTV; đồng bộ màu thiết bị theo Ticket.
            thietBi.TrangThai = ticket.TrangThai switch
            {
                TrangThaiTicket.DaKhacPhuc => TrangThaiThietBi.SanSang,
                TrangThaiTicket.DangSuaChua or TrangThaiTicket.BaoCaoCapTren
                    => TrangThaiThietBi.DangBaoTri,
                _ => TrangThaiThietBi.Hong
            };

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new OkObjectResult(new
            {
                success = true,
                alreadyAggregated = daTongHopTruocDo,
                message = daTongHopTruocDo
                    ? "Ghi nhận đã được tổng hợp trước đó. Đã đồng bộ lại Ticket và thiết bị."
                    : "Đã tổng hợp thành Ticket và cập nhật trạng thái thiết bị.",
                ghiNhanId = ghiNhan.Id,
                lichDayId = ghiNhan.LichDayId,
                ticketId = ticket.Id,
                maThietBi = ghiNhan.MaThietBi,
                trangThaiGhiNhan = ghiNhan.TrangThai,
                trangThaiTicket = ticket.TrangThai,
                trangThaiThietBi = thietBi.TrangThai
            });
        };

        return await strategy.ExecuteAsync(operation);
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
                x.LichDayId,
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
