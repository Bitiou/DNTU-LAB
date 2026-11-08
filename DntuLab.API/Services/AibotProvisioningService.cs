using System.Data;
using DntuLab.Api.Data;
using DntuLab.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DntuLab.Api.Services;

/// <summary>
/// Đảm bảo Trung tâm AIBOT và 6 bộ KIT là dữ liệu thật trong SQL Server.
/// Không tạo dữ liệu giả ở frontend và không ghi đè trạng thái KIT đã tồn tại.
/// </summary>
public sealed class AibotProvisioningService
{
    private static readonly SemaphoreSlim ProvisionLock = new(1, 1);
    private readonly LabDbContext _db;

    public AibotProvisioningService(LabDbContext db) => _db = db;

    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        await ProvisionLock.WaitAsync(cancellationToken);
        try
        {
        // Serializable + khóa trong process tránh hai GET song song cùng tạo phòng/KIT.
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var room = await _db.Phongs
            .FirstOrDefaultAsync(x => x.MaPhong == CumPhong.AIBOT, cancellationToken);

        if (room == null)
        {
            room = new Phong
            {
                MaPhong = CumPhong.AIBOT,
                LoaiPhong = KieuPhong.LabAI,
                Cum = CumPhong.AIBOT,
                SucChua = 80
            };
            _db.Phongs.Add(room);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Chuẩn hóa metadata phòng, không đụng đến thiết bị hoặc lịch đã có.
            room.LoaiPhong = KieuPhong.LabAI;
            room.Cum = CumPhong.AIBOT;
            if (room.SucChua <= 0) room.SucChua = 80;
        }

        var kitCodes = Enumerable.Range(1, 6)
            .Select(i => $"AI-KIT{i:00}")
            .ToArray();

        // Tìm theo mã toàn hệ thống để không INSERT trùng khóa nếu KIT từng
        // được khai báo nhầm phòng hoặc nhầm loại thiết bị.
        var existingKits = await _db.ThietBis
            .Where(x => kitCodes.Contains(x.MaThietBi))
            .ToListAsync(cancellationToken);

        for (var i = 1; i <= 6; i++)
        {
            var code = $"AI-KIT{i:00}";
            var kit = existingKits.FirstOrDefault(x => x.MaThietBi == code);

            if (kit == null)
            {
                _db.ThietBis.Add(new ThietBi
                {
                    MaThietBi = code,
                    MaPhong = CumPhong.AIBOT,
                    Loai = LoaiThietBi.KIT,
                    Ten = $"Bộ Kit Robot & mô hình STEM {i:00}",
                    TrangThai = TrangThaiThietBi.SanSang
                });
            }
            else
            {
                kit.MaPhong = CumPhong.AIBOT;
                kit.Loai = LoaiThietBi.KIT;
                kit.Ten = $"Bộ Kit Robot & mô hình STEM {i:00}";
                // Giữ nguyên trạng thái thực tế đã có của KIT.
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            ProvisionLock.Release();
        }
    }
}
