
using DntuLab.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThietBiController : ControllerBase
{
    private readonly LabDbContext _context;

    public ThietBiController(LabDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var thietBis = await _context.ThietBis
            .AsNoTracking()
            .ToListAsync();

        return Ok(new
        {
            success = true,
            count = thietBis.Count,
            data = thietBis
        });
    }
}
