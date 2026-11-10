using System;

using System.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Globalization;
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
public class CheckInController : ControllerBase
{
    private readonly LabDbContext _context;

    public CheckInController(LabDbContext context)
    {
        _context = context;
    }


    [HttpPost]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
        CancellationToken cancellationToken)
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

        var environment = HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>();

        var strategy = _context.Database.CreateExecutionStrategy();

        Func<Task<IActionResult>> operation = async () =>
        {
            // Mỗi lần retry phải đọc lại dữ liệu mới từ database.
            _context.ChangeTracker.Clear();

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var lich = await _context.LichDays
                .FirstOrDefaultAsync(
                    x => x.Id == request.LichDayId,
                    cancellationToken);

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

            var hienTai = DemoClock.GetNow(Request, environment);
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

            var gioChoCheckIn = lich.BatDau.AddMinutes(-30);
            var gioKetThucCheckIn = lich.BatDau.AddMinutes(30);

            if (gioHienTai < gioChoCheckIn ||
                gioHienTai >= gioKetThucCheckIn)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Chỉ được Check-in trong khoảng từ 30 phút trước đến trước 30 phút sau giờ bắt đầu ca. Sau mốc này hệ thống tự ghi nhận Check-in muộn và chuyển sang Check-out."
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

            var nhatKyHienTai = await _context.NhatKys
                .FirstOrDefaultAsync(
                    x => x.LichDayId == lich.Id,
                    cancellationToken);

            if (nhatKyHienTai != null)
            {
                // Trong Demo/Development, khi tua ngược về trước một Check-in thủ công,
                // nhật ký tương lai không được khóa nút Check-in của thời điểm hiện tại.
                // Chỉ thay thế nhật ký có thời điểm Check-in nằm sau giờ Demo đang chọn.
                if (environment.IsDevelopment() &&
                    nhatKyHienTai.ThoiGianCheckIn >= hienTai)
                {
                    _context.NhatKys.Remove(nhatKyHienTai);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "Lịch này đã có nhật ký check-in."
                    });
                }
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
            lich.TrangThai = TrangThaiLich.DangDienRa;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

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
        };

        return await strategy.ExecuteAsync(operation);
    }

}
