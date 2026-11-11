
using System.ComponentModel.DataAnnotations;

namespace DntuLab.Api.DTOs;

public class TaoYeuCauDoiPhongRequest
{
    [Range(1, int.MaxValue)]
    public int LichDayId { get; set; }

    [Required, StringLength(20)]
    public string PhongMoi { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string LyDo { get; set; } = string.Empty;
}
