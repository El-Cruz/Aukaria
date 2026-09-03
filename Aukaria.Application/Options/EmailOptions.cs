namespace Aukaria.Application.Options;

public class EmailOptions
{
    public const string SectionName = "Email";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Aukaria";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;

    public string HostEfectivo => NoVacio(Host) ? Host : SmtpHost;
    public string UsuarioEfectivo => NoVacio(Username) ? Username : SmtpUser;
    public string ClaveEfectiva => NoVacio(Password) ? Password : SmtpPassword;
    public string RemitenteEfectivo => NoVacio(FromEmail) ? FromEmail : FromAddress;

    private static bool NoVacio(string valor) => !string.IsNullOrWhiteSpace(valor);
}
