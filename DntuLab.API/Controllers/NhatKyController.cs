
using DntuLab.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NhatKyController : ControllerBase
{
    private readonly LabDbContext _context;

    public NhatKyController(LabDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var nhatKys = await _context.NhatKys
            .AsNoTracking()
            .ToListAsync();

        return Ok(new
        {
            success = true,
            count = nhatKys.Count,
            data = nhatKys
        });
    }
}
