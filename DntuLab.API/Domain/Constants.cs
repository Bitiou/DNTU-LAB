namespace DntuLab.Api.Domain;

public static class Roles
{
    public const string GV = "GV";     // Giảng viên
    public const string KTV = "KTV";   // Kỹ thuật viên
    public const string QL = "QL";     // Quản lý LAB
}

public static class CumPhong
{
    public const string F = "F";
    public const string G = "G";
    public const string AIBOT = "AIBOT";
}

public static class KieuPhong
{
    public const string PC = "PC";
    public const string BYOD = "BYOD";
    public const string LabAI = "Lab AI";
}

public static class LoaiThietBi
{
    public const string PC = "PC";
    public const string TV = "TV";
    public const string WIFI = "WIFI";
    public const string TUSAC = "TUSAC";
    public const string KIT = "KIT";
}

public static class TrangThaiThietBi
{
    public const string SanSang = "Sẵn sàng";
    public const string Hong = "Hỏng";
    public const string DangBaoTri = "Đang bảo trì";
}

public static class TrangThaiLich
{
    public const string ChuaCheckIn = "Chưa check-in";
    public const string DangDienRa = "Đang diễn ra";
    public const string DaKetThuc = "Đã kết thúc";
}

public static class TrangThaiTicket
{
    public const string ChoXuLy = "Chờ xử lý";
    public const string DangSuaChua = "Đang sửa chữa";
    public const string BaoCaoCapTren = "Báo cáo cấp trên";
    public const string DaKhacPhuc = "Đã khắc phục";
}

public static class LoaiYeuCau
{
    public const string CoKeHoach = "Có kế hoạch";
    public const string KhanCap = "Khẩn cấp";
}

public static class TrangThaiYeuCau
{
    public const string ChoDuyet = "Chờ duyệt";
    public const string TuDongDuyet = "Tự động duyệt";
    public const string DaDuyet = "Đã duyệt";
    public const string TuChoi = "Từ chối";
}

public static class TrangThaiThongBao
{
    public const string ChoHauKiem = "Chờ hậu kiểm sau 08:00";
    public const string DaHauKiem = "Đã hậu kiểm";
    public const string ChuaDoc = "Chưa đọc";
    public const string DaDoc = "Đã đọc";
}

public static class TrangThaiGhiNhan
{
    public const string ChuaTongHop = "Chưa tổng hợp";
    public const string DaTaoTicket = "Đã tạo Ticket";
}

public static class QuyDinh
{
    public const int CheckInSomPhut = 15;
    public const int CheckInMuonPhut = 15;
    public static readonly TimeOnly GioChotKhanCap = new(8, 0);
}
