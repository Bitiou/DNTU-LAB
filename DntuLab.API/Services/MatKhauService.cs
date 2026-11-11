
using DntuLab.Api.Domain;
using Microsoft.AspNetCore.Identity;

namespace DntuLab.Api.Services;

public class MatKhauService
{
    private readonly PasswordHasher<NguoiDung> _hasher = new();

    public string TaoHash(NguoiDung nguoiDung, string matKhau)
    {
        return _hasher.HashPassword(nguoiDung, matKhau);
    }

    public bool KiemTra(
        NguoiDung nguoiDung,
        string matKhau)
    {
        var result = _hasher.VerifyHashedPassword(
            nguoiDung,
            nguoiDung.MatKhauHash,
            matKhau);

        return result != PasswordVerificationResult.Failed;
    }
}
