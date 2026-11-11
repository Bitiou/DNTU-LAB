
using System.ComponentModel.DataAnnotations;

namespace DntuLab.Api.DTOs;

public class TaoGhiNhanTamRequest
{
    [Range(1, int.MaxValue)]
    public int LichDayId { get; set; }

    [Required, StringLength(50)]
    public string MaThietBi { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string MoTa { get; set; } = string.Empty;
}
