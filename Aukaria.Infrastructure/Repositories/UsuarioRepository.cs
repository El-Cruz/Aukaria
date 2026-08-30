using Aukaria.Application.Interfaces;
using Aukaria.Domain.Entities;
using Aukaria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aukaria.Infrastructure.Repositories;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly AukariaDbContext _context;

    public UsuarioRepository(AukariaDbContext context)
    {
        _context = context;
    }

    public Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
    }

    public Task<Usuario?> ObtenerPorProviderAsync(string provider, string subjectId, CancellationToken cancellationToken = default)
    {
        return _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Provider == provider && u.SubjectId == subjectId, cancellationToken);
    }

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<Usuario> CrearAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        await _context.Usuarios.AddAsync(usuario, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return usuario;
    }

    public async Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<Empresa>> ObtenerEmpresasAsync(CancellationToken cancellationToken = default)
    {
        return _context.Empresas
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
