using System.Security.Claims;
using Aukaria.Application.Interfaces;
using Aukaria.Domain.Entities;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace Aukaria.Api.Auth;

public static class AukariaAuthExtensions
{
    public const string SessionScheme = "Aukaria.Jwt";
    public const string ClaimEmpresaId = "EmpresaId";

    public static IServiceCollection AddAukariaAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<JwtTokenService>();

        var googleClientId = configuration["Authentication:Google:ClientId"] ?? string.Empty;
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        var microsoftClientId = configuration["Authentication:Microsoft:ClientId"] ?? string.Empty;
        var microsoftClientSecret = configuration["Authentication:Microsoft:ClientSecret"] ?? string.Empty;

        var authenticationBuilder = services.AddAuthentication(SessionScheme)
            .AddJwtBearer(SessionScheme, jwt =>
            {
                jwt.RequireHttpsMetadata = false;
                jwt.SaveToken = true;
                jwt.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Authentication:Jwt:Issuer"] ?? "Aukaria.Api",
                    ValidateAudience = true,
                    ValidAudience = configuration["Authentication:Jwt:Audience"] ?? "Aukaria.Web",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(
                            configuration["Authentication:Jwt:Secret"]
                            ?? "DevOnlySecret_NoUsarEnProduccion_1234567890")),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            authenticationBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, google =>
            {
                google.ClientId = googleClientId;
                google.ClientSecret = googleClientSecret;
                google.CallbackPath = "/api/auth/signin-google";
                google.Events.OnCreatingTicket = ctx => CrearSesionDesdeProviderAsync(ctx, "google", configuration);
            });
        }

        if (!string.IsNullOrWhiteSpace(microsoftClientId) && !string.IsNullOrWhiteSpace(microsoftClientSecret))
        {
            authenticationBuilder.AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, microsoft =>
            {
                microsoft.ClientId = microsoftClientId;
                microsoft.ClientSecret = microsoftClientSecret;
                microsoft.CallbackPath = "/api/auth/signin-microsoft";
                microsoft.Events.OnCreatingTicket = ctx => CrearSesionDesdeProviderAsync(ctx, "microsoft", configuration);
            });
        }

        return services;
    }

    private static async Task CrearSesionDesdeProviderAsync(
        OAuthCreatingTicketContext context,
        string provider,
        IConfiguration configuration)
    {
        string sub = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        string email = context.Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        string nombre = context.Principal?.FindFirstValue(ClaimTypes.Name)
                        ?? context.Principal?.FindFirstValue("name")
                        ?? email.Split('@')[0];

        if (string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(email))
        {
            context.Fail("No se pudo obtener la identidad del proveedor.");
            return;
        }

        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
        Guid empresaId = ObtenerEmpresaIdPorDefecto(configuration);
        Usuario? usuario = await authService.ObtenerOCrearPorProviderAsync(
            provider, email.ToLowerInvariant(), nombre, sub, empresaId, context.HttpContext.RequestAborted);

        if (usuario is null)
        {
            context.Fail("No se pudo crear el usuario.");
            return;
        }

        var jwt = context.HttpContext.RequestServices.GetRequiredService<JwtTokenService>();
        string token = jwt.Generar(
            new IniciarSesionUsuario(usuario.Id, usuario.Nombre, usuario.Email, usuario.EmpresaId));

        string frontend = configuration["Authentication:RedirectToFrontend"] ?? "/";
        string separador = frontend.Contains('?') ? "&" : "?";
        context.Properties.RedirectUri = $"{frontend}{separador}token={Uri.EscapeDataString(token)}";
    }

    public static Guid ObtenerEmpresaIdPorDefecto(IConfiguration configuration)
    {
        string valor = configuration["Authentication:EmpresaIdPorDefecto"] ?? "11111111-1111-1111-1111-111111111111";
        return Guid.TryParse(valor, out Guid id) ? id : Guid.Parse("11111111-1111-1111-1111-111111111111");
    }
}

public sealed record IniciarSesionUsuario(Guid Id, string Nombre, string Email, Guid EmpresaId);
