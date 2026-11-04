using DntuLab.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "QL")]
public class ThongBaoController : ControllerBase
{
    private readonly LabDbContext _context;
    public ThongBaoController(LabDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _context.ThongBaos.AsNoTracking()
            .Where(x => x.VaiTroNhan == "QL")
            .OrderByDescending(x => x.ThoiGian)
            .Select(x => new { x.Id, x.MaThongBao, x.NguoiNhan, x.NoiDung, x.ThoiGian, x.TrangThai, x.YeuCauId })
            .ToListAsync();
        return Ok(new { success = true, count = data.Count, data });
    }
}
