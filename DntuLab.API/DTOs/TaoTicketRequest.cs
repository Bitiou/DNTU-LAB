
using System.ComponentModel.DataAnnotations;

namespace DntuLab.Api.DTOs;

public class TaoTicketRequest
{
    [Required, StringLength(30)]
    public string MaThietBi { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LoaiLoi { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? GhiChu { get; set; }
}
