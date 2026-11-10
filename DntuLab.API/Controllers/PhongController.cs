
using DntuLab.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhongController : ControllerBase
{
    private readonly LabDbContext _context;

    public PhongController(LabDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var phongs = await _context.Phongs
            .AsNoTracking()
            .ToListAsync();

        return Ok(new
        {
            success = true,
            count = phongs.Count,
            data = phongs
        });
    }
}
