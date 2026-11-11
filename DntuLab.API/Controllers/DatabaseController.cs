using DntuLab.Api.Data;
using DntuLab.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
namespace DntuLab.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly LabDbContext _context;

    public DatabaseController(LabDbContext context)
    {
        _context = context;
    }


    /// <summary>Khoi phuc du lieu demo thiet bi, Ticket va yeu cau doi phong UC11-UC12.</summary>
    [HttpPost("reset-demo")]
    [Authorize(Roles = "GV,KTV,QL")]
    public async Task<IActionResult> ResetDemo()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId)) return Unauthorized();

        var devices = await _context.ThietBis.OrderBy(x => x.MaThietBi).ToListAsync();
        if (devices.Count == 0)
            return Conflict(new { success = false, message = "Chua co danh muc thiet bi trong PostgreSQL. Hay nap du lieu phong/thiet bi truoc." });

        // EnableRetryOnFailure yeu cau transaction tu tao phai chay ben trong execution strategy.
        // Dung generic IActionResult de lambda co kieu Task<IActionResult>, tranh loi async lambda Task.
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            // Reset Demo phai dua cac ca nam trong tuong lai ve trang thai ban dau,
            // ke ca nhat ky duoc tao boi phien ban catch-up cu khong con nhan dien duoc.
            // Chi xoa nhat ky cua ca chua den theo THOI GIAN THUC Viet Nam; cac ca da
            // dien ra trong thuc te (ngay <= hom nay va da bat dau) duoc giu lai.
            TimeZoneInfo vietnamZone;
            try { vietnamZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
            catch (TimeZoneNotFoundException) { vietnamZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
            var realNowOffset = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, vietnamZone);
            var realNow = realNowOffset.DateTime;
            var realToday = DateOnly.FromDateTime(realNow);
            var realTime = TimeOnly.FromDateTime(realNow);

            // Reset Demo khong duoc xoa lich giang day/import hoac nhat ky cua
            // cac ngay khac. Chi xoa ghi nhan tam de khoi phuc bo demo sach.
            var allTemporaryNotes = await _context.GhiNhanTams.ToListAsync();

            // Reset UC11-12: xoa toan bo yeu cau doi phong cu va thong bao lien quan,
            // sau do se tao lai bo yeu cau demo co dinh. Truoc khi xoa, khoi phuc phong
            // goc cho cac yeu cau da duyet/tu dong duyet.
            var allRoomRequests = await _context.YeuCaus
                .OrderBy(x => x.ThoiGianTao)
                .ToListAsync();
            var approvedRequests = allRoomRequests
                .Where(x => x.TrangThai == TrangThaiYeuCau.DaDuyet ||
                            x.TrangThai == TrangThaiYeuCau.TuDongDuyet)
                .ToList();
            var changedScheduleIds = approvedRequests.Select(x => x.LichDayId).Distinct().ToList();
            var changedSchedules = await _context.LichDays
                .Where(x => changedScheduleIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);
            foreach (var group in approvedRequests.GroupBy(x => x.LichDayId))
            {
                if (changedSchedules.TryGetValue(group.Key, out var changedLesson))
                    changedLesson.MaPhong = group.OrderBy(x => x.ThoiGianTao).First().PhongCu;
            }

            var allRequestIds = allRoomRequests.Select(x => x.Id).ToList();
            // Reset toàn bộ thông báo Demo cũ; thông báo hậu kiểm sẽ được tạo lại
            // tương ứng với các yêu cầu khẩn cấp mới bên dưới.
            var oldRoomNotifications = await _context.ThongBaos.ToListAsync();

            // Xóa FK con trước: ThongBao -> YeuCauDoiPhong.
            _context.ThongBaos.RemoveRange(oldRoomNotifications);
            _context.YeuCaus.RemoveRange(allRoomRequests);
            _context.GhiNhanTams.RemoveRange(allTemporaryNotes);

            if (oldRoomNotifications.Count > 0 || allRoomRequests.Count > 0 ||
                allTemporaryNotes.Count > 0)
                await _context.SaveChangesAsync();

            var allLogsForReset = await _context.NhatKys
                .Include(x => x.LichDay)
                .ToListAsync();

            var futureScheduleIds = await _context.LichDays
                .Where(x => x.Ngay > realToday ||
                            (x.Ngay == realToday && x.BatDau > realTime))
                .Select(x => x.Id)
                .ToListAsync();

            var futureIdSet = futureScheduleIds.ToHashSet();
            var logsToReset = allLogsForReset
                .Where(x => futureIdSet.Contains(x.LichDayId))
                .ToList();

            foreach (var log in logsToReset)
                log.LichDay.TrangThai = TrangThaiLich.ChuaCheckIn;

            // Ca tuong lai tro ve chua check-in ke ca truoc do chi bi doi TrangThai
            // ma khong co NhatKy (du lieu cu hoac thao tac test).
            var futureSchedules = await _context.LichDays
                .Where(x => futureIdSet.Contains(x.Id))
                .ToListAsync();
            foreach (var schedule in futureSchedules)
                schedule.TrangThai = TrangThaiLich.ChuaCheckIn;

            _context.NhatKys.RemoveRange(logsToReset);
            if (logsToReset.Count > 0 || futureSchedules.Count > 0)
                await _context.SaveChangesAsync();

            // Tao lai bo du lieu demo UC11-12: 2 yeu cau co ke hoach cho QL duyet
            // va 2 ca doi phong khan cap da tu dong duyet, cho hau kiem sau 08:00.
            var roomsForChange = await _context.Phongs
                .Where(p => p.Cum == CumPhong.F || p.Cum == CumPhong.G)
                .OrderBy(p => p.MaPhong)
                .ToListAsync();
            // Tao du lieu mau tren cac ca F/G chua dien ra, khong gioi han
            // cung ngay seed co dinh de Reset van hoat dong khi lich duoc cap nhat.
            var demoLessons = await _context.LichDays
                .Where(l => (l.MaPhong.StartsWith("F") || l.MaPhong.StartsWith("G")) &&
                            (l.Ngay > realToday ||
                             (l.Ngay == realToday && l.BatDau > realTime)))
                .OrderBy(l => l.Ngay).ThenBy(l => l.BatDau).ToListAsync();

            var seededRoomRequests = 0;
            var seededAuditNotifications = 0;
            var usedLessonIds = new HashSet<int>();

            // Neu khong con ca F/G tuong lai trong khoang seed, van hoan tat reset
            // thiet bi/Ticket va attendance. Chi bo qua phan tao yeu cau doi phong mau.

            string? FindAvailableRoom(LichDay lesson)
            {
                var sourceRoom = roomsForChange.FirstOrDefault(p => p.MaPhong == lesson.MaPhong);
                if (sourceRoom == null) return null;

                var candidates = roomsForChange
                    .Where(p => p.MaPhong != lesson.MaPhong && p.SucChua >= sourceRoom.SucChua)
                    .OrderBy(p => p.Cum == sourceRoom.Cum ? 1 : 0)
                    .ThenBy(p => p.MaPhong);

                foreach (var candidate in candidates)
                {
                    var occupied = demoLessons.Any(x =>
                        x.Id != lesson.Id &&
                        x.Ngay == lesson.Ngay &&
                        x.MaPhong == candidate.MaPhong &&
                        x.BatDau < lesson.KetThuc &&
                        x.KetThuc > lesson.BatDau &&
                        x.TrangThai != "Đã hủy");

                    if (!occupied) return candidate.MaPhong;
                }
                return null;
            }

            // Hai yêu cầu có kế hoạch: không đổi phòng lịch gốc, chờ QL duyệt.
            foreach (var lesson in demoLessons
                .GroupBy(x => x.Ngay)
                .Select(g => g.First())
                .Take(2))
            {
                var targetRoom = FindAvailableRoom(lesson);
                if (targetRoom == null) continue;

                _context.YeuCaus.Add(new YeuCauDoiPhong
                {
                    LichDayId = lesson.Id,
                    GiangVienId = lesson.GiangVienId,
                    PhongCu = lesson.MaPhong,
                    PhongMoi = targetRoom,
                    LyDo = "Phòng hiện tại cần bảo trì thiết bị trước buổi học; đề nghị chuyển sang phòng tương đương để bảo đảm tiết thực hành.",
                    Loai = LoaiYeuCau.CoKeHoach,
                    TrangThai = TrangThaiYeuCau.ChoDuyet,
                    ThoiGianTao = realNow
                });
                usedLessonIds.Add(lesson.Id);
                seededRoomRequests++;
            }

            // Hai yêu cầu khẩn cấp: chọn ca sáng, cập nhật phòng như nghiệp vụ thật
            // và tạo thông báo để QL hậu kiểm sau 08:00.
            foreach (var lesson in demoLessons
                .Where(x => x.BatDau < new TimeOnly(8, 0) && !usedLessonIds.Contains(x.Id))
                .Take(2))
            {
                var targetRoom = FindAvailableRoom(lesson);
                if (targetRoom == null) continue;

                var oldRoom = lesson.MaPhong;
                lesson.MaPhong = targetRoom;
                var createdAt = lesson.Ngay.ToDateTime(new TimeOnly(7, 5));
                var request = new YeuCauDoiPhong
                {
                    LichDayId = lesson.Id,
                    GiangVienId = lesson.GiangVienId,
                    PhongCu = oldRoom,
                    PhongMoi = targetRoom,
                    LyDo = "Thiết bị chính tại phòng hiện tại phát sinh sự cố trong giờ chuẩn bị; giảng viên chuyển sang phòng tương thích để không gián đoạn buổi học.",
                    Loai = LoaiYeuCau.KhanCap,
                    TrangThai = TrangThaiYeuCau.TuDongDuyet,
                    ThoiGianTao = createdAt
                };
                _context.YeuCaus.Add(request);
                await _context.SaveChangesAsync();

                _context.ThongBaos.Add(new ThongBao
                {
                    VaiTroNhan = Roles.QL,
                    NguoiNhan = "Quản lý LAB, Phòng Đào tạo",
                    NoiDung = $"DEMO hậu kiểm: lịch {lesson.Id}, {oldRoom} → {targetRoom}. Lý do: thiết bị chính phát sinh sự cố; cần xác nhận việc chuyển phòng.",
                    ThoiGian = createdAt,
                    TrangThai = TrangThaiThongBao.ChoHauKiem,
                    YeuCauId = request.Id
                });

                usedLessonIds.Add(lesson.Id);
                seededRoomRequests++;
                seededAuditNotifications++;
            }

            await _context.SaveChangesAsync();

            // Reset tat ca thiet bi ve xanh va xoa Ticket demo cu, sau do nap lai bo loi mau co dinh.
            var oldTickets = await _context.Tickets.ToListAsync();
            _context.Tickets.RemoveRange(oldTickets);
            foreach (var device in devices) device.TrangThai = TrangThaiThietBi.SanSang;
            await _context.SaveChangesAsync();

            var ktvUserId = await _context.NguoiDungs
                .Where(x => x.VaiTro == Roles.KTV && x.HoatDong)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            // Chon thiet bi CO THAT trong PostgreSQL theo loai va phong; khong tao ma thiet bi gia.
            var seed = new List<(ThietBi Device, string LoaiLoi, string GhiChu, string TrangThaiThietBi, string TrangThaiTicket)>();
            void AddSeed(string loai, int count, string loaiLoi, string ghiChu, bool firstRed)
            {
                var selected = devices
                    .Where(d => string.Equals(d.Loai, loai, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(d => d.MaPhong).ThenBy(d => d.MaThietBi)
                    .Take(count).ToList();
                for (var i = 0; i < selected.Count; i++)
                {
                    var red = (i % 2 == 0) == firstRed;
                    seed.Add((selected[i], loaiLoi, ghiChu,
                        red ? TrangThaiThietBi.Hong : TrangThaiThietBi.DangBaoTri,
                        red ? TrangThaiTicket.ChoXuLy : TrangThaiTicket.DangSuaChua));
                }
            }

            AddSeed("PC", 2, "Máy tính không hoạt động ổn định", "Máy tính không khởi động bình thường hoặc thường xuyên treo khi mở ứng dụng thực hành. Cần kiểm tra nguồn, RAM và ổ lưu trữ.", true);
            AddSeed("TV", 1, "Thiết bị hiển thị không nhận tín hiệu", "Màn hình không nhận tín hiệu từ máy tính giảng viên qua cổng HDMI. Cần kiểm tra dây kết nối và ngõ vào.", true);
            AddSeed("WIFI", 1, "Kết nối mạng không ổn định", "Kết nối Wi-Fi chập chờn, thiết bị trong phòng thường xuyên mất mạng. Cần kiểm tra nguồn cấp và đường uplink.", false);
            AddSeed("TUSAC", 1, "Thiết bị sạc không cấp nguồn", "Thiết bị sạc không cấp điện ổn định cho thiết bị đầu cuối. Cần kiểm tra adapter, dây nguồn và đầu ra.", true);
            AddSeed("KIT", 2, "Bộ KIT thực hành hoạt động bất thường", "Bộ KIT không nhận tín hiệu ổn định từ bộ điều khiển hoặc cảm biến. Cần kiểm tra cáp, nguồn và module kết nối.", false);

            // Neu CSDL thuc te khong co du loai thiet bi, bo sung tu cac dong con lai.
            foreach (var device in devices.Where(d => seed.All(x => x.Device.MaThietBi != d.MaThietBi)).OrderBy(d => d.MaPhong).ThenBy(d => d.MaThietBi))
            {
                if (seed.Count >= 6) break;
                var red = seed.Count % 2 == 0;
                seed.Add((device, "Thiết bị cần kiểm tra", $"Thiết bị {device.Ten} tại phòng {device.MaPhong} được đưa vào danh sách kiểm thử để KTV kiểm tra tình trạng hoạt động thực tế.",
                    red ? TrangThaiThietBi.Hong : TrangThaiThietBi.DangBaoTri,
                    red ? TrangThaiTicket.ChoXuLy : TrangThaiTicket.DangSuaChua));
            }

            var seeded = 0;
            foreach (var item in seed)
            {
                var device = item.Device;
                device.TrangThai = item.TrangThaiThietBi;
                _context.Tickets.Add(new TicketSuCo
                {
                    MaThietBi = device.MaThietBi,
                    LoaiLoi = item.LoaiLoi,
                    GhiChu = item.GhiChu,
                    NguoiBaoId = userId,
                    ThoiGianTao = realNow,
                    TrangThai = item.TrangThaiTicket,
                    KtvXuLyId = item.TrangThaiTicket == TrangThaiTicket.DangSuaChua ? (ktvUserId ?? userId) : null,
                    ThoiGianCapNhat = item.TrangThaiTicket == TrangThaiTicket.DangSuaChua ? realNow : null
                });
                seeded++;
            }

            if (seeded == 0)
            {
                await tx.RollbackAsync();
                return Conflict(new { success = false, message = "Danh muc thiet bi chua co du lieu de tao Ticket demo." });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new
            {
                success = true,
                message = $"Da reset demo: khoi phuc {devices.Count} thiet bi va tao {seeded} Ticket tu danh muc thiet bi thuc te.",
                ticketCount = seeded,
                futureAttendanceReset = logsToReset.Count,
                roomChangeRequestsSeeded = seededRoomRequests,
                auditNotificationsSeeded = seededAuditNotifications,
                oldRoomNotificationsDeleted = oldRoomNotifications.Count,
                totalTemporaryNotesDeleted = allTemporaryNotes.Count,
                currentVietnamDate = realNow.ToString("yyyy-MM-dd"),
                currentVietnamTime = realNow.ToString("HH:mm"),
                currentVietnamDateTime = realNowOffset.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                redCount = seed.Count(x => x.TrangThaiThietBi == TrangThaiThietBi.Hong),
                yellowCount = seed.Count(x => x.TrangThaiThietBi == TrangThaiThietBi.DangBaoTri)
            });
        });
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            bool connected =
                await _context.Database.CanConnectAsync();

            if (!connected)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Không kết nối được database DntuLab."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Kết nối PostgreSQL thành công!",
                database = _context.Database
                    .GetDbConnection().Database
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message
            });
        }
    }
}
