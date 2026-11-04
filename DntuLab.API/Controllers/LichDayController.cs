
using DntuLab.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LichDayController : ControllerBase
{
    private readonly LabDbContext _context;

    public LichDayController(LabDbContext context)
    {
        _context = context;
    }

    // Lấy toàn bộ lịch dạy
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Tra ve ten giang vien kem lich de giao dien Quan ly LAB khong phai
        // tu ghep ten theo mon hoc hoac phu thuoc vao ten truong JSON doan.
        var lichDays = await _context.LichDays
            .AsNoTracking()
            .OrderBy(x => x.Ngay)
            .ThenBy(x => x.BatDau)
            .Select(x => new
            {
                x.Id,
                x.MaLich,
                x.Ngay,
                x.Ca,
                x.BatDau,
                x.KetThuc,
                x.MonHoc,
                x.MaPhong,
                x.GiangVienId,
                TenGiangVien = x.GiangVien.HoTen,
                x.TrangThai,
                x.SoBuoi
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            count = lichDays.Count,
            data = lichDays
        });
    }

    // Tra cứu lịch dạy theo ngày
    [HttpGet("theo-ngay")]
    public async Task<IActionResult> GetTheoNgay(
        [FromQuery] DateOnly ngay)
    {
        var lichDays = await _context.LichDays
            .AsNoTracking()
            .Where(x => x.Ngay == ngay)
            .OrderBy(x => x.BatDau)
            .Select(x => new
            {
                x.Id,
                x.MaLich,
                x.Ngay,
                x.Ca,
                x.BatDau,
                x.KetThuc,
                x.MonHoc,
                x.MaPhong,
                x.GiangVienId,
                TenGiangVien = x.GiangVien.HoTen,
                x.TrangThai
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            ngay,
            count = lichDays.Count,
            data = lichDays
        });
    }
}
