using System.Text;
using Aukaria.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Aukaria.Api.Controllers;

[ApiController]
[Route("api/test")]
public sealed class TestController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<TestController> _logger;

    public TestController(IEmailService emailService, ILogger<TestController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint temporal para validar el envío directo de correos SMTP desde Swagger o Postman.
    /// </summary>
    [HttpPost("email")]
    public async Task<IActionResult> EnviarCorreoPrueba(
        [FromQuery] string destinatario,
        [FromQuery] string? asunto = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinatario) || !destinatario.Contains('@'))
        {
            return BadRequest("El parámetro 'destinatario' debe ser un correo válido (p. ej. POST /api/test/email?destinatario=algo@dominio.com).");
        }

        string cuerpo = new StringBuilder()
            .AppendLine("Este es un correo de prueba desde Aukaria.")
            .AppendLine()
            .AppendLine("Si recibes este mensaje, la configuración SMTP funciona correctamente.")
            .AppendLine($"Fecha de envío: {DateTime.UtcNow:O}")
            .ToString();

        try
        {
            bool enviado = await _emailService.EnviarOtpAsync(destinatario, "123456", cancellationToken);
            if (enviado)
            {
                return Ok(new { mensaje = $"Correo de prueba enviado a {destinatario}." });
            }
            return StatusCode(500, new { error = "No se pudo enviar el correo. Revisa los logs (probablemente faltan credenciales SMTP o el envío falló)." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TEST EMAIL] Error al enviar correo de prueba a {Destinatario}", destinatario);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
