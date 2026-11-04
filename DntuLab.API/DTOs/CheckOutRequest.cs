
using System.ComponentModel.DataAnnotations;

namespace DntuLab.Api.DTOs;

public class CheckOutRequest
{
    [Range(1, int.MaxValue)]
    public int LichDayId { get; set; }

    public bool DaTatDien { get; set; }

    public bool DaKhoaCua { get; set; }
}
