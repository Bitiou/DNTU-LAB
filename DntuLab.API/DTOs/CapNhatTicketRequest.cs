
using System.ComponentModel.DataAnnotations;

namespace DntuLab.Api.DTOs;

public class CapNhatTicketRequest
{
    [Required]
    public string TrangThai { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? GhiChu { get; set; }
}
