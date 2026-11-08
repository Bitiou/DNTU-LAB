
using ClosedXML.Excel;
using DntuLab.Api.Data;
using DntuLab.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/ImportLichExcel")]
[Authorize(Roles = Roles.QL)]
public class ImportLichExcelController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private readonly LabDbContext _db;

    public ImportLichExcelController(LabDbContext db)
    {
        _db = db;
    }

    // GET: api/ImportLichExcel/mau
    // Tải file Excel mẫu
    [HttpGet("mau")]
    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();

        var sheet = workbook.Worksheets.Add("LichDay");

        var headers = new[]
        {
            "Ngay", "Ca", "BatDau", "KetThuc",
            "MonHoc", "MaPhong", "TenDangNhapGV", "SoBuoi"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        sheet.Cell(2, 1).Value = new DateTime(2026, 12, 1);
        sheet.Cell(2, 1).Style.DateFormat.Format = "dd/MM/yyyy";
        sheet.Cell(2, 2).Value = "Ca 1";
        sheet.Cell(2, 3).Value = "07:00";
        sheet.Cell(2, 4).Value = "09:00";
        sheet.Cell(2, 5).Value = "Lập trình Web API";
        sheet.Cell(2, 6).Value = "F507";
        sheet.Cell(2, 7).Value = "gv01";
        sheet.Cell(2, 8).Value = 1;

        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Mau_Import_LichDay_DNTU.xlsx"
        );
    }

    // POST: api/ImportLichExcel
    // Đọc Excel và lưu vào SQL Server
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Import(
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Vui lòng chọn file Excel."
            });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                success = false,
                message = "File vượt quá giới hạn 10 MB."
            });
        }

        if (!Path.GetExtension(file.FileName)
            .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                success = false,
                message = "Chỉ hỗ trợ file .xlsx."
            });
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Position = 0;

        XLWorkbook workbook;

        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch
        {
            return BadRequest(new
            {
                success = false,
                message = "File Excel không hợp lệ."
            });
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault();

            if (sheet == null || sheet.LastRowUsed() == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "File Excel không có dữ liệu."
                });
            }

            // Đọc tên cột
            var columns = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var cell in sheet.Row(1).CellsUsed())
            {
                var name = cell.GetString().Trim();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    columns[name] = cell.Address.ColumnNumber;
                }
            }

            var required = new[]
            {
                "Ngay", "Ca", "BatDau", "KetThuc",
                "MonHoc", "MaPhong", "TenDangNhapGV"
            };

            var missing = required
                .Where(x => !columns.ContainsKey(x))
                .ToArray();

            if (missing.Length > 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Thiếu cột: " + string.Join(", ", missing)
                });
            }

            string ReadCell(IXLRow row, string name)
            {
                return row.Cell(columns[name])
                    .GetFormattedString()
                    .Trim();
            }

            // Transaction để hạn chế import đồng thời gây trùng dữ liệu
            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, ct
                );

            var teachers = await _db.NguoiDungs
                .Where(x =>
                    x.HoatDong &&
                    x.VaiTro == Roles.GV &&
                    (x.TenDangNhap == "gv01" ||
                     x.TenDangNhap == "gv02"))
                .ToDictionaryAsync(
                    x => x.TenDangNhap.ToLower(), ct
                );

            var rooms = await _db.Phongs
                .Select(x => x.MaPhong)
                .ToListAsync(ct);

            var existing = await _db.LichDays
                .AsNoTracking()
                .ToListAsync(ct);

            var parsed = new List<LichDay>();
            var errors = new List<string>();
            int duplicateCount = 0;

            int lastRow = sheet.LastRowUsed()!.RowNumber();

            for (int rowNo = 2; rowNo <= lastRow; rowNo++)
            {
                var row = sheet.Row(rowNo);

                if (row.IsEmpty())
                    continue;

                try
                {
                    var username =
                        ReadCell(row, "TenDangNhapGV")
                        .ToLowerInvariant();

                    if (!teachers.TryGetValue(username, out var teacher))
                    {
                        throw new FormatException(
                            $"Tài khoản {username} không hợp lệ."
                        );
                    }

                    var room = ReadCell(row, "MaPhong")
                        .ToUpperInvariant();

                    if (!rooms.Contains(
                        room, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new FormatException(
                            $"Phòng {room} không tồn tại."
                        );
                    }

                    if (!TryReadDate(
                        row.Cell(columns["Ngay"]), out var date))
                    {
                        throw new FormatException("Ngày không hợp lệ.");
                    }

                    if (!TryReadTime(
                        row.Cell(columns["BatDau"]), out var start) ||
                        !TryReadTime(
                        row.Cell(columns["KetThuc"]), out var end) ||
                        start >= end)
                    {
                        throw new FormatException("Giờ học không hợp lệ.");
                    }

                    var shift = ReadCell(row, "Ca");
                    var subject = ReadCell(row, "MonHoc");

                    if (string.IsNullOrWhiteSpace(shift) ||
                        shift.Length > 10)
                    {
                        throw new FormatException("Ca học không hợp lệ.");
                    }

                    if (string.IsNullOrWhiteSpace(subject) ||
                        subject.Length > 150)
                    {
                        throw new FormatException("Môn học không hợp lệ.");
                    }

                    int sessions = 1;
                    var sessionText = columns.ContainsKey("SoBuoi")
                        ? ReadCell(row, "SoBuoi")
                        : "";

                    if (!string.IsNullOrWhiteSpace(sessionText) &&
                        (!int.TryParse(sessionText, out sessions) ||
                         sessions < 1))
                    {
                        throw new FormatException(
                            "SoBuoi phải lớn hơn 0."
                        );
                    }

                    // Kiểm tra lịch đã tồn tại cùng giảng viên/ngày/ca
                    bool duplicate = existing.Any(x =>
                        x.GiangVienId == teacher.Id &&
                        x.Ngay == date &&
                        x.Ca.Equals(
                            shift,
                            StringComparison.OrdinalIgnoreCase
                        )
                    ) || parsed.Any(x =>
                        x.GiangVienId == teacher.Id &&
                        x.Ngay == date &&
                        x.Ca.Equals(
                            shift,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                    if (duplicate)
                    {
                        duplicateCount++;
                        continue;
                    }

                    // Kiểm tra trùng phòng
                    bool roomConflict = existing.Any(x =>
                        x.Ngay == date &&
                        x.MaPhong == room &&
                        x.TrangThai != "Đã hủy" &&
                        x.BatDau < end &&
                        x.KetThuc > start
                    ) || parsed.Any(x =>
                        x.Ngay == date &&
                        x.MaPhong == room &&
                        x.BatDau < end &&
                        x.KetThuc > start
                    );

                    if (roomConflict)
                    {
                        throw new FormatException(
                            $"Phòng {room} bị trùng lịch."
                        );
                    }

                    // Kiểm tra trùng giờ giảng viên
                    bool teacherConflict = existing.Any(x =>
                        x.Ngay == date &&
                        x.GiangVienId == teacher.Id &&
                        x.BatDau < end &&
                        x.KetThuc > start
                    ) || parsed.Any(x =>
                        x.Ngay == date &&
                        x.GiangVienId == teacher.Id &&
                        x.BatDau < end &&
                        x.KetThuc > start
                    );

                    if (teacherConflict)
                    {
                        throw new FormatException(
                            $"Giảng viên {username} bị trùng giờ."
                        );
                    }

                    parsed.Add(new LichDay
                    {
                        Ngay = date,
                        Ca = shift,
                        BatDau = start,
                        KetThuc = end,
                        MonHoc = subject,
                        MaPhong = room,
                        GiangVienId = teacher.Id,
                        TrangThai = TrangThaiLich.ChuaCheckIn,
                        SoBuoi = sessions
                    });
                }
                catch (FormatException ex)
                {
                    errors.Add($"Dòng {rowNo}: {ex.Message}");
                }
            }

            // Có lỗi thì không lưu một phần
            if (errors.Count > 0)
            {
                await transaction.RollbackAsync(ct);

                return UnprocessableEntity(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ. Không có lịch nào được lưu.",
                    errors,
                    duplicateCount,
                    importedCount = 0
                });
            }

            if (parsed.Count == 0)
            {
                await transaction.CommitAsync(ct);

                return Ok(new
                {
                    success = true,
                    message = "Không có lịch mới để import.",
                    importedCount = 0,
                    duplicateCount
                });
            }

            // Lưu dữ liệu thật vào database
            _db.LichDays.AddRange(parsed);

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Ok(new
            {
                success = true,
                message = $"Đã import {parsed.Count} lịch vào SQL Server.",
                importedCount = parsed.Count,
                duplicateCount
            });
        }
    }

    private static bool TryReadDate(
        IXLCell cell, out DateOnly date)
    {
        if (cell.TryGetValue<DateTime>(out var dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }

        var value = cell.GetFormattedString().Trim();

        foreach (var format in new[]
        {
            "dd/MM/yyyy", "d/M/yyyy",
            "yyyy-MM-dd", "MM/dd/yyyy"
        })
        {
            if (DateOnly.TryParseExact(
                value,
                format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
            {
                return true;
            }
        }

        date = default;
        return false;
    }

    private static bool TryReadTime(
        IXLCell cell, out TimeOnly time)
    {
        if (cell.TryGetValue<TimeSpan>(out var span) &&
            span >= TimeSpan.Zero &&
            span < TimeSpan.FromDays(1))
        {
            time = TimeOnly.FromTimeSpan(span);
            return true;
        }

        if (cell.TryGetValue<DateTime>(out var dt))
        {
            time = TimeOnly.FromDateTime(dt);
            return true;
        }

        var value = cell.GetFormattedString().Trim();

        return TimeOnly.TryParse(
            value,
            CultureInfo.GetCultureInfo("vi-VN"),
            DateTimeStyles.None,
            out time
        ) || TimeOnly.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time
        );
    }
}
