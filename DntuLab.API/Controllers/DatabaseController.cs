using DntuLab.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
namespace DntuLab.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly LabDbContext _context;

    public DatabaseController(LabDbContext context)
    {
        _context = context;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            bool connected =
                await _context.Database.CanConnectAsync();

            if (!connected)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Không kết nối được database DntuLab."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Kết nối SQL Server thành công!",
                database = _context.Database
                    .GetDbConnection().Database
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message
            });
        }
    }
}