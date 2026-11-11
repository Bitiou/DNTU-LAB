
using System.ComponentModel.DataAnnotations;

namespace DntuLab.Api.DTOs;

public class DangNhapRequest
{
    [Required]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required]
    public string MatKhau { get; set; } = string.Empty;
}
