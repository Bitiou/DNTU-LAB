
using System.Security.Claims;
using System.Data;
using System.Globalization;
using DntuLab.Api.Data;
using DntuLab.Api.DTOs;
using DntuLab.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/DoiPhongKhanCap")]
[Authorize(Roles = "GV")]
public class DoiPhongKhanCapController : ControllerBase
{
    private readonly LabDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public DoiPhongKhanCapController(
        LabDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    private DateTime GetVietnamNow()
    {
        TimeZoneInfo vietnamTimeZone;
        try
        {
            // Windows
            vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            // Linux / container
            vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

        // Dong ho demo chi duoc phep ghi de trong Development.
        // Production bo qua header de khong the gia mao gio he thong.
        if (_environment.IsDevelopment())
        {
            var dateText = Request.Headers["X-Debug-Date"].ToString();
            var timeText = Request.Headers["X-Debug-Time"].ToString();
            var date = DateOnly.FromDateTime(now);
            var time = TimeOnly.FromDateTime(now);
            if (DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)) date = parsedDate;
            if (TimeOnly.TryParse(timeText, out var parsedTime)) time = parsedTime;
            return date.ToDateTime(time);
        }

        return now;
    }

    [HttpPost]
    public async Task<IActionResult> DoiPhong(
        [FromBody] DoiPhongKhanCapRequest request)
    {
        var hienTai = GetVietnamNow();

        if (hienTai.TimeOfDay >= new TimeSpan(8, 0, 0))
        {
            return BadRequest(new
            {
                success = false,
                message = "Đổi phòng khẩn cấp chỉ được thực hiện trước 08:00."
            });
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var giangVienId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Không xác định được tài khoản."
            });
        }

        // Khi EnableRetryOnFailure được bật, transaction thủ công phải nằm
        // bên trong execution strategy để toàn bộ nghiệp vụ được retry như một đơn vị.
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

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
                return Forbid();

            if (lich.TrangThai != "Đang diễn ra")
            {
                return Conflict(new
                {
                    success = false,
                    message = "Chỉ được đổi phòng cho ca đang diễn ra."
                });
            }

            var daDoiKhanCap = await _context.YeuCaus
                .AnyAsync(x => x.LichDayId == lich.Id && x.Loai == "Khẩn cấp");

            if (daDoiKhanCap)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Lịch này đã có yêu cầu đổi phòng khẩn cấp."
                });
            }

            var phongCu = lich.MaPhong;
            var phongMoi = (request.PhongMoi ?? string.Empty).Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(phongMoi))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng chọn phòng mới."
                });
            }

            if (phongCu == phongMoi)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Phòng mới phải khác phòng hiện tại."
                });
            }

            var phongHopLe = await _context.Phongs
                .AnyAsync(x => x.MaPhong == phongMoi);

            if (!phongHopLe)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Phòng mới không tồn tại."
                });
            }

            bool LaPhongFG(string maPhong) =>
                maPhong.StartsWith("F", StringComparison.OrdinalIgnoreCase) ||
                maPhong.StartsWith("G", StringComparison.OrdinalIgnoreCase);

            if (!LaPhongFG(phongCu) || !LaPhongFG(phongMoi))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Đổi phòng khẩn cấp chỉ áp dụng giữa các phòng khu F và G."
                });
            }

            var biTrungLich = await _context.LichDays
                .AnyAsync(x =>
                    x.Id != lich.Id &&
                    x.Ngay == lich.Ngay &&
                    x.MaPhong == phongMoi &&
                    x.BatDau < lich.KetThuc &&
                    x.KetThuc > lich.BatDau &&
                    x.TrangThai != "Đã hủy");

            if (biTrungLich)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Phòng mới đã có lịch sử dụng trong khung giờ này."
                });
            }

            // Cập nhật lịch, tạo yêu cầu hậu kiểm và thông báo trong cùng transaction.
            lich.MaPhong = phongMoi;

            var lyDo = (request.LyDo ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(lyDo))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng nhập lý do đổi phòng."
                });
            }

            var yeuCau = new YeuCauDoiPhong
            {
                LichDayId = lich.Id,
                GiangVienId = giangVienId,
                PhongCu = phongCu,
                PhongMoi = phongMoi,
                LyDo = lyDo,
                Loai = "Khẩn cấp",
                TrangThai = "Tự động duyệt",
                ThoiGianTao = hienTai
            };

            _context.YeuCaus.Add(yeuCau);
            await _context.SaveChangesAsync();

            _context.ThongBaos.Add(new ThongBao
            {
                VaiTroNhan = Roles.QL,
                NguoiNhan = "Quản lý LAB, Phòng Đào tạo",
                NoiDung = $"Đổi phòng khẩn cấp: lịch {lich.Id}, {phongCu} → {phongMoi}. Lý do: {lyDo}",
                ThoiGian = hienTai,
                TrangThai = TrangThaiThongBao.ChoHauKiem,
                YeuCauId = yeuCau.Id
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                success = true,
                message = "Đã đổi phòng khẩn cấp. Yêu cầu cần được Quản lý LAB kiểm tra sau.",
                lichDayId = lich.Id,
                phongCu,
                phongMoi,
                thoiGianDoi = hienTai
            });
        });
    }
}
