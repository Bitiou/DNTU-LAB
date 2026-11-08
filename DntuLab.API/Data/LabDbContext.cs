using DntuLab.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Data;

public class LabDbContext(DbContextOptions<LabDbContext> options) : DbContext(options)
{
    public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
    public DbSet<Phong> Phongs => Set<Phong>();
    public DbSet<ThietBi> ThietBis => Set<ThietBi>();
    public DbSet<LichDay> LichDays => Set<LichDay>();
    public DbSet<NhatKyCheckIn> NhatKys => Set<NhatKyCheckIn>();
    public DbSet<TicketSuCo> Tickets => Set<TicketSuCo>();
    public DbSet<YeuCauDoiPhong> YeuCaus => Set<YeuCauDoiPhong>();
    public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
    public DbSet<GhiNhanTam> GhiNhanTams => Set<GhiNhanTam>();

    // Mã nghiệp vụ hiển thị (L001, YC001, TK001...) sinh từ Id
    private static string Code(string prefix) =>
        $"N'{prefix}' + CASE WHEN [Id] < 1000 THEN RIGHT(N'000' + CAST([Id] AS nvarchar(10)), 3) ELSE CAST([Id] AS nvarchar(10)) END";

    private static string In(string col, params string[] values) =>
        $"[{col}] IN ({string.Join(", ", values.Select(v => $"N'{v}'"))})";

    // Data Annotations đã mô tả bảng/cột/khóa/FK/index đơn giản; Fluent API chỉ bổ sung
    // những gì annotation không biểu diễn được: computed column, CHECK, filtered/included index, delete behavior.
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<NguoiDung>().ToTable(t =>
            t.HasCheckConstraint("CK_NguoiDung_VaiTro", In("VaiTro", Roles.GV, Roles.KTV, Roles.QL)));

        b.Entity<Phong>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Phong_Loai", In("LoaiPhong", KieuPhong.PC, KieuPhong.BYOD, KieuPhong.LabAI));
            t.HasCheckConstraint("CK_Phong_Cum", In("Cum", CumPhong.F, CumPhong.G, CumPhong.AIBOT));
            t.HasCheckConstraint("CK_Phong_SucChua", "[SucChua] > 0");
        });

        b.Entity<ThietBi>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_ThietBi_Loai", In("Loai", LoaiThietBi.PC, LoaiThietBi.TV, LoaiThietBi.WIFI, LoaiThietBi.TUSAC, LoaiThietBi.KIT));
            t.HasCheckConstraint("CK_ThietBi_TrangThai", In("TrangThai", TrangThaiThietBi.SanSang, TrangThaiThietBi.Hong, TrangThaiThietBi.DangBaoTri));
        });

        b.Entity<LichDay>(e =>
        {
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_LichDay_Gio", "[BatDau] < [KetThuc]");
                t.HasCheckConstraint("CK_LichDay_TrangThai", In("TrangThai", TrangThaiLich.ChuaCheckIn, TrangThaiLich.DangDienRa, TrangThaiLich.DaKetThuc));
            });
            e.Property(x => x.MaLich).HasComputedColumnSql(Code("L"), stored: true);
            e.HasIndex(x => new { x.MaPhong, x.Ngay }).HasDatabaseName("IX_LichDay_Phong_Ngay")
                .IncludeProperties(x => new { x.BatDau, x.KetThuc, x.TrangThai });
        });

        b.Entity<NhatKyCheckIn>(e =>
        {
            e.ToTable(t => t.HasCheckConstraint("CK_NhatKy_Gio", "[ThoiGianCheckOut] IS NULL OR [ThoiGianCheckOut] >= [ThoiGianCheckIn]"));
            e.Property(x => x.MaNhatKy).HasComputedColumnSql(Code("NK"), stored: true);
            e.HasOne(x => x.LichDay).WithOne(l => l.NhatKy).HasForeignKey<NhatKyCheckIn>(x => x.LichDayId);
        });

        b.Entity<TicketSuCo>(e =>
        {
            e.ToTable(t => t.HasCheckConstraint("CK_Ticket_TrangThai",
                In("TrangThai", TrangThaiTicket.ChoXuLy, TrangThaiTicket.DangSuaChua, TrangThaiTicket.BaoCaoCapTren, TrangThaiTicket.DaKhacPhuc)));
            e.Property(x => x.MaTicket).HasComputedColumnSql(Code("TK"), stored: true);
        });

        b.Entity<YeuCauDoiPhong>(e =>
        {
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_YeuCau_Loai", In("Loai", LoaiYeuCau.CoKeHoach, LoaiYeuCau.KhanCap));
                t.HasCheckConstraint("CK_YeuCau_TrangThai", In("TrangThai", TrangThaiYeuCau.ChoDuyet, TrangThaiYeuCau.TuDongDuyet, TrangThaiYeuCau.DaDuyet, TrangThaiYeuCau.TuChoi));
                t.HasCheckConstraint("CK_YeuCau_Phong", "[PhongCu] <> [PhongMoi]");
            });
            e.Property(x => x.MaYeuCau).HasComputedColumnSql(Code("YC"), stored: true);
            // Mỗi ca chỉ có tối đa 1 yêu cầu "Chờ duyệt"
            e.HasIndex(x => x.LichDayId).IsUnique().HasDatabaseName("UX_YeuCau_ChoDuyet")
                .HasFilter($"[TrangThai] = N'{TrangThaiYeuCau.ChoDuyet}'");
        });

        b.Entity<ThongBao>()
            .Property(x => x.MaThongBao).HasComputedColumnSql(Code("TB"), stored: true);


        b.Entity<GhiNhanTam>(e =>
        {
            e.ToTable("GhiNhanTam", "dbo", t =>
            {
                t.HasCheckConstraint(
                    "CK_GhiNhan_TrangThai",
                    In("TrangThai",
                        TrangThaiGhiNhan.ChuaTongHop,
                        TrangThaiGhiNhan.DaTaoTicket));
            });

            e.Property(x => x.MaGhiNhan)
                .HasComputedColumnSql(Code("GN"), stored: true);
        });


        // Không cascade delete: dữ liệu vận hành (nhật ký, ticket) không mất theo khi xóa dữ liệu gốc.
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
