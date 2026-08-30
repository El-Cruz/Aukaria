using System.Collections.Concurrent;
using Aukaria.Application.Interfaces;

namespace Aukaria.Infrastructure.Services;

public sealed class InMemoryOtpStore : IOtpStore
{
    private sealed record OtpDato(string Codigo, DateTimeOffset Expira);

    private readonly ConcurrentDictionary<string, OtpDato> _almacen = new();

    public void Guardar(string email, string codigo, TimeSpan expiracion)
    {
        _almacen[email.ToLowerInvariant()] = new OtpDato(codigo, DateTimeOffset.UtcNow.Add(expiracion));
    }

    public bool Validar(string email, string codigo)
    {
        string clave = email.ToLowerInvariant();
        if (!_almacen.TryGetValue(clave, out OtpDato? dato))
        {
            return false;
        }

        if (dato.Expira < DateTimeOffset.UtcNow || dato.Codigo != codigo)
        {
            _almacen.TryRemove(clave, out _);
            return false;
        }

        _almacen.TryRemove(clave, out _);
        return true;
    }
}
