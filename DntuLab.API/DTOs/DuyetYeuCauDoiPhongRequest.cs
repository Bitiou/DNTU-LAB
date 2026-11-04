
using System.ComponentModel.DataAnnotations;

namespace DntuLab.Api.DTOs;

public class DuyetYeuCauDoiPhongRequest
{
    [Required]
    public string GhiChuDuyet { get; set; } = string.Empty;
}
