namespace Aukaria.Application.Interfaces;

public interface IOtpStore
{
    void Guardar(string email, string codigo, TimeSpan expiracion);
    bool Validar(string email, string codigo);
}
