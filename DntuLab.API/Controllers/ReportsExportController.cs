using ClosedXML.Excel;
using DntuLab.Api.Data;
using DntuLab.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/reports/hieu-suat")]
[Authorize(Roles = "KTV,QL")]
public class ReportsExportController : ControllerBase
{
    private readonly LabDbContext _db;
    public ReportsExportController(LabDbContext db) => _db = db;

    private sealed record RoomRow(string MaPhong, string Cum, int SoLich, int SoCaThucTe, int SucChua);
    private sealed record TicketRow(string TrangThai, int SoLuong);
    private sealed record TicketDetail(string MaTicket, string MaThietBi, string TenThietBi, string MaPhong, string LoaiLoi, string TrangThai, DateTime TaoLuc, DateTime? HoanTatLuc);
    private sealed record AttendanceRow(string MaLich, DateOnly Ngay, string Ca, string MonHoc, string MaPhong, string GiangVien, bool CheckInMuon, int SoPhutTre, DateTime? CheckOut);
    private sealed record ReportData(DateTime GeneratedAt, DateOnly? From, DateOnly? To, int TotalRooms, int TotalDevices, int TotalSchedules, int ActualSessions, int FreeRate, int LateCheckIns, double AverageDelay, double? AverageResolutionHours, List<RoomRow> Rooms, List<TicketRow> Tickets, List<TicketDetail> TicketDetails, List<AttendanceRow> Attendance);

    private async Task<ReportData> LoadReport(DateOnly? from, DateOnly? to)
    {
        if (from.HasValue && to.HasValue && from > to)
            throw new ArgumentException("Từ ngày không được lớn hơn đến ngày.");

        var schedules = _db.LichDays.AsNoTracking();
        if (from.HasValue) schedules = schedules.Where(x => x.Ngay >= from.Value);
        if (to.HasValue) schedules = schedules.Where(x => x.Ngay <= to.Value);

        var scheduleRows = await schedules
            .Select(x => new { x.MaLich, x.Ngay, x.Ca, x.MonHoc, x.MaPhong, x.SoBuoi, x.TrangThai, GiangVien = x.GiangVien.HoTen,
                Log = x.NhatKy == null ? null : new { x.NhatKy.CheckInMuon, x.NhatKy.SoPhutTre, x.NhatKy.ThoiGianCheckOut } })
            .ToListAsync();

        var roomMeta = await _db.Phongs.AsNoTracking()
            .Select(x => new { x.MaPhong, x.Cum, x.SucChua }).ToListAsync();
        var roomRows = scheduleRows.GroupBy(x => x.MaPhong).Select(g =>
        {
            var meta = roomMeta.FirstOrDefault(r => r.MaPhong == g.Key);
            return new RoomRow(g.Key, meta?.Cum ?? "-", g.Count(), g.Count(x => x.TrangThai == TrangThaiLich.DangDienRa || x.TrangThai == TrangThaiLich.DaKetThuc), meta?.SucChua ?? 0);
        }).OrderBy(x => x.MaPhong).ToList();

        var ticketsQuery = _db.Tickets.AsNoTracking();
        if (from.HasValue) { var start = from.Value.ToDateTime(TimeOnly.MinValue); ticketsQuery = ticketsQuery.Where(x => x.ThoiGianTao >= start); }
        if (to.HasValue) { var end = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue); ticketsQuery = ticketsQuery.Where(x => x.ThoiGianTao < end); }
        var ticketDetails = await ticketsQuery
            .OrderByDescending(x => x.ThoiGianTao)
            .Select(x => new TicketDetail(x.MaTicket, x.MaThietBi, x.ThietBi.Ten, x.ThietBi.MaPhong, x.LoaiLoi, x.TrangThai, x.ThoiGianTao, x.ThoiGianHoanTat))
            .ToListAsync();

        var ticketRows = ticketDetails.GroupBy(x => x.TrangThai)
            .Select(g => new TicketRow(g.Key, g.Count())).OrderBy(x => x.TrangThai).ToList();
        var actual = scheduleRows.Count(x => x.TrangThai == TrangThaiLich.DangDienRa || x.TrangThai == TrangThaiLich.DaKetThuc);
        var expectedSlots = roomMeta.Count * 6 * 4;
        var booked = scheduleRows.Sum(x => x.SoBuoi);
        var freeRate = expectedSlots == 0 ? 0 : Math.Clamp((int)Math.Round((1d - (double)booked / expectedSlots) * 100), 0, 100);
        var attendance = scheduleRows.Where(x => x.Log != null).Select(x => new AttendanceRow(x.MaLich, x.Ngay, x.Ca, x.MonHoc, x.MaPhong, x.GiangVien, x.Log!.CheckInMuon, x.Log.SoPhutTre, x.Log.ThoiGianCheckOut)).OrderByDescending(x => x.Ngay).ToList();
        var late = attendance.Where(x => x.CheckInMuon).ToList();
        var resolution = ticketDetails.Where(x => x.HoanTatLuc.HasValue).Select(x => (x.HoanTatLuc!.Value - x.TaoLuc).TotalHours).Where(x => x >= 0).ToList();

        return new ReportData(DateTime.Now, from, to,
            roomMeta.Count, await _db.ThietBis.CountAsync(), scheduleRows.Count, actual, freeRate,
            late.Count, late.Count == 0 ? 0 : late.Average(x => x.SoPhutTre),
            resolution.Count == 0 ? null : resolution.Average(), roomRows, ticketRows, ticketDetails, attendance);
    }

    [HttpGet("excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] DateOnly? tuNgay, [FromQuery] DateOnly? denNgay)
    {
        ReportData r;
        try { r = await LoadReport(tuNgay, denNgay); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Tong quan");
        ws.Cell(1, 1).Value = "DNTULAB - BÁO CÁO HIỆU SUẤT";
        ws.Range(1, 1, 1, 4).Merge().Style.Font.Bold = true;
        ws.Range(1, 1, 1, 4).Style.Font.FontSize = 16;
        ws.Cell(2, 1).Value = "Thời gian xuất"; ws.Cell(2, 2).Value = r.GeneratedAt.ToString("dd/MM/yyyy HH:mm:ss");
        ws.Cell(3, 1).Value = "Khoảng ngày"; ws.Cell(3, 2).Value = DateRange(r.From, r.To);
        var kpis = new (string Name, object Value)[] {
            ("Tổng số phòng", r.TotalRooms), ("Tổng thiết bị", r.TotalDevices), ("Tổng lịch dạy", r.TotalSchedules),
            ("Số ca thực tế", r.ActualSessions), ("Tỷ lệ phòng trống (%)", r.FreeRate), ("Check-in muộn", r.LateCheckIns),
            ("Trễ trung bình (phút)", Math.Round(r.AverageDelay, 1)), ("Xử lý Ticket TB (giờ)", r.AverageResolutionHours.HasValue ? Math.Round(r.AverageResolutionHours.Value, 2) : "Chưa có dữ liệu")
        };
        for (var i = 0; i < kpis.Length; i++) { ws.Cell(5 + i, 1).Value = kpis[i].Name; ws.Cell(5 + i, 2).Value = Convert.ToString(kpis[i].Value) ?? ""; }
        StyleHeader(ws.Range(5, 1, 5, 2)); ws.Columns().AdjustToContents();

        var rooms = wb.Worksheets.Add("Hieu suat phong");
        string[] roomHeaders = ["Mã phòng", "Cụm", "Tổng lịch", "Ca thực tế", "Sức chứa"];
        for (int c = 0; c < roomHeaders.Length; c++) rooms.Cell(1, c + 1).Value = roomHeaders[c];
        for (int i = 0; i < r.Rooms.Count; i++) { var x = r.Rooms[i]; var row = i + 2; rooms.Cell(row, 1).Value = x.MaPhong; rooms.Cell(row, 2).Value = x.Cum; rooms.Cell(row, 3).Value = x.SoLich; rooms.Cell(row, 4).Value = x.SoCaThucTe; rooms.Cell(row, 5).Value = x.SucChua; }
        StyleHeader(rooms.Range(1, 1, 1, roomHeaders.Length)); rooms.Columns().AdjustToContents();

        var tickets = wb.Worksheets.Add("Ticket theo trang thai");
        tickets.Cell(1, 1).Value = "Trạng thái"; tickets.Cell(1, 2).Value = "Số lượng";
        for (int i = 0; i < r.Tickets.Count; i++) { tickets.Cell(i + 2, 1).Value = r.Tickets[i].TrangThai; tickets.Cell(i + 2, 2).Value = r.Tickets[i].SoLuong; }
        StyleHeader(tickets.Range(1, 1, 1, 2)); tickets.Columns().AdjustToContents();

        var details = wb.Worksheets.Add("Chi tiet Ticket");
        details.Cell(1, 1).Value = "Mã Ticket"; details.Cell(1, 2).Value = "Mã thiết bị"; details.Cell(1, 3).Value = "Tên thiết bị"; details.Cell(1, 4).Value = "Phòng"; details.Cell(1, 5).Value = "Loại lỗi"; details.Cell(1, 6).Value = "Trạng thái"; details.Cell(1, 7).Value = "Thời gian tạo"; details.Cell(1, 8).Value = "Thời gian hoàn tất";
        for (int i = 0; i < r.TicketDetails.Count; i++) { var x = r.TicketDetails[i]; var row = i + 2; details.Cell(row, 1).Value = x.MaTicket; details.Cell(row, 2).Value = x.MaThietBi; details.Cell(row, 3).Value = x.TenThietBi; details.Cell(row, 4).Value = x.MaPhong; details.Cell(row, 5).Value = x.LoaiLoi; details.Cell(row, 6).Value = x.TrangThai; details.Cell(row, 7).Value = x.TaoLuc; if (x.HoanTatLuc.HasValue) details.Cell(row, 8).Value = x.HoanTatLuc.Value; }
        StyleHeader(details.Range(1, 1, 1, 8)); details.Columns().AdjustToContents(); details.SheetView.FreezeRows(1);

        var checkins = wb.Worksheets.Add("Check-in");
        checkins.Cell(1, 1).Value = "Mã lịch"; checkins.Cell(1, 2).Value = "Ngày"; checkins.Cell(1, 3).Value = "Ca"; checkins.Cell(1, 4).Value = "Môn học"; checkins.Cell(1, 5).Value = "Phòng"; checkins.Cell(1, 6).Value = "Giảng viên"; checkins.Cell(1, 7).Value = "Check-in muộn"; checkins.Cell(1, 8).Value = "Số phút trễ"; checkins.Cell(1, 9).Value = "Check-out";
        for (int i = 0; i < r.Attendance.Count; i++) { var x = r.Attendance[i]; var row = i + 2; checkins.Cell(row, 1).Value = x.MaLich; checkins.Cell(row, 2).Value = x.Ngay.ToString("dd/MM/yyyy"); checkins.Cell(row, 3).Value = x.Ca; checkins.Cell(row, 4).Value = x.MonHoc; checkins.Cell(row, 5).Value = x.MaPhong; checkins.Cell(row, 6).Value = x.GiangVien; checkins.Cell(row, 7).Value = x.CheckInMuon ? "Có" : "Không"; checkins.Cell(row, 8).Value = x.SoPhutTre; if (x.CheckOut.HasValue) checkins.Cell(row, 9).Value = x.CheckOut.Value; }
        StyleHeader(checkins.Range(1, 1, 1, 9)); checkins.Columns().AdjustToContents(); checkins.SheetView.FreezeRows(1);

        using var stream = new MemoryStream(); wb.SaveAs(stream); stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DNTULab_BaoCaoHieuSuat_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] DateOnly? tuNgay, [FromQuery] DateOnly? denNgay)
    {
        ReportData r;
        try { r = await LoadReport(tuNgay, denNgay); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        QuestPDF.Settings.License = LicenseType.Community;
        var bytes = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4); page.Margin(32); page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Column(c => { c.Item().Text("DNTULAB").FontSize(19).Bold().FontColor("#8B0000"); c.Item().Text("BÁO CÁO HIỆU SUẤT HỆ THỐNG LAB").FontSize(14).Bold(); c.Item().Text($"Khoảng ngày: {DateRange(r.From, r.To)} | Xuất lúc: {r.GeneratedAt:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken2); });
            page.Content().PaddingVertical(14).Column(col =>
            {
                col.Spacing(10);
                col.Item().Text("1. Chỉ số tổng quan").Bold().FontSize(12);
                col.Item().Table(t => { t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                    AddKpi(t, "Số phòng", r.TotalRooms.ToString()); AddKpi(t, "Thiết bị", r.TotalDevices.ToString()); AddKpi(t, "Lịch dạy", r.TotalSchedules.ToString()); AddKpi(t, "Ca thực tế", r.ActualSessions.ToString());
                    AddKpi(t, "Phòng trống", $"{r.FreeRate}%"); AddKpi(t, "Check-in muộn", r.LateCheckIns.ToString()); AddKpi(t, "Trễ TB", $"{r.AverageDelay:0.0} phút"); AddKpi(t, "Xử lý Ticket TB", r.AverageResolutionHours.HasValue ? $"{r.AverageResolutionHours.Value:0.00} giờ" : "N/A"); });
                col.Item().Text("2. Hiệu suất theo phòng").Bold().FontSize(12);
                col.Item().Table(t => { t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); }); TableHeader(t, "Phòng", "Cụm", "Tổng lịch", "Ca thực tế"); foreach (var x in r.Rooms) { t.Cell().Element(Cell).Text(x.MaPhong); t.Cell().Element(Cell).Text(x.Cum); t.Cell().Element(Cell).Text(x.SoLich.ToString()); t.Cell().Element(Cell).Text(x.SoCaThucTe.ToString()); } });
                col.Item().Text("3. Ticket theo trạng thái").Bold().FontSize(12);
                col.Item().Table(t => { t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(); }); TableHeader(t, "Trạng thái", "Số lượng"); foreach (var x in r.Tickets) { t.Cell().Element(Cell).Text(x.TrangThai); t.Cell().Element(Cell).Text(x.SoLuong.ToString()); } if (r.Tickets.Count == 0) { t.Cell().ColumnSpan(2).Element(Cell).Text("Không có Ticket trong khoảng thời gian này."); } });
                col.Item().Text("4. Danh sách Ticket gần nhất").Bold().FontSize(12);
                col.Item().Table(t => { t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(2); c.RelativeColumn(2); }); TableHeader(t, "Mã", "Phòng", "Lỗi", "Trạng thái"); foreach (var x in r.TicketDetails.Take(20)) { t.Cell().Element(Cell).Text(x.MaTicket); t.Cell().Element(Cell).Text(x.MaPhong); t.Cell().Element(Cell).Text(x.LoaiLoi); t.Cell().Element(Cell).Text(x.TrangThai); } if (r.TicketDetails.Count == 0) t.Cell().ColumnSpan(4).Element(Cell).Text("Không có Ticket trong khoảng thời gian này."); });
                col.Item().Text("5. Check-in muộn").Bold().FontSize(12);
                col.Item().Text($"Tổng số lượt check-in có ghi nhận: {r.Attendance.Count}; check-in muộn: {r.LateCheckIns}; số phút trễ trung bình của các lượt muộn: {r.AverageDelay:0.0} phút.");
            });
            page.Footer().AlignCenter().Text(x => { x.Span("DNTULab · Trang "); x.CurrentPageNumber(); x.Span("/"); x.TotalPages(); });
        })).GeneratePdf();
        return File(bytes, "application/pdf", $"DNTULab_BaoCaoHieuSuat_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    private static string DateRange(DateOnly? from, DateOnly? to) => $"{(from.HasValue ? from.Value.ToString("dd/MM/yyyy") : "Tất cả")} - {(to.HasValue ? to.Value.ToString("dd/MM/yyyy") : "hiện tại")}";
    private static void StyleHeader(IXLRange range) { range.Style.Font.Bold = true; range.Style.Fill.BackgroundColor = XLColor.FromHtml("#8B0000"); range.Style.Font.FontColor = XLColor.White; }
    private static IContainer Cell(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(3);
    private static void TableHeader(TableDescriptor t, params string[] values) { foreach (var value in values) t.Cell().Element(c => c.Background("#8B0000").Padding(5)).Text(value).FontColor(Colors.White).Bold(); }
    private static void AddKpi(TableDescriptor t, string label, string value) { t.Cell().Element(c => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6)).Column(x => { x.Item().Text(label).FontColor(Colors.Grey.Darken2).FontSize(8); x.Item().Text(value).Bold().FontSize(13); }); }
}
