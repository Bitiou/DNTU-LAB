using System.ComponentModel.DataAnnotations.Schema;


namespace DntuLab.Api.Domain;

public class GhiNhanTam
{
    public int Id { get; set; }

    public string? MaGhiNhan { get; set; }

    public int LichDayId { get; set; }

    public string MaThietBi { get; set; } = string.Empty;

    public string MoTa { get; set; } = string.Empty;

    public int NguoiGhiId { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime ThoiGian { get; set; }

    public string TrangThai { get; set; } = string.Empty;

    public int? TicketId { get; set; }
}
