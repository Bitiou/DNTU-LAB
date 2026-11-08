using DntuLab.Api.Data;
using DntuLab.Api.Domain;
using DntuLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/LichDay")]
[Authorize(Roles = "GV,KTV,QL")]
public class TuDongDongCaController : ControllerBase
{
    private static readonly TimeSpan TuDongCheckInSau = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan GiaHanCheckOut = TimeSpan.FromMinutes(30);

    // Ngăn các request đồng bộ giờ Demo chạy đồng thời trong cùng tiến trình API.
    // React StrictMode/useEffect có thể phát sinh hai request gần nhau khi chạy Development.
    private static readonly SemaphoreSlim DongBoGate = new(1, 1);

    private readonly LabDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public TuDongDongCaController(
        LabDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    /// <summary>
    /// Đồng bộ toàn bộ ca theo thời gian Demo:
    /// - BatDau - 30 phút đến trước BatDau + 30 phút: cho phép Check-in thủ công.
    /// - Tại BatDau + 30 phút: tự Check-in muộn nếu chưa có nhật ký.
    /// - Có Check-in, chưa Check-out: Đang diễn ra.
    /// - Tại KetThuc + 30 phút: tự Check-out, không xác nhận tắt điện/khóa cửa.
    /// - Chỉ hoàn tác nhật ký tự động trong Development khi tua ngược.
    /// </summary>
    [HttpPost("tu-dong-checkin-checkout")]
    public async Task<IActionResult> TuDongCheckInCheckOut(
        CancellationToken cancellationToken)
    {
        await DongBoGate.WaitAsync(cancellationToken);

        try
        {
            // EnableRetryOnFailure yêu cầu transaction thủ công nằm bên trong execution strategy.
            var strategy = _context.Database.CreateExecutionStrategy();

            var result = await strategy.ExecuteAsync(async () =>
            {
                // Mỗi lần retry phải đọc lại trạng thái database, không dùng entity cũ
                // còn nằm trong ChangeTracker từ transaction đã rollback.
                _context.ChangeTracker.Clear();

                var now = DemoClock.GetNow(Request, _environment);

                await using var transaction =
                    await _context.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.ReadCommitted,
                        cancellationToken);

                var schedules = await _context.LichDays
                    .Include(x => x.NhatKy)
                    .OrderBy(x => x.Ngay)
                    .ThenBy(x => x.BatDau)
                    .ThenBy(x => x.Id)
                    .ToListAsync(cancellationToken);

                var autoCheckIns = 0;
                var autoCheckOuts = 0;
                var revertedCheckIns = 0;
                var revertedCheckOuts = 0;

                foreach (var lesson in schedules)
                {
                    var startAt = lesson.Ngay.ToDateTime(lesson.BatDau);
                    var endAt = lesson.Ngay.ToDateTime(lesson.KetThuc);
                    var autoCheckInAt = startAt.Add(TuDongCheckInSau);
                    var autoCheckOutAt = endAt.Add(GiaHanCheckOut);
                    var log = lesson.NhatKy;

                    if (log != null && _environment.IsDevelopment())
                    {
                        // Chỉ hoàn tác Check-in được hệ thống tự tạo đúng mốc +30 phút.
                        // Không xóa Check-in thủ công.
                        var isAutoCheckIn =
                            log.CheckInMuon &&
                            log.SoPhutTre == 30 &&
                            log.ThoiGianCheckIn == autoCheckInAt;

                        if (isAutoCheckIn &&
                            now < autoCheckInAt &&
                            !log.DaTatDien.HasValue &&
                            !log.DaKhoaCua.HasValue)
                        {
                            _context.NhatKys.Remove(log);
                            lesson.NhatKy = null;
                            log = null;
                            lesson.TrangThai = TrangThaiLich.ChuaCheckIn;
                            revertedCheckIns++;
                        }
                        else
                        {
                            // Chỉ hoàn tác Check-out tự động tại đúng mốc KetThuc +30,
                            // nhận diện bằng hai xác nhận vật lý đều NULL.
                            var isAutoCheckOut =
                                log.ThoiGianCheckOut == autoCheckOutAt &&
                                !log.DaTatDien.HasValue &&
                                !log.DaKhoaCua.HasValue;

                            if (isAutoCheckOut && now < autoCheckOutAt)
                            {
                                log.ThoiGianCheckOut = null;
                                lesson.TrangThai = TrangThaiLich.DangDienRa;
                                revertedCheckOuts++;
                            }
                        }
                    }

                    if (log == null)
                    {
                        if (now >= autoCheckInAt)
                        {
                            log = new NhatKyCheckIn
                            {
                                LichDayId = lesson.Id,
                                ThoiGianCheckIn = autoCheckInAt,
                                CheckInMuon = true,
                                SoPhutTre = 30
                            };

                            _context.NhatKys.Add(log);
                            lesson.NhatKy = log;
                            lesson.TrangThai = TrangThaiLich.DangDienRa;
                            autoCheckIns++;
                        }
                        else
                        {
                            // Chưa tới mốc tự Check-in. Trong cửa sổ -30/+30,
                            // UI hiển thị nút Check-in; ngoài cửa sổ thì chưa đến giờ.
                            lesson.TrangThai = TrangThaiLich.ChuaCheckIn;
                            continue;
                        }
                    }
                    else if (log.ThoiGianCheckIn > now)
                    {
                        // Nhật ký thủ công có timestamp tương lai được giữ nguyên;
                        // không cho trạng thái tương lai làm sai trạng thái Demo hiện tại.
                        lesson.TrangThai = TrangThaiLich.ChuaCheckIn;
                        continue;
                    }
                    else if (log.ThoiGianCheckOut.HasValue &&
                             log.ThoiGianCheckOut.Value <= now)
                    {
                        lesson.TrangThai = TrangThaiLich.DaKetThuc;
                        continue;
                    }
                    else
                    {
                        lesson.TrangThai = TrangThaiLich.DangDienRa;
                    }

                    if (log != null &&
                        !log.ThoiGianCheckOut.HasValue &&
                        now >= autoCheckOutAt)
                    {
                        log.ThoiGianCheckOut = autoCheckOutAt;
                        log.DaTatDien = null;
                        log.DaKhoaCua = null;
                        lesson.TrangThai = TrangThaiLich.DaKetThuc;
                        autoCheckOuts++;
                    }
                }

                if (autoCheckIns > 0 ||
                    autoCheckOuts > 0 ||
                    revertedCheckIns > 0 ||
                    revertedCheckOuts > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                return new
                {
                    Now = now,
                    AutoCheckIns = autoCheckIns,
                    AutoCheckOuts = autoCheckOuts,
                    RevertedCheckIns = revertedCheckIns,
                    RevertedCheckOuts = revertedCheckOuts
                };
            });

            return Ok(new
            {
                success = true,
                message =
                    $"Đồng bộ giờ Demo thành công. Tự Check-in: {result.AutoCheckIns}; " +
                    $"tự Check-out: {result.AutoCheckOuts}; " +
                    $"hoàn tác Check-in tự động: {result.RevertedCheckIns}; " +
                    $"hoàn tác Check-out tự động: {result.RevertedCheckOuts}.",
                thoiGianDemo = result.Now,
                cuaSoCheckInTruocPhut = 30,
                tuDongCheckInSauPhut = 30,
                giaHanCheckOutPhut = 30,
                soCaTuDongCheckIn = result.AutoCheckIns,
                soCaTuDongCheckOut = result.AutoCheckOuts,
                soCaHoanTacCheckIn = result.RevertedCheckIns,
                soCaHoanTacCheckOut = result.RevertedCheckOuts
            });
        }
        finally
        {
            DongBoGate.Release();
        }
    }
}
