using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Domain;

/// <summary>Tài khoản đăng nhập (GV / KTV / QL).</summary>
[Table("NguoiDung")]
[Index(nameof(TenDangNhap), IsUnique = true, Name = "UQ_NguoiDung_TenDangNhap")]
public class NguoiDung
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string TenDangNhap { get; set; } = null!;

    [Required, StringLength(500)]
    public string MatKhauHash { get; set; } = null!;

    [Required, StringLength(100)]
    public string HoTen { get; set; } = null!;

    [Required, StringLength(5)]
    [Column(TypeName = "varchar(5)")]
    public string VaiTro { get; set; } = Roles.GV;

    public bool HoatDong { get; set; } = true;
}

[Table("Phong")]
public class Phong
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [StringLength(20)]
    public string MaPhong { get; set; } = null!;          // F507, G201, AIBOT...

    [Required, StringLength(20)]
    public string LoaiPhong { get; set; } = null!;        // PC | BYOD | Lab AI

    [Required, StringLength(10)]
    [Column(TypeName = "varchar(10)")]
    public string Cum { get; set; } = null!;              // F | G | AIBOT

    [Range(1, int.MaxValue)]
    public int SucChua { get; set; }

    [InverseProperty(nameof(ThietBi.Phong))]
    public ICollection<ThietBi> ThietBis { get; set; } = new List<ThietBi>();

    [InverseProperty(nameof(LichDay.Phong))]
    public ICollection<LichDay> LichDays { get; set; } = new List<LichDay>();
}

[Table("ThietBi")]
[Index(nameof(MaPhong), Name = "IX_ThietBi_MaPhong")]
public class ThietBi
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [StringLength(30)]
    public string MaThietBi { get; set; } = null!;        // F507-PC01, G201-TV01...

    [Required, StringLength(20)]
    public string MaPhong { get; set; } = null!;

    [Required, StringLength(10)]
    [Column(TypeName = "varchar(10)")]
    public string Loai { get; set; } = null!;             // PC | TV | WIFI | TUSAC | KIT

    [Required, StringLength(150)]
    public string Ten { get; set; } = null!;

    [Required, StringLength(20)]
    public string TrangThai { get; set; } = TrangThaiThietBi.SanSang;

    [ForeignKey(nameof(MaPhong))]
    [InverseProperty(nameof(Domain.Phong.ThietBis))]
    public Phong Phong { get; set; } = null!;
}

[Table("LichDay")]
[Index(nameof(GiangVienId), nameof(Ngay), Name = "IX_LichDay_GV_Ngay")]
// IX_LichDay_Phong_Ngay (MaPhong, Ngay) INCLUDE (BatDau, KetThuc, TrangThai) cấu hình ở LabDbContext
public class LichDay
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [StringLength(10)]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]  // L001, L002...
    public string MaLich { get; private set; } = null!;

    [Column(TypeName = "date")]
    public DateOnly Ngay { get; set; }

    [Required, StringLength(10)]
    public string Ca { get; set; } = null!;                // "Ca 1"...

    [Column(TypeName = "time(0)")]
    public TimeOnly BatDau { get; set; }

    [Column(TypeName = "time(0)")]
    public TimeOnly KetThuc { get; set; }

    [Required, StringLength(150)]
    public string MonHoc { get; set; } = null!;

    [Required, StringLength(20)]
    public string MaPhong { get; set; } = null!;

    public int GiangVienId { get; set; }

    [Required, StringLength(20)]
    public string TrangThai { get; set; } = TrangThaiLich.ChuaCheckIn;

    [Range(1, int.MaxValue)]
    public int SoBuoi { get; set; } = 1;                   // số buổi/tuần (UC14)

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    [ForeignKey(nameof(MaPhong))]
    [InverseProperty(nameof(Domain.Phong.LichDays))]
    public Phong Phong { get; set; } = null!;

    [ForeignKey(nameof(GiangVienId))]
    public NguoiDung GiangVien { get; set; } = null!;

    [InverseProperty(nameof(NhatKyCheckIn.LichDay))]
    public NhatKyCheckIn? NhatKy { get; set; }
}

[Table("NhatKyCheckIn")]
[Index(nameof(LichDayId), IsUnique = true, Name = "UQ_NhatKy_LichDay")]   // 1-1 với LichDay
public class NhatKyCheckIn
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [StringLength(10)]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]  // NK001...
    public string MaNhatKy { get; private set; } = null!;

    public int LichDayId { get; set; }

    [Column(TypeName = "datetime2(0)")]
    public DateTime ThoiGianCheckIn { get; set; }

    public bool CheckInMuon { get; set; }

    [Range(0, int.MaxValue)]
    public int SoPhutTre { get; set; }

    [Column(TypeName = "datetime2(0)")]
    public DateTime? ThoiGianCheckOut { get; set; }

    public bool? DaTatDien { get; set; }
    public bool? DaKhoaCua { get; set; }

    [ForeignKey(nameof(LichDayId))]
    [InverseProperty(nameof(Domain.LichDay.NhatKy))]
    public LichDay LichDay { get; set; } = null!;
}

[Table("TicketSuCo")]
[Index(nameof(TrangThai), Name = "IX_Ticket_TrangThai")]
[Index(nameof(MaThietBi), Name = "IX_Ticket_MaThietBi")]
public class TicketSuCo
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [StringLength(10)]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]  // TK001...
    public string MaTicket { get; private set; } = null!;

    [Required, StringLength(30)]
    public string MaThietBi { get; set; } = null!;

    [Required, StringLength(100)]
    public string LoaiLoi { get; set; } = "Sự cố thiết bị";

    [StringLength(1000)]
    public string? GhiChu { get; set; }

    public int NguoiBaoId { get; set; }

    [Column(TypeName = "datetime2(0)")]
    public DateTime ThoiGianTao { get; set; }

    [Required, StringLength(20)]
    public string TrangThai { get; set; } = TrangThaiTicket.ChoXuLy;

    public int? KtvXuLyId { get; set; }

    [Column(TypeName = "datetime2(0)")]
    public DateTime? ThoiGianCapNhat { get; set; }

    [Column(TypeName = "datetime2(0)")]
    public DateTime? ThoiGianHoanTat { get; set; }

    [ForeignKey(nameof(MaThietBi))]
    public ThietBi ThietBi { get; set; } = null!;

    [ForeignKey(nameof(NguoiBaoId))]
    public NguoiDung NguoiBao { get; set; } = null!;

    [ForeignKey(nameof(KtvXuLyId))]
    public NguoiDung? KtvXuLy { get; set; }
}

[Table("YeuCauDoiPhong")]
// UX_YeuCau_ChoDuyet (unique filtered index) cấu hình ở LabDbContext
public class YeuCauDoiPhong
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [StringLength(10)]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]  // YC001...
    public string MaYeuCau { get; private set; } = null!;

    public int LichDayId { get; set; }
    public int GiangVienId { get; set; }

    [Required, StringLength(20)]
    public string PhongCu { get; set; } = null!;

    [Required, StringLength(20)]
    public string PhongMoi { get; set; } = null!;

    [Required, StringLength(500)]
    public string LyDo { get; set; } = null!;

    [Required, StringLength(20)]
    public string Loai { get; set; } = LoaiYeuCau.CoKeHoach;

    [Required, StringLength(20)]
    public string TrangThai { get; set; } = TrangThaiYeuCau.ChoDuyet;

    [Column(TypeName = "datetime2(0)")]
    public DateTime ThoiGianTao { get; set; }

    public int? NguoiDuyetId { get; set; }

    [Column(TypeName = "datetime2(0)")]
    public DateTime? ThoiGianDuyet { get; set; }

    [StringLength(500)]
    public string? GhiChuDuyet { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;        // chặn nhiều QL duyệt/từ chối cùng lúc

    [ForeignKey(nameof(LichDayId))]
    public LichDay LichDay { get; set; } = null!;

    [ForeignKey(nameof(GiangVienId))]
    public NguoiDung GiangVien { get; set; } = null!;

    [ForeignKey(nameof(NguoiDuyetId))]
    public NguoiDung? NguoiDuyet { get; set; }

    [ForeignKey(nameof(PhongCu))]
    public Phong PhongCuRef { get; set; } = null!;

    [ForeignKey(nameof(PhongMoi))]
    public Phong PhongMoiRef { get; set; } = null!;
}

[Table("ThongBao")]
[Index(nameof(VaiTroNhan), nameof(TrangThai), Name = "IX_ThongBao_VaiTro_TrangThai")]
[Index(nameof(NguoiNhanId), Name = "IX_ThongBao_NguoiNhanId")]
public class ThongBao
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [StringLength(10)]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]  // TB001...
    public string MaThongBao { get; private set; } = null!;

    [Required, StringLength(5)]
    [Column(TypeName = "varchar(5)")]
    public string VaiTroNhan { get; set; } = Roles.QL;

    public int? NguoiNhanId { get; set; }                  // null = gửi cho cả vai trò

    [Required, StringLength(200)]
    public string NguoiNhan { get; set; } = null!;         // chuỗi hiển thị

    [Required, StringLength(1000)]
    public string NoiDung { get; set; } = null!;

    [Column(TypeName = "datetime2(0)")]
    public DateTime ThoiGian { get; set; }

    [Required, StringLength(30)]
    public string TrangThai { get; set; } = TrangThaiThongBao.ChoHauKiem;

    public int? YeuCauId { get; set; }

    [ForeignKey(nameof(YeuCauId))]
    public YeuCauDoiPhong? YeuCau { get; set; }

    [ForeignKey(nameof(NguoiNhanId))]
    public NguoiDung? NguoiNhanUser { get; set; }
}

