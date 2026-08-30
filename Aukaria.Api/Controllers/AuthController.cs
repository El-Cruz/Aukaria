using System.Security.Claims;
using Aukaria.Api.Auth;
using Aukaria.Application.DTOs.Requests;
using Aukaria.Application.DTOs.Responses;
using Aukaria.Application.Exceptions;
using Aukaria.Application.Interfaces;
using Aukaria.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aukaria.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUsuarioRepository _usuarioRepository;

    public AuthController(IAuthService authService, IUsuarioRepository usuarioRepository)
    {
        _authService = authService;
        _usuarioRepository = usuarioRepository;
    }

    [HttpGet("login-google")]
    public IActionResult LoginGoogle([FromQuery] string? returnUrl = null)
    {
        string redirect = string.IsNullOrWhiteSpace(returnUrl)
            ? "/"
            : returnUrl;
        var props = new AuthenticationProperties { RedirectUri = redirect };
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("login-microsoft")]
    public IActionResult LoginMicrosoft([FromQuery] string? returnUrl = null)
    {
        string redirect = string.IsNullOrWhiteSpace(returnUrl)
            ? "/"
            : returnUrl;
        var props = new AuthenticationProperties { RedirectUri = redirect };
        return Challenge(props, MicrosoftAccountDefaults.AuthenticationScheme);
    }

    [HttpPost("solicitar-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SolicitarOtp([FromBody] SolicitarOtpRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return BadRequest("Ingresa un correo válido.");
        }

        await _authService.SolicitarOtpAsync(request, cancellationToken);
        return Ok(new { mensaje = "Código enviado a tu correo." });
    }

    [HttpPost("verificar-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerificarOtp([FromBody] VerificarOtpRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            UsuarioSesionDto usuario = await _authService.VerificarOtpAsync(request, cancellationToken);

            var principal = AukariaAuthExtensions.ConstruirPrincipal(
                new IniciarSesionUsuario(usuario.Id, usuario.Nombre, usuario.Email, usuario.EmpresaId));

            await HttpContext.SignInAsync(
                AukariaAuthExtensions.SessionScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true, AllowRefresh = true });

            return Ok(usuario);
        }
        catch (OtpInvalidoException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> ObtenerUsuarioActual(CancellationToken cancellationToken)
    {
        string? usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(usuarioId) || !Guid.TryParse(usuarioId, out Guid id))
        {
            return Unauthorized();
        }

        Usuario? usuario = await _usuarioRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (usuario is null || !usuario.Activo)
        {
            return Unauthorized();
        }

        var empresas = await _usuarioRepository.ObtenerEmpresasAsync(cancellationToken);
        string? empresaNombre = empresas.FirstOrDefault(e => e.Id == usuario.EmpresaId)?.Nombre;

        return Ok(new UsuarioSesionDto
        {
            Id = usuario.Id,
            EmpresaId = usuario.EmpresaId,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Provider = usuario.Provider,
            Rol = usuario.Rol,
            EmailConfirmado = usuario.EmailConfirmado,
            EmpresaNombre = empresaNombre
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AukariaAuthExtensions.SessionScheme);
        return Ok(new { mensaje = "Sesión cerrada." });
    }
}
