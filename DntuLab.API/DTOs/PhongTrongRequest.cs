
using System.ComponentModel.DataAnnotations;

namespace DntuLab.Api.DTOs;

public class PhongTrongRequest
{
    [Required]
    public DateOnly Ngay { get; set; }

    [Required]
    public TimeOnly BatDau { get; set; }

    [Required]
    public TimeOnly KetThuc { get; set; }
}
