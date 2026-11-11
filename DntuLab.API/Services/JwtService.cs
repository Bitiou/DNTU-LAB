
using DntuLab.Api.Domain;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DntuLab.Api.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string TaoToken(NguoiDung nguoiDung)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Chưa cấu hình Jwt:Key.");

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier,
                nguoiDung.Id.ToString()),
            new Claim(ClaimTypes.Name,
                nguoiDung.TenDangNhap),
            new Claim(ClaimTypes.Role,
                nguoiDung.VaiTro)
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            signingKey, SecurityAlgorithms.HmacSha256);

        var expiresMinutes = _configuration.GetValue<int>(
            "Jwt:ExpiresMinutes", 60);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
