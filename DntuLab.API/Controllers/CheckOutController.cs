
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
public class CheckOutController : ControllerBase
{
    private readonly LabDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public CheckOutController(
        LabDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpPost]
    public async Task<IActionResult> CheckOut(
        [FromBody] CheckOutRequest request)
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

        // Thực hiện toàn bộ transaction bên trong Execution Strategy.
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
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

                if (lich.TrangThai != TrangThaiLich.DangDienRa)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "Lịch không ở trạng thái đang diễn ra."
                    });
                }

                var nhatKy = await _context.NhatKys
                    .FirstOrDefaultAsync(x => x.LichDayId == lich.Id);

                if (nhatKy == null)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "Không tìm thấy nhật ký check-in."
                    });
                }

                var thoiGianDemo =
                    DemoClock.GetNow(Request, _environment);

                if (nhatKy.ThoiGianCheckOut.HasValue)
                {
                    // Khi tua ngược trong Development về trước một Check-out thủ công,
                    // bỏ mốc Check-out tương lai để ca có thể tiếp tục và Check-out lại.
                    if (_environment.IsDevelopment() &&
                        nhatKy.ThoiGianCheckOut.Value > thoiGianDemo)
                    {
                        nhatKy.ThoiGianCheckOut = null;
                        nhatKy.DaTatDien = null;
                        nhatKy.DaKhoaCua = null;
                    }
                    else
                    {
                        return Conflict(new
                        {
                            success = false,
                            message = "Buổi học đã được check-out."
                        });
                    }
                }

                var mocTuDongCheckOut =
                    lich.Ngay.ToDateTime(lich.KetThuc)
                        .AddMinutes(30);

                if (thoiGianDemo >= mocTuDongCheckOut)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "Ca học đã quá 30 phút sau giờ kết thúc và được hệ thống tự động Check-out."
                    });
                }

                // Bắt buộc xác nhận cả hai điều kiện.
                if (!request.DaTatDien || !request.DaKhoaCua)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Vui lòng xác nhận đã tắt điện và khóa cửa."
                    });
                }

                // Cập nhật nhật ký Check-out.
                nhatKy.ThoiGianCheckOut = thoiGianDemo;
                nhatKy.DaTatDien = request.DaTatDien;
                nhatKy.DaKhoaCua = request.DaKhoaCua;

                // Cập nhật trạng thái lịch.
                lich.TrangThai = TrangThaiLich.DaKetThuc;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Check-out thành công.",
                    data = new
                    {
                        lich.Id,
                        lich.MaLich,
                        lich.MaPhong,
                        nhatKy.ThoiGianCheckOut,
                        nhatKy.DaTatDien,
                        nhatKy.DaKhoaCua,
                        lich.TrangThai
                    }
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}
