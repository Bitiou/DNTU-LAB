
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
[Route("api/QuanLyYeuCauDoiPhong")]
[Authorize(Roles = "QL")]
public class QuanLyYeuCauDoiPhongController : ControllerBase
{
    private readonly LabDbContext _context;

    public QuanLyYeuCauDoiPhongController(LabDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // GET: Lấy danh sách yêu cầu đổi phòng
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? trangThai = null)
    {
        var query = _context.YeuCaus
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai))
        {
            query = query.Where(x => x.TrangThai == trangThai);
        }

        var data = await query
            .OrderByDescending(x => x.ThoiGianTao)
            .Select(x => new
            {
                x.Id,
                x.LichDayId,
                x.GiangVienId,
                TenGiangVien = x.GiangVien.HoTen,
                x.PhongCu,
                x.PhongMoi,
                x.LyDo,
                x.Loai,
                x.TrangThai,
                x.ThoiGianTao,
                x.NguoiDuyetId,
                x.ThoiGianDuyet,
                x.GhiChuDuyet
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // =====================================================
    // POST: Phê duyệt yêu cầu đổi phòng
    // =====================================================
    [HttpPost("{id:int}/phe-duyet")]
    public async Task<IActionResult> PheDuyet(
        int id,
        [FromBody] DuyetYeuCauDoiPhongRequest? request)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var nguoiDuyetId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Không xác định được người duyệt."
            });
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var yeuCau = await _context.YeuCaus
                .FirstOrDefaultAsync(x => x.Id == id);

            if (yeuCau == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy yêu cầu."
                });
            }

            if (yeuCau.TrangThai != "Chờ duyệt")
            {
                return Conflict(new
                {
                    success = false,
                    message = "Yêu cầu không còn ở trạng thái chờ duyệt."
                });
            }

            var lich = await _context.LichDays
                .FirstOrDefaultAsync(x => x.Id == yeuCau.LichDayId);

            if (lich == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy lịch dạy."
                });
            }

            if (lich.MaPhong != yeuCau.PhongCu)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Phòng hiện tại đã thay đổi. Cần kiểm tra lại yêu cầu."
                });
            }

            // Kiểm tra phòng mới có bị trùng lịch hay không.
            var biTrungLich = await _context.LichDays
                .AnyAsync(x =>
                    x.Id != lich.Id &&
                    x.Ngay == lich.Ngay &&
                    x.MaPhong == yeuCau.PhongMoi &&
                    x.BatDau < lich.KetThuc &&
                    x.KetThuc > lich.BatDau &&
                    x.TrangThai != "Đã hủy");

            if (biTrungLich)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Phòng mới bị trùng lịch sử dụng."
                });
            }

            // Cập nhật phòng trong lịch dạy.
            lich.MaPhong = yeuCau.PhongMoi;

            // Cập nhật yêu cầu.
            yeuCau.TrangThai = "Đã duyệt";
            yeuCau.NguoiDuyetId = nguoiDuyetId;
            yeuCau.ThoiGianDuyet = DateTime.Now;
            yeuCau.GhiChuDuyet = request?.GhiChuDuyet?.Trim();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                success = true,
                message = "Đã phê duyệt và cập nhật phòng.",
                yeuCauId = yeuCau.Id,
                phongMoi = lich.MaPhong,
                trangThai = yeuCau.TrangThai
            });
        });
    }

    // =====================================================
    // POST: Từ chối yêu cầu đổi phòng
    // =====================================================
    [HttpPost("{id:int}/tu-choi")]
    public async Task<IActionResult> TuChoi(
        int id,
        [FromBody] DuyetYeuCauDoiPhongRequest? request)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var nguoiDuyetId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Không xác định được người duyệt."
            });
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var yeuCau = await _context.YeuCaus
                .FirstOrDefaultAsync(x => x.Id == id);

            if (yeuCau == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy yêu cầu."
                });
            }

            if (yeuCau.TrangThai != "Chờ duyệt")
            {
                return Conflict(new
                {
                    success = false,
                    message = "Yêu cầu không còn ở trạng thái chờ duyệt."
                });
            }

            yeuCau.TrangThai = "Từ chối";
            yeuCau.NguoiDuyetId = nguoiDuyetId;
            yeuCau.ThoiGianDuyet = DateTime.Now;
            yeuCau.GhiChuDuyet = request?.GhiChuDuyet?.Trim();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                success = true,
                message = "Đã từ chối yêu cầu đổi phòng.",
                yeuCauId = yeuCau.Id,
                trangThai = yeuCau.TrangThai
            });
        });
    }
}
