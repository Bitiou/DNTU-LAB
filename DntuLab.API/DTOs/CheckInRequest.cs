
using System.ComponentModel.DataAnnotations;

namespace DntuLab.Api.DTOs;

public class CheckInRequest
{
    [Range(1, int.MaxValue)]
    public int LichDayId { get; set; }

    [Required, StringLength(20)]
    public string MaPhong { get; set; } = string.Empty;
}
