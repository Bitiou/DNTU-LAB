
using DntuLab.Api.Data;
using DntuLab.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhongTrongController : ControllerBase
{
    private readonly LabDbContext _context;

    public PhongTrongController(LabDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPhongTrong(
        [FromQuery] PhongTrongRequest request)
    {
        if (request.BatDau >= request.KetThuc)
        {
            return BadRequest(new
            {
                success = false,
                message = "Giờ bắt đầu phải nhỏ hơn giờ kết thúc."
            });
        }

        var phongDaDat = await _context.LichDays
            .AsNoTracking()
            .Where(x =>
                x.Ngay == request.Ngay &&
                x.BatDau < request.KetThuc &&
                x.KetThuc > request.BatDau &&
                x.TrangThai != "Đã hủy")
            .Select(x => x.MaPhong)
            .Distinct()
            .ToListAsync();

        var phongsTrong = await _context.Phongs
            .AsNoTracking()
            .Where(x => !phongDaDat.Contains(x.MaPhong))
            .OrderBy(x => x.MaPhong)
            .Select(x => new
            {
                x.MaPhong,
                x.LoaiPhong,
                x.Cum,
                x.SucChua
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            ngay = request.Ngay,
            batDau = request.BatDau,
            ketThuc = request.KetThuc,
            count = phongsTrong.Count,
            data = phongsTrong
        });
    }
}
