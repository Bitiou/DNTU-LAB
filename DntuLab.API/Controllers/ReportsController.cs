
using DntuLab.Api.Data;
using DntuLab.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "KTV,QL")]
public class ReportsController : ControllerBase
{
    private readonly LabDbContext _context;

    public ReportsController(LabDbContext context)
    {
        _context = context;
    }

    // 1. Báo cáo tổng quan hệ thống
    [HttpGet("tong-quan")]
    public async Task<IActionResult> TongQuan()
    {
        var tongPhong = await _context.Phongs.CountAsync();
        var tongThietBi = await _context.ThietBis.CountAsync();
        var tongLichDay = await _context.LichDays.CountAsync();
        var tongNhatKy = await _context.NhatKys.CountAsync();
        var tongTicket = await _context.Tickets.CountAsync();

        var ticketChoXuLy = await _context.Tickets
            .CountAsync(x => x.TrangThai == TrangThaiTicket.ChoXuLy);

        var ticketDangSuaChua = await _context.Tickets
            .CountAsync(x => x.TrangThai == TrangThaiTicket.DangSuaChua);

        var ticketBaoCaoCapTren = await _context.Tickets
            .CountAsync(x => x.TrangThai == TrangThaiTicket.BaoCaoCapTren);

        var ticketDaKhacPhuc = await _context.Tickets
            .CountAsync(x => x.TrangThai == TrangThaiTicket.DaKhacPhuc);

        return Ok(new
        {
            success = true,
            data = new
            {
                tongPhong,
                tongThietBi,
                tongLichDay,
                tongNhatKy,
                tongTicket,
                thongKeTicket = new
                {
                    choXuLy = ticketChoXuLy,
                    dangSuaChua = ticketDangSuaChua,
                    baoCaoCapTren = ticketBaoCaoCapTren,
                    daKhacPhuc = ticketDaKhacPhuc
                }
            }
        });
    }

    // 2. Thống kê số lịch dạy theo phòng
    [HttpGet("lich-theo-phong")]
    public async Task<IActionResult> LichTheoPhong(
        [FromQuery] DateOnly? tuNgay,
        [FromQuery] DateOnly? denNgay)
    {
        if (tuNgay.HasValue &&
            denNgay.HasValue &&
            tuNgay.Value > denNgay.Value)
        {
            return BadRequest(new
            {
                success = false,
                message = "Từ ngày không được lớn hơn đến ngày."
            });
        }

        var query = _context.LichDays
            .AsNoTracking()
            .AsQueryable();

        if (tuNgay.HasValue)
        {
            query = query.Where(x => x.Ngay >= tuNgay.Value);
        }

        if (denNgay.HasValue)
        {
            query = query.Where(x => x.Ngay <= denNgay.Value);
        }

        var data = await query
            .GroupBy(x => x.MaPhong)
            .Select(g => new
            {
                maPhong = g.Key,
                tongSoLich = g.Count()
            })
            .OrderBy(x => x.maPhong)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            tuNgay,
            denNgay,
            tongSoLich = data.Sum(x => x.tongSoLich),
            count = data.Count,
            data
        });
    }

    // 3. Thống kê Ticket theo trạng thái
    [HttpGet("ticket-theo-trang-thai")]
    public async Task<IActionResult> TicketTheoTrangThai(
        [FromQuery] DateOnly? tuNgay,
        [FromQuery] DateOnly? denNgay)
    {
        if (tuNgay.HasValue &&
            denNgay.HasValue &&
            tuNgay.Value > denNgay.Value)
        {
            return BadRequest(new
            {
                success = false,
                message = "Từ ngày không được lớn hơn đến ngày."
            });
        }

        var query = _context.Tickets
            .AsNoTracking()
            .AsQueryable();

        if (tuNgay.HasValue)
        {
            var batDau = tuNgay.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.ThoiGianTao >= batDau);
        }

        if (denNgay.HasValue)
        {
            var ketThuc = denNgay.Value.AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            query = query.Where(x => x.ThoiGianTao < ketThuc);
        }

        var data = await query
            .GroupBy(x => x.TrangThai)
            .Select(g => new
            {
                trangThai = g.Key,
                soLuong = g.Count()
            })
            .OrderBy(x => x.trangThai)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            tuNgay,
            denNgay,
            tongSoTicket = data.Sum(x => x.soLuong),
            count = data.Count,
            data
        });
    }
}
