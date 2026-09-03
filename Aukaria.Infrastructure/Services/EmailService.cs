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

    public async Task EnviarOtpAsync(string destinatario, string codigoOtp, CancellationToken cancellationToken = default)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = "Aukaria — Código de verificación";

        mensaje.Body = new TextPart("plain")
        {
            Text = $"Tu código de verificación de Aukaria es: {codigoOtp}\n\nEste código expira en 5 minutos."
        };

        await EnviarAsync(mensaje, cancellationToken);
    }

    public async Task EnviarReporteAsync(string destinatario, string nombreArchivo, byte[] adjunto, string fmi, CancellationToken cancellationToken = default)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = $"Informe Jurídico Predial — FMI {fmi}";

        mensaje.Body = new MimePart("application", "vnd.openxmlformats-officedocument.wordprocessingml.document")
        {
            Content = new MimeContent(new MemoryStream(adjunto), ContentEncoding.Default),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
            FileName = nombreArchivo
        };

        await EnviarAsync(mensaje, cancellationToken);
    }

    private async Task EnviarAsync(MimeMessage mensaje, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new InvalidOperationException("Email no configurado: falta Email__Host.");
        }
        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException("Email no configurado: falta Email__FromEmail (remitente verificado en Brevo).");
        }
        if (string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException("Email no configurado: faltan Email__Username o Email__Password (SMTP key de Brevo).");
        }

        using var cliente = new SmtpClient();

        try
        {
            var opcionesSsl = _options.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;
            await cliente.ConnectAsync(_options.Host, _options.Port, opcionesSsl, cancellationToken);

            await cliente.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

            await cliente.SendAsync(mensaje, cancellationToken);
            await cliente.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo a {Destinatario}. Host: {Host}:{Port}", mensaje.To, _options.Host, _options.Port);
            throw;
        }
    }
}
