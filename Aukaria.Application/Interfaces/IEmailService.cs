namespace Aukaria.Application.Interfaces;

public interface IEmailService
{
    Task<bool> EnviarOtpAsync(string destinatario, string codigoOtp, CancellationToken cancellationToken = default);
    Task<bool> EnviarReporteAsync(string destinatario, string nombreArchivo, byte[] adjunto, string fmi, CancellationToken cancellationToken = default);
}
