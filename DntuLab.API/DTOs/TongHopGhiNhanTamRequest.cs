
using System.ComponentModel.DataAnnotations;

namespace DntuLab.Api.DTOs;

public class TongHopGhiNhanTamRequest
{
    [Required]
    [MinLength(1)]
    public List<int> GhiNhanIds { get; set; } = new();
}
