using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Aukaria.Api.Auth;

public sealed class JwtTokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _expiracion;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["Authentication:Jwt:Secret"]
                  ?? "DevOnlySecret_NoUsarEnProduccion_1234567890";
        _issuer = configuration["Authentication:Jwt:Issuer"] ?? "Aukaria.Api";
        _audience = configuration["Authentication:Jwt:Audience"] ?? "Aukaria.Web";
        double dias = double.TryParse(configuration["Authentication:Jwt:DiasExpiracion"], out double d) ? d : 7;
        _expiracion = TimeSpan.FromDays(dias);
    }

    public string Generar(IniciarSesionUsuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(AukariaAuthExtensions.ClaimEmpresaId, usuario.EmpresaId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(_expiracion),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
