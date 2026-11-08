
using System.Security.Claims;
using DntuLab.Api.Data;
using DntuLab.Api.Domain;
using DntuLab.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "KTV,QL,GV")]
public class TicketController : ControllerBase
{
    private readonly LabDbContext _context;

    public TicketController(LabDbContext context)
    {
        _context = context;
    }

    // Giang vien chi xem Ticket thuoc phong dang thao tac; KTV/QL co the xem tuong tu.
    [HttpGet("phong/{maPhong}")]
    [Authorize(Roles = "GV,KTV,QL")]
    public async Task<IActionResult> GetByRoom(string maPhong)
    {
        if (string.IsNullOrWhiteSpace(maPhong))
            return BadRequest(new { success = false, message = "Mã phòng không hợp lệ." });

        var room = maPhong.Trim().ToUpperInvariant();

        var tickets = await _context.Tickets
            .AsNoTracking()
            .Where(x => x.ThietBi.MaPhong == room)
            .OrderByDescending(x => x.ThoiGianTao)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.MaTicket,
                x.MaThietBi,
                TenThietBi = x.ThietBi.Ten,
                MaPhong = x.ThietBi.MaPhong,
                TenNguoiBao = x.NguoiBao.HoTen,
                x.LoaiLoi,
                x.GhiChu,
                x.NguoiBaoId,
                x.ThoiGianTao,
                x.TrangThai,
                x.KtvXuLyId,
                x.ThoiGianCapNhat,
                x.ThoiGianHoanTat
            })
            .ToListAsync();

        return Ok(new { success = true, count = tickets.Count, data = tickets });
    }

    [HttpGet]
    [Authorize(Roles = "KTV,QL")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? trangThai = null)
    {
        // Đồng bộ ngược các trạng thái thiết bị đã tồn tại trước khi có cơ chế tự tạo Ticket.
        // Thiết bị Hỏng -> Ticket Chờ xử lý; Đang bảo trì -> Ticket Đang sửa chữa.
        // Chỉ tạo mới khi chưa có Ticket đang mở, tránh trùng lặp.
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var nguoiBaoId)) return Unauthorized();

        // Thiet bi do do GhiNhanTam (cum F) chua tong hop chua duoc tao Ticket chinh thuc.
        // Loai cac thiet bi nay khoi co che tu dong dong bo de giu dung quy trinh GV tong hop cuoi ca.
        var maThietBiDangChoTongHop = await _context.GhiNhanTams
            .Where(g => !g.TicketId.HasValue && g.TrangThai != "Đã tạo Ticket")
            .Select(g => g.MaThietBi)
            .Distinct()
            .ToListAsync();

        var devicesCanSync = await _context.ThietBis
            .Where(d =>
                (d.TrangThai == TrangThaiThietBi.Hong || d.TrangThai == TrangThaiThietBi.DangBaoTri) &&
                !maThietBiDangChoTongHop.Contains(d.MaThietBi))
            .ToListAsync();

        // Thiết bị xanh/Sẵn sàng không nằm trong Kanban đang xử lý.
        // Nếu còn Ticket mở do dữ liệu cũ, đóng Ticket để trạng thái nhất quán.
        var readyDeviceCodes = await _context.ThietBis
            .Where(d => d.TrangThai == TrangThaiThietBi.SanSang)
            .Select(d => d.MaThietBi)
            .ToListAsync();
        var staleOpenTickets = await _context.Tickets
            .Where(t => readyDeviceCodes.Contains(t.MaThietBi) && t.TrangThai != TrangThaiTicket.DaKhacPhuc)
            .ToListAsync();
        foreach (var staleTicket in staleOpenTickets)
        {
            staleTicket.TrangThai = TrangThaiTicket.DaKhacPhuc;
            staleTicket.ThoiGianHoanTat ??= DateTime.Now;
            staleTicket.ThoiGianCapNhat = DateTime.Now;
        }

        foreach (var device in devicesCanSync)
        {
            var targetStatus = device.TrangThai == TrangThaiThietBi.Hong
                ? TrangThaiTicket.ChoXuLy
                : TrangThaiTicket.DangSuaChua;

            var openTicket = await _context.Tickets
                .Where(t => t.MaThietBi == device.MaThietBi && t.TrangThai != TrangThaiTicket.DaKhacPhuc)
                .OrderByDescending(t => t.ThoiGianTao)
                .FirstOrDefaultAsync();

            if (openTicket == null)
            {
                _context.Tickets.Add(new TicketSuCo
                {
                    MaThietBi = device.MaThietBi,
                    LoaiLoi = device.TrangThai == TrangThaiThietBi.Hong ? "Thiết bị hỏng" : "Bảo trì thiết bị",
                    GhiChu = "Ticket được hệ thống đồng bộ từ trạng thái thiết bị đã có.",
                    NguoiBaoId = nguoiBaoId,
                    ThoiGianTao = DateTime.Now,
                    TrangThai = targetStatus,
                    KtvXuLyId = targetStatus == TrangThaiTicket.DangSuaChua ? nguoiBaoId : null,
                    ThoiGianCapNhat = targetStatus == TrangThaiTicket.DangSuaChua ? DateTime.Now : null
                });
            }
            else if (openTicket.TrangThai != TrangThaiTicket.BaoCaoCapTren)
            {
                openTicket.TrangThai = targetStatus;
                if (targetStatus == TrangThaiTicket.DangSuaChua)
                {
                    openTicket.KtvXuLyId ??= nguoiBaoId;
                    openTicket.ThoiGianCapNhat ??= DateTime.Now;
                }
                else
                {
                    openTicket.KtvXuLyId = null;
                }
            }
        }

        if (devicesCanSync.Count > 0 || staleOpenTickets.Count > 0)
            await _context.SaveChangesAsync();

        var query = _context.Tickets
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai))
        {
            query = query.Where(x => x.TrangThai == trangThai);
        }

        var tickets = await query
            .OrderByDescending(x => x.ThoiGianTao)
            .Select(x => new
            {
                x.Id,
                x.MaTicket,
                x.MaThietBi,
                TenThietBi = x.ThietBi.Ten,
                MaPhong = x.ThietBi.MaPhong,
                TenNguoiBao = x.NguoiBao.HoTen,
                x.LoaiLoi,
                x.GhiChu,
                x.NguoiBaoId,
                x.ThoiGianTao,
                x.TrangThai,
                x.KtvXuLyId,
                x.ThoiGianCapNhat,
                x.ThoiGianHoanTat
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            count = tickets.Count,
            data = tickets
        });
    }

    [HttpPost]
    [Authorize(Roles = "KTV,QL,GV")]
    public async Task<IActionResult> Tao(
        [FromBody] TaoTicketRequest request)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var nguoiBaoId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Không xác định được tài khoản đăng nhập."
            });
        }

        var thietBi = await _context.ThietBis
            .FirstOrDefaultAsync(x => x.MaThietBi == request.MaThietBi);

        if (thietBi == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy thiết bị."
            });
        }

        // KTV khong duoc tu tao su co tren thiet bi xanh; chi xu ly thiet bi da co co loi do/vang.
        if (User.IsInRole(Roles.KTV) && thietBi.TrangThai == TrangThaiThietBi.SanSang)
            return Conflict(new { success = false, message = "KTV chỉ được thao tác trên thiết bị màu đỏ hoặc vàng. Thiết bị màu xanh không thể tạo Ticket từ tài khoản KTV." });

        var ticketDangMo = await _context.Tickets.AnyAsync(x =>
            x.MaThietBi == request.MaThietBi && x.TrangThai != TrangThaiTicket.DaKhacPhuc);
        if (ticketDangMo)
            return Conflict(new { success = false, message = "Thiết bị đã có Ticket chưa hoàn tất. Không tạo Ticket trùng." });

        var ticket = new TicketSuCo
        {
            MaThietBi = request.MaThietBi,
            LoaiLoi = request.LoaiLoi,
            GhiChu = request.GhiChu,
            NguoiBaoId = nguoiBaoId,
            ThoiGianTao = DateTime.Now,
            TrangThai = TrangThaiTicket.ChoXuLy
        };

        _context.Tickets.Add(ticket);
        thietBi.TrangThai = TrangThaiThietBi.Hong;
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetAll),
            new { },
            new
            {
                success = true,
                message = "Tạo Ticket thành công.",
                id = ticket.Id,
                maThietBi = ticket.MaThietBi,
                trangThai = ticket.TrangThai
            });
    }

    [HttpPut("{id:int}/trang-thai")]
    [Authorize(Roles = "KTV,QL")]
    public async Task<IActionResult> CapNhatTrangThai(
        int id,
        [FromBody] CapNhatTicketRequest request)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ticket == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy Ticket."
            });
        }

        var trangThaiHopLe = new[]
        {
        TrangThaiTicket.ChoXuLy,
        TrangThaiTicket.DangSuaChua,
        TrangThaiTicket.BaoCaoCapTren,
        TrangThaiTicket.DaKhacPhuc
    };

        if (!trangThaiHopLe.Contains(request.TrangThai))
        {
            return BadRequest(new
            {
                success = false,
                message = "Trạng thái Ticket không hợp lệ."
            });
        }

        var trangThaiCu = ticket.TrangThai;

        // Không cho mở lại Ticket đã khắc phục.
        if (trangThaiCu == TrangThaiTicket.DaKhacPhuc)
        {
            return BadRequest(new
            {
                success = false,
                message = "Ticket đã khắc phục, không thể cập nhật lại."
            });
        }

        // Kiểm tra chuyển trạng thái theo quy trình.
        var chuyenTrangThaiHopLe =
            trangThaiCu == TrangThaiTicket.ChoXuLy &&
            (request.TrangThai == TrangThaiTicket.DangSuaChua ||
             request.TrangThai == TrangThaiTicket.BaoCaoCapTren)
            ||
            trangThaiCu == TrangThaiTicket.DangSuaChua &&
            (request.TrangThai == TrangThaiTicket.DaKhacPhuc ||
             request.TrangThai == TrangThaiTicket.BaoCaoCapTren)
            ||
            trangThaiCu == TrangThaiTicket.BaoCaoCapTren &&
            request.TrangThai == TrangThaiTicket.DangSuaChua;

        if (!chuyenTrangThaiHopLe)
        {
            return BadRequest(new
            {
                success = false,
                message = "Không thể chuyển từ trạng thái hiện tại sang trạng thái yêu cầu."
            });
        }

        var userIdValue = User.FindFirstValue(
            System.Security.Claims.ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var ktvId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Không xác định được tài khoản đăng nhập."
            });
        }

        ticket.TrangThai = request.TrangThai;
        ticket.KtvXuLyId = ktvId;
        ticket.ThoiGianCapNhat = DateTime.Now;
        var thietBiCapNhat = await _context.ThietBis.FirstOrDefaultAsync(x => x.MaThietBi == ticket.MaThietBi);
        if (thietBiCapNhat != null)
        {
            thietBiCapNhat.TrangThai = request.TrangThai == TrangThaiTicket.DaKhacPhuc
                ? TrangThaiThietBi.SanSang
                : request.TrangThai == TrangThaiTicket.ChoXuLy
                    ? TrangThaiThietBi.Hong : TrangThaiThietBi.DangBaoTri;
        }
        if (request.TrangThai == TrangThaiTicket.DaKhacPhuc)
            ticket.ThoiGianHoanTat = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Cập nhật Ticket thành công.",
            id = ticket.Id,
            trangThaiCu,
            trangThaiMoi = ticket.TrangThai,
            ticket.KtvXuLyId,
            ticket.ThoiGianCapNhat,
            ticket.ThoiGianHoanTat
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "KTV,QL")]
    public async Task<IActionResult> GetById(int id)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.MaTicket,

                x.MaThietBi,
                TenThietBi = x.ThietBi.Ten,
                LoaiThietBi = x.ThietBi.Loai,
                MaPhong = x.ThietBi.MaPhong,
                TrangThaiThietBi = x.ThietBi.TrangThai,

                x.LoaiLoi,
                x.GhiChu,

                x.NguoiBaoId,
                TenNguoiBao = x.NguoiBao.HoTen,

                x.ThoiGianTao,
                x.TrangThai,

                x.KtvXuLyId,
                TenKtvXuLy = x.KtvXuLy != null
                    ? x.KtvXuLy.HoTen
                    : null,

                x.ThoiGianCapNhat,
                x.ThoiGianHoanTat
            })
            .FirstOrDefaultAsync();

        if (ticket == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy Ticket."
            });
        }

        return Ok(new
        {
            success = true,
            data = ticket
        });
    }


    [HttpGet("loc")]
    [Authorize(Roles = "KTV,QL")]
    public async Task<IActionResult> Loc(
        [FromQuery] LocTicketRequest request)
    {
        if (request.Trang < 1)
        {
            return BadRequest(new
            {
                success = false,
                message = "Trang phải lớn hơn hoặc bằng 1."
            });
        }

        if (request.KichThuocTrang < 1 ||
            request.KichThuocTrang > 100)
        {
            return BadRequest(new
            {
                success = false,
                message = "Kích thước trang phải từ 1 đến 100."
            });
        }

        if (request.TuNgay.HasValue &&
            request.DenNgay.HasValue &&
            request.TuNgay.Value > request.DenNgay.Value)
        {
            return BadRequest(new
            {
                success = false,
                message = "Từ ngày không được lớn hơn đến ngày."
            });
        }

        var query = _context.Tickets
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            query = query.Where(x =>
                x.TrangThai == request.TrangThai);
        }

        if (!string.IsNullOrWhiteSpace(request.MaPhong))
        {
            query = query.Where(x =>
                x.ThietBi.MaPhong == request.MaPhong);
        }

        if (request.TuNgay.HasValue)
        {
            query = query.Where(x =>
                x.ThoiGianTao >= request.TuNgay.Value);
        }

        if (request.DenNgay.HasValue)
        {
            query = query.Where(x =>
                x.ThoiGianTao <= request.DenNgay.Value);
        }

        var tongSoTicket = await query.CountAsync();

        var tongSoTrang = (int)Math.Ceiling(
            tongSoTicket / (double)request.KichThuocTrang);

        var tickets = await query
            .OrderByDescending(x => x.ThoiGianTao)
            .ThenByDescending(x => x.Id)
            .Skip((request.Trang - 1) *
                  request.KichThuocTrang)
            .Take(request.KichThuocTrang)
            .Select(x => new
            {
                x.Id,
                x.MaTicket,
                x.MaThietBi,
                TenThietBi = x.ThietBi.Ten,
                MaPhong = x.ThietBi.MaPhong,
                x.LoaiLoi,
                x.ThoiGianTao,
                x.TrangThai,
                x.NguoiBaoId,
                TenNguoiBao = x.NguoiBao.HoTen,
                x.KtvXuLyId,
                TenKtvXuLy = x.KtvXuLy != null
                    ? x.KtvXuLy.HoTen
                    : null
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            pagination = new
            {
                trangHienTai = request.Trang,
                kichThuocTrang = request.KichThuocTrang,
                tongSoTicket,
                tongSoTrang
            },
            filters = request,
            data = tickets
        });
    }

}
