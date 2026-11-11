
namespace DntuLab.Api.DTOs;

public class LocTicketRequest
{
    public string? TrangThai { get; set; }

    public string? MaPhong { get; set; }

    public DateTime? TuNgay { get; set; }

    public DateTime? DenNgay { get; set; }

    public int Trang { get; set; } = 1;

    public int KichThuocTrang { get; set; } = 10;
}
