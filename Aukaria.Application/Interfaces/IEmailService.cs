namespace Aukaria.Application.Interfaces;

public interface IEmailService
{
    Task EnviarOtpAsync(string destinatario, string codigoOtp, CancellationToken cancellationToken = default);
    Task EnviarReporteAsync(string destinatario, string nombreArchivo, byte[] adjunto, string fmi, CancellationToken cancellationToken = default);
}
