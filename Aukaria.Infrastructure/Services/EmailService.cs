using Aukaria.Application.Interfaces;
using Aukaria.Application.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Aukaria.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> EnviarOtpAsync(string destinatario, string codigoOtp, CancellationToken cancellationToken = default)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_options.FromName, Remitente()));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = "Aukaria — Código de verificación";

        mensaje.Body = new TextPart("plain")
        {
            Text = $"Tu código de verificación de Aukaria es: {codigoOtp}\n\nEste código expira en 5 minutos."
        };

        return await EnviarAsync(mensaje, destinatario, cancellationToken);
    }

    public async Task<bool> EnviarReporteAsync(string destinatario, string nombreArchivo, byte[] adjunto, string fmi, CancellationToken cancellationToken = default)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_options.FromName, Remitente()));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = $"Informe Jurídico Predial — FMI {fmi}";

        mensaje.Body = new MimePart("application", "vnd.openxmlformats-officedocument.wordprocessingml.document")
        {
            Content = new MimeContent(new MemoryStream(adjunto), ContentEncoding.Default),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
            FileName = nombreArchivo
        };

        return await EnviarAsync(mensaje, destinatario, cancellationToken);
    }

    private async Task<bool> EnviarAsync(MimeMessage mensaje, string destinatario, CancellationToken cancellationToken)
    {
        string host = _options.HostEfectivo;
        string usuario = _options.UsuarioEfectivo;
        string clave = _options.ClaveEfectiva;
        int puerto = _options.Port > 0 ? _options.Port : _options.SmtpPort;

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning("[EMAIL] Credenciales SMTP no configuradas. Omitiendo envío a {Destinatario} (falta SMTP_HOST / Email:Host).", destinatario);
            return false;
        }
        if (string.IsNullOrWhiteSpace(Remitente()))
        {
            _logger.LogWarning("[EMAIL] Credenciales SMTP no configuradas. Omitiendo envío a {Destinatario} (falta EMAIL_FROM / Email:FromEmail).", destinatario);
            return false;
        }
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
        {
            _logger.LogWarning("[EMAIL] Credenciales SMTP no configuradas. Omitiendo envío a {Destinatario} (falta SMTP_USER o SMTP_PASS).", destinatario);
            return false;
        }

        using var cliente = new SmtpClient();

        try
        {
            var opcionesSsl = puerto == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await cliente.ConnectAsync(host, puerto, opcionesSsl, cancellationToken);
            await cliente.AuthenticateAsync(usuario, clave, cancellationToken);
            await cliente.SendAsync(mensaje, cancellationToken);
            await cliente.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("[EMAIL] Correo enviado correctamente a {Destinatario} vía {Host}:{Puerto}", destinatario, host, puerto);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("[EMAIL] Envío a {Destinatario} cancelado por el cliente.", destinatario);
            return false;
        }
        catch (Exception ex)
        {
            // No-bloqueante: un fallo de email nunca debe romper la petición HTTP principal.
            _logger.LogError(ex, "[EMAIL ERROR] Falló el envío a {Destinatario}: {Mensaje}", destinatario, ex.Message);
            return false;
        }
    }

    private string Remitente() => _options.RemitenteEfectivo;
}
