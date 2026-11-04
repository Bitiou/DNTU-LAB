
using System.Security.Claims;
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

    public DoiPhongKhanCapController(LabDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> DoiPhong(
        [FromBody] DoiPhongKhanCapRequest request)
    {
        // Chỉ cho phép xử lý trước 08:00 theo giờ máy chủ.
        var hienTai = DateTime.Now;

        if (hienTai.TimeOfDay >= new TimeSpan(8, 0, 0))
        {
            return BadRequest(new
            {
                success = false,
                message = "Đổi phòng khẩn cấp chỉ được thực hiện trước 08:00."
            });
        }

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
            await _context.Database.BeginTransactionAsync();

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

        if (lich.Ngay != DateOnly.FromDateTime(hienTai))
        {
            return BadRequest(new
            {
                success = false,
                message = "Chỉ áp dụng đổi phòng khẩn cấp cho lịch trong ngày."
            });
        }

        if (lich.TrangThai != "Đang diễn ra")
        {
            return Conflict(new { success = false, message = "Chỉ được đổi phòng cho ca đang diễn ra." });
        }
        var daDoiKhanCap = await _context.YeuCaus.AnyAsync(x => x.LichDayId == lich.Id && x.Loai == "Khẩn cấp");
        if (daDoiKhanCap)
        {
            return Conflict(new { success = false, message = "Lịch này đã có yêu cầu đổi phòng khẩn cấp." });
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

        // Không áp dụng cho AIBOT hoặc đổi từ/đến AIBOT.
        bool laPhongFG(string maPhong) =>
            maPhong.StartsWith("F") ||
            maPhong.StartsWith("G");

        if (!laPhongFG(phongCu) || !laPhongFG(phongMoi))
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
        var yeuCau = new YeuCauDoiPhong
        {
            LichDayId = lich.Id,
            GiangVienId = giangVienId,
            PhongCu = phongCu,
            PhongMoi = phongMoi,
            LyDo = request.LyDo.Trim(),
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
            NoiDung = $"Đổi phòng khẩn cấp: lịch {lich.Id}, {phongCu} → {phongMoi}. Lý do: {request.LyDo.Trim()}",
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
    }
}
