
using System.Security.Claims;
using System.Data;
using System.Globalization;
using DntuLab.Api.Data;
using DntuLab.Api.DTOs;
using DntuLab.Api.Domain;
using DntuLab.Api.Services;
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

    [HttpPost]
    public async Task<IActionResult> DoiPhong(
        [FromBody] DoiPhongKhanCapRequest request)
    {
        var hienTai = DemoClock.GetNow(Request, _environment);

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

            if (lich.Ngay != DateOnly.FromDateTime(hienTai))
            {
                return Conflict(new
                {
                    success = false,
                    message = "Ca học không thuộc ngày Demo đang chọn."
                });
            }

            // Nhật ký Check-in là nguồn xác thực nghiệp vụ; không chỉ dựa vào
            // TrangThai có thể chưa đồng bộ khi đồng hồ Demo vừa được tua.
            var nhatKy = await _context.NhatKys
                .FirstOrDefaultAsync(x => x.LichDayId == lich.Id);

            if (nhatKy == null || nhatKy.ThoiGianCheckIn > hienTai ||
                (nhatKy.ThoiGianCheckOut.HasValue && nhatKy.ThoiGianCheckOut.Value <= hienTai))
            {
                return Conflict(new
                {
                    success = false,
                    message = "Ca học phải Check-in và chưa Check-out tại thời điểm Demo hiện tại."
                });
            }

            if (hienTai < lich.Ngay.ToDateTime(lich.BatDau).AddMinutes(-30) ||
                hienTai >= lich.Ngay.ToDateTime(lich.KetThuc).AddMinutes(30))
            {
                return Conflict(new
                {
                    success = false,
                    message = "Ca học không còn trong khoảng thời gian được phép đổi phòng khẩn cấp."
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

            var lyDo = (request.LyDo ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(lyDo))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng nhập lý do đổi phòng."
                });
            }

            // Cập nhật lịch, đồng thời đóng các yêu cầu có kế hoạch đang chờ
            // duyệt cho cùng ca. Sau UC05, phòng nguồn của các yêu cầu cũ
            // không còn đúng nên không được để chúng treo hoặc duyệt nhầm.
            lich.MaPhong = phongMoi;

            var yeuCauKeHoachChoDuyet = await _context.YeuCaus
                .Where(x =>
                    x.LichDayId == lich.Id &&
                    x.Loai == LoaiYeuCau.CoKeHoach &&
                    x.TrangThai == TrangThaiYeuCau.ChoDuyet)
                .ToListAsync();

            foreach (var yeuCauCu in yeuCauKeHoachChoDuyet)
            {
                yeuCauCu.TrangThai = TrangThaiYeuCau.TuChoi;
                yeuCauCu.ThoiGianDuyet = hienTai;
                yeuCauCu.GhiChuDuyet =
                    $"Tự đóng do đổi phòng khẩn cấp UC05: lịch đã chuyển từ {phongCu} sang {phongMoi}.";
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
