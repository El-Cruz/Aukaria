using Aukaria.Application.Interfaces;
using Aukaria.Application.Options;
using MailKit.Net.Smtp;
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
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogWarning("EmailService no configurado (falta host o from). No se envió el correo.");
            return;
        }

        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(_options.Host, _options.Port, _options.UseSsl, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await cliente.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }

        await cliente.SendAsync(mensaje, cancellationToken);
        await cliente.DisconnectAsync(true, cancellationToken);
    }
}
