using System.Security.Claims;
using Aukaria.Application.Interfaces;
using Aukaria.Domain.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection;

namespace Aukaria.Api.Auth;

public static class AukariaAuthExtensions
{
    public const string SessionScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string ClaimEmpresaId = "EmpresaId";

    public static IServiceCollection AddAukariaAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var googleClientId = configuration["Authentication:Google:ClientId"] ?? string.Empty;
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        var microsoftClientId = configuration["Authentication:Microsoft:ClientId"] ?? string.Empty;
        var microsoftClientSecret = configuration["Authentication:Microsoft:ClientSecret"] ?? string.Empty;

        var authenticationBuilder = services.AddAuthentication(SessionScheme)
            .AddCookie(SessionScheme, cookie =>
            {
                cookie.Cookie.Name = "Aukaria.Auth";
                cookie.Cookie.SameSite = SameSiteMode.None;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookie.Cookie.HttpOnly = true;
                cookie.SlidingExpiration = true;
                cookie.ExpireTimeSpan = TimeSpan.FromDays(7);
                cookie.LoginPath = "/api/auth/login";
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

        context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()));
        context.Identity?.AddClaim(new Claim(ClaimTypes.Name, usuario.Nombre));
        context.Identity?.AddClaim(new Claim(ClaimTypes.Email, usuario.Email));
        context.Identity?.AddClaim(new Claim(ClaimEmpresaId, usuario.EmpresaId.ToString()));

        string frontend = configuration["Authentication:RedirectToFrontend"] ?? "/";
        context.Properties.RedirectUri = frontend;
    }

    public static Guid ObtenerEmpresaIdPorDefecto(IConfiguration configuration)
    {
        string valor = configuration["Authentication:EmpresaIdPorDefecto"] ?? "11111111-1111-1111-1111-111111111111";
        return Guid.TryParse(valor, out Guid id) ? id : Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    public static ClaimsPrincipal ConstruirPrincipal(IniciarSesionUsuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimEmpresaId, usuario.EmpresaId.ToString())
        };

        var identidad = new ClaimsIdentity(claims, SessionScheme);
        return new ClaimsPrincipal(identidad);
    }
}

public sealed record IniciarSesionUsuario(Guid Id, string Nombre, string Email, Guid EmpresaId);
