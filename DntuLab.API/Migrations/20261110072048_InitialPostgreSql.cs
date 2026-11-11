using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DntuLab.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "GhiNhanTam",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaGhiNhan = table.Column<string>(type: "text", nullable: true, computedColumnSql: "('GN' || CASE WHEN \"Id\" < 1000 THEN lpad(\"Id\"::text, 3, '0') ELSE \"Id\"::text END)", stored: true),
                    LichDayId = table.Column<int>(type: "integer", nullable: false),
                    MaThietBi = table.Column<string>(type: "text", nullable: false),
                    MoTa = table.Column<string>(type: "text", nullable: false),
                    NguoiGhiId = table.Column<int>(type: "integer", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TrangThai = table.Column<string>(type: "text", nullable: false),
                    TicketId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhiNhanTam", x => x.Id);
                    table.CheckConstraint("CK_GhiNhan_TrangThai", "\"TrangThai\" IN ('Chưa tổng hợp', 'Đã tạo Ticket')");
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenDangNhap = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MatKhauHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HoTen = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VaiTro = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    HoatDong = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung", x => x.Id);
                    table.CheckConstraint("CK_NguoiDung_VaiTro", "\"VaiTro\" IN ('GV', 'KTV', 'QL')");
                });

            migrationBuilder.CreateTable(
                name: "Phong",
                columns: table => new
                {
                    MaPhong = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoaiPhong = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Cum = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    SucChua = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phong", x => x.MaPhong);
                    table.CheckConstraint("CK_Phong_Cum", "\"Cum\" IN ('F', 'G', 'AIBOT')");
                    table.CheckConstraint("CK_Phong_Loai", "\"LoaiPhong\" IN ('PC', 'BYOD', 'Lab AI')");
                    table.CheckConstraint("CK_Phong_SucChua", "\"SucChua\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "LichDay",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaLich = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, computedColumnSql: "('L' || CASE WHEN \"Id\" < 1000 THEN lpad(\"Id\"::text, 3, '0') ELSE \"Id\"::text END)", stored: true),
                    Ngay = table.Column<DateOnly>(type: "date", nullable: false),
                    Ca = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BatDau = table.Column<TimeOnly>(type: "time(0) without time zone", nullable: false),
                    KetThuc = table.Column<TimeOnly>(type: "time(0) without time zone", nullable: false),
                    MonHoc = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MaPhong = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GiangVienId = table.Column<int>(type: "integer", nullable: false),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SoBuoi = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichDay", x => x.Id);
                    table.CheckConstraint("CK_LichDay_Gio", "\"BatDau\" < \"KetThuc\"");
                    table.CheckConstraint("CK_LichDay_TrangThai", "\"TrangThai\" IN ('Chưa check-in', 'Đang diễn ra', 'Đã kết thúc')");
                    table.ForeignKey(
                        name: "FK_LichDay_NguoiDung_GiangVienId",
                        column: x => x.GiangVienId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichDay_Phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "Phong",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThietBi",
                columns: table => new
                {
                    MaThietBi = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MaPhong = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Loai = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Ten = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThietBi", x => x.MaThietBi);
                    table.CheckConstraint("CK_ThietBi_Loai", "\"Loai\" IN ('PC', 'TV', 'WIFI', 'TUSAC', 'KIT')");
                    table.CheckConstraint("CK_ThietBi_TrangThai", "\"TrangThai\" IN ('Sẵn sàng', 'Hỏng', 'Đang bảo trì')");
                    table.ForeignKey(
                        name: "FK_ThietBi_Phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "Phong",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyCheckIn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaNhatKy = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, computedColumnSql: "('NK' || CASE WHEN \"Id\" < 1000 THEN lpad(\"Id\"::text, 3, '0') ELSE \"Id\"::text END)", stored: true),
                    LichDayId = table.Column<int>(type: "integer", nullable: false),
                    ThoiGianCheckIn = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: false),
                    CheckInMuon = table.Column<bool>(type: "boolean", nullable: false),
                    SoPhutTre = table.Column<int>(type: "integer", nullable: false),
                    ThoiGianCheckOut = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    DaTatDien = table.Column<bool>(type: "boolean", nullable: true),
                    DaKhoaCua = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyCheckIn", x => x.Id);
                    table.CheckConstraint("CK_NhatKy_Gio", "\"ThoiGianCheckOut\" IS NULL OR \"ThoiGianCheckOut\" >= \"ThoiGianCheckIn\"");
                    table.ForeignKey(
                        name: "FK_NhatKyCheckIn_LichDay_LichDayId",
                        column: x => x.LichDayId,
                        principalTable: "LichDay",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauDoiPhong",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaYeuCau = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, computedColumnSql: "('YC' || CASE WHEN \"Id\" < 1000 THEN lpad(\"Id\"::text, 3, '0') ELSE \"Id\"::text END)", stored: true),
                    LichDayId = table.Column<int>(type: "integer", nullable: false),
                    GiangVienId = table.Column<int>(type: "integer", nullable: false),
                    PhongCu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PhongMoi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LyDo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Loai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ThoiGianTao = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: false),
                    NguoiDuyetId = table.Column<int>(type: "integer", nullable: true),
                    ThoiGianDuyet = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    GhiChuDuyet = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauDoiPhong", x => x.Id);
                    table.CheckConstraint("CK_YeuCau_Loai", "\"Loai\" IN ('Có kế hoạch', 'Khẩn cấp')");
                    table.CheckConstraint("CK_YeuCau_Phong", "\"PhongCu\" <> \"PhongMoi\"");
                    table.CheckConstraint("CK_YeuCau_TrangThai", "\"TrangThai\" IN ('Chờ duyệt', 'Tự động duyệt', 'Đã duyệt', 'Từ chối')");
                    table.ForeignKey(
                        name: "FK_YeuCauDoiPhong_LichDay_LichDayId",
                        column: x => x.LichDayId,
                        principalTable: "LichDay",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauDoiPhong_NguoiDung_GiangVienId",
                        column: x => x.GiangVienId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauDoiPhong_NguoiDung_NguoiDuyetId",
                        column: x => x.NguoiDuyetId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauDoiPhong_Phong_PhongCu",
                        column: x => x.PhongCu,
                        principalTable: "Phong",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauDoiPhong_Phong_PhongMoi",
                        column: x => x.PhongMoi,
                        principalTable: "Phong",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketSuCo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaTicket = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, computedColumnSql: "('TK' || CASE WHEN \"Id\" < 1000 THEN lpad(\"Id\"::text, 3, '0') ELSE \"Id\"::text END)", stored: true),
                    MaThietBi = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LoaiLoi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NguoiBaoId = table.Column<int>(type: "integer", nullable: false),
                    ThoiGianTao = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: false),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KtvXuLyId = table.Column<int>(type: "integer", nullable: true),
                    ThoiGianCapNhat = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    ThoiGianHoanTat = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketSuCo", x => x.Id);
                    table.CheckConstraint("CK_Ticket_TrangThai", "\"TrangThai\" IN ('Chờ xử lý', 'Đang sửa chữa', 'Báo cáo cấp trên', 'Đã khắc phục')");
                    table.ForeignKey(
                        name: "FK_TicketSuCo_NguoiDung_KtvXuLyId",
                        column: x => x.KtvXuLyId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketSuCo_NguoiDung_NguoiBaoId",
                        column: x => x.NguoiBaoId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketSuCo_ThietBi_MaThietBi",
                        column: x => x.MaThietBi,
                        principalTable: "ThietBi",
                        principalColumn: "MaThietBi",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaThongBao = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, computedColumnSql: "('TB' || CASE WHEN \"Id\" < 1000 THEN lpad(\"Id\"::text, 3, '0') ELSE \"Id\"::text END)", stored: true),
                    VaiTroNhan = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    NguoiNhanId = table.Column<int>(type: "integer", nullable: true),
                    NguoiNhan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NoiDung = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: false),
                    TrangThai = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    YeuCauId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongBao_NguoiDung_NguoiNhanId",
                        column: x => x.NguoiNhanId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThongBao_YeuCauDoiPhong_YeuCauId",
                        column: x => x.YeuCauId,
                        principalTable: "YeuCauDoiPhong",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LichDay_GV_Ngay",
                table: "LichDay",
                columns: new[] { "GiangVienId", "Ngay" });

            migrationBuilder.CreateIndex(
                name: "IX_LichDay_Phong_Ngay",
                table: "LichDay",
                columns: new[] { "MaPhong", "Ngay" })
                .Annotation("Npgsql:IndexInclude", new[] { "BatDau", "KetThuc", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "UQ_NguoiDung_TenDangNhap",
                table: "NguoiDung",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_NhatKy_LichDay",
                table: "NhatKyCheckIn",
                column: "LichDayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThietBi_MaPhong",
                table: "ThietBi",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_NguoiNhanId",
                table: "ThongBao",
                column: "NguoiNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_VaiTro_TrangThai",
                table: "ThongBao",
                columns: new[] { "VaiTroNhan", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_YeuCauId",
                table: "ThongBao",
                column: "YeuCauId");

            migrationBuilder.CreateIndex(
                name: "IX_Ticket_MaThietBi",
                table: "TicketSuCo",
                column: "MaThietBi");

            migrationBuilder.CreateIndex(
                name: "IX_Ticket_TrangThai",
                table: "TicketSuCo",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_TicketSuCo_KtvXuLyId",
                table: "TicketSuCo",
                column: "KtvXuLyId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketSuCo_NguoiBaoId",
                table: "TicketSuCo",
                column: "NguoiBaoId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDoiPhong_GiangVienId",
                table: "YeuCauDoiPhong",
                column: "GiangVienId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDoiPhong_NguoiDuyetId",
                table: "YeuCauDoiPhong",
                column: "NguoiDuyetId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDoiPhong_PhongCu",
                table: "YeuCauDoiPhong",
                column: "PhongCu");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDoiPhong_PhongMoi",
                table: "YeuCauDoiPhong",
                column: "PhongMoi");

            migrationBuilder.CreateIndex(
                name: "UX_YeuCau_ChoDuyet",
                table: "YeuCauDoiPhong",
                column: "LichDayId",
                unique: true,
                filter: "\"TrangThai\" = 'Chờ duyệt'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GhiNhanTam",
                schema: "public");

            migrationBuilder.DropTable(
                name: "NhatKyCheckIn");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropTable(
                name: "TicketSuCo");

            migrationBuilder.DropTable(
                name: "YeuCauDoiPhong");

            migrationBuilder.DropTable(
                name: "ThietBi");

            migrationBuilder.DropTable(
                name: "LichDay");

            migrationBuilder.DropTable(
                name: "NguoiDung");

            migrationBuilder.DropTable(
                name: "Phong");
        }
    }
}
