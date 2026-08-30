using Aukaria.Application.DTOs.Requests;
using Aukaria.Application.DTOs.Responses;
using Aukaria.Application.Exceptions;
using Aukaria.Application.Interfaces;
using Aukaria.Domain.Entities;
using Aukaria.Domain.Enums;

namespace Aukaria.Application.Services;

public sealed class AuthService : IAuthService
{
    private static readonly TimeSpan ExpiracionOtp = TimeSpan.FromMinutes(5);

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IOtpStore _otpStore;
    private readonly IEmailService _emailService;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IOtpStore otpStore,
        IEmailService emailService)
    {
        _usuarioRepository = usuarioRepository;
        _otpStore = otpStore;
        _emailService = emailService;
    }

    public async Task SolicitarOtpAsync(SolicitarOtpRequestDto request, CancellationToken cancellationToken = default)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        string codigo = GenerarCodigoOtp();

        _otpStore.Guardar(email, codigo, ExpiracionOtp);
        await _emailService.EnviarOtpAsync(email, codigo, cancellationToken);
    }

    public async Task<UsuarioSesionDto> VerificarOtpAsync(VerificarOtpRequestDto request, CancellationToken cancellationToken = default)
    {
        string email = request.Email.Trim().ToLowerInvariant();

        if (!_otpStore.Validar(email, request.CodigoOtp.Trim()))
        {
            throw new OtpInvalidoException("El código de verificación es incorrecto o ha expirado.");
        }

        Usuario? usuario = await _usuarioRepository.ObtenerPorEmailAsync(email, cancellationToken);

        if (usuario is null)
        {
            if (!request.EsRegistro || request.EmpresaId == Guid.Empty || string.IsNullOrWhiteSpace(request.Nombre))
            {
                throw new OtpInvalidoException("No existe un usuario con este correo. Regístrate primero.");
            }

            usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                EmpresaId = request.EmpresaId,
                Nombre = request.Nombre.Trim(),
                Email = email,
                Provider = "local",
                EmailConfirmado = true,
                Rol = RolUsuario.AnalistaPredial,
                Activo = true,
                UltimoAcceso = DateTime.UtcNow
            };

            await _usuarioRepository.CrearAsync(usuario, cancellationToken);
        }
        else
        {
            usuario.UltimoAcceso = DateTime.UtcNow;
            await _usuarioRepository.ActualizarAsync(usuario, cancellationToken);
        }

        return new UsuarioSesionDto
        {
            Id = usuario.Id,
            EmpresaId = usuario.EmpresaId,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Provider = "local",
            Rol = usuario.Rol,
            EmailConfirmado = true
        };
    }

    public async Task<Usuario?> ObtenerUsuarioSesionAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await _usuarioRepository.ObtenerPorIdAsync(usuarioId, cancellationToken);
    }

    public async Task<Usuario?> ObtenerOCrearPorProviderAsync(
        string provider,
        string email,
        string nombre,
        string subjectId,
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        Usuario? usuario = await _usuarioRepository.ObtenerPorProviderAsync(provider, subjectId, cancellationToken);

        if (usuario is not null)
        {
            usuario.UltimoAcceso = DateTime.UtcNow;
            usuario.Email = email;
            usuario.Nombre = nombre;
            await _usuarioRepository.ActualizarAsync(usuario, cancellationToken);
            return usuario;
        }

        Usuario? porEmail = await _usuarioRepository.ObtenerPorEmailAsync(email, cancellationToken);
        if (porEmail is not null)
        {
            porEmail.UltimoAcceso = DateTime.UtcNow;
            await _usuarioRepository.ActualizarAsync(porEmail, cancellationToken);
            return porEmail;
        }

        var nuevo = new Usuario
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nombre = nombre,
            Email = email,
            Provider = provider,
            SubjectId = subjectId,
            EmailConfirmado = true,
            Rol = RolUsuario.AnalistaPredial,
            Activo = true,
            UltimoAcceso = DateTime.UtcNow
        };

        await _usuarioRepository.CrearAsync(nuevo, cancellationToken);
        return nuevo;
    }

    private static string GenerarCodigoOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }
}
