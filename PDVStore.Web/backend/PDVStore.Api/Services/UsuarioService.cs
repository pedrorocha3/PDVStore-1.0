using Microsoft.EntityFrameworkCore;
using PDVStore.Api.Contracts;
using PDVStore.Api.Data;
using PDVStore.Api.Models;
using PDVStore.Api.Security;

namespace PDVStore.Api.Services;

/// <summary>
/// Usuários do caixa. Preserva as regras do UsuarioService original: nome
/// obrigatório, senha apenas como hash BCrypt, edição sem alterar senha,
/// soft delete e troca de senha exigindo a senha atual. A autenticação web
/// lê a MESMA tabela Usuarios, sem reutilizar o Session do WinForms.
/// </summary>
public class UsuarioService
{
    private readonly PdvStoreDbContext _context;
    private readonly TokenService _tokenService;

    public UsuarioService(PdvStoreDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse?> AutenticarAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var nome = request.Nome.Trim();
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Nome == nome, cancellationToken);

        // Mesma mensagem para usuário inexistente, inativo ou senha errada.
        if (usuario == null || !usuario.Ativo || !usuario.Autenticar(request.Senha))
            return null;

        var (token, expiraEm) = _tokenService.GerarToken(usuario);
        return new LoginResponse(token, expiraEm, usuario.ToDto());
    }

    public async Task<PagedResult<UsuarioDto>> ListarAsync(bool? ativo, PaginacaoQuery query, CancellationToken cancellationToken)
    {
        var consulta = _context.Usuarios.AsNoTracking();
        if (ativo.HasValue)
            consulta = consulta.Where(u => u.Ativo == ativo.Value);

        var total = await consulta.CountAsync(cancellationToken);
        var usuarios = await consulta
            .OrderBy(u => u.Nome)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<UsuarioDto>.Create(
            usuarios.Select(u => u.ToDto()).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<UsuarioDto> ObterPorIdAsync(int id, CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Usuário {id} não encontrado.");

        return usuario.ToDto();
    }

    public async Task<UsuarioDto> CriarAsync(CriarUsuarioRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            throw new ValidacaoException("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < 6)
            throw new ValidacaoException("Senha deve ter pelo menos 6 caracteres.");
        if (!Enum.IsDefined(request.Permissao))
            throw new ValidacaoException("Permissão inválida.");

        var nome = request.Nome.Trim();
        if (await _context.Usuarios.AnyAsync(u => u.Nome == nome, cancellationToken))
            throw new ConflitoException("Já existe um usuário com esse nome.");

        var usuario = new UsuarioCaixa
        {
            Nome = nome,
            Permissao = request.Permissao,
            CreatedAt = DateTime.UtcNow,
            Ativo = true
        };
        usuario.SetSenha(request.Senha);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);
        return usuario.ToDto();
    }

    public async Task<UsuarioDto> AtualizarAsync(int id, AtualizarUsuarioRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            throw new ValidacaoException("Nome é obrigatório.");
        if (!Enum.IsDefined(request.Permissao))
            throw new ValidacaoException("Permissão inválida.");

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Usuário {id} não encontrado.");

        var nome = request.Nome.Trim();
        if (await _context.Usuarios.AnyAsync(u => u.Nome == nome && u.Id != id, cancellationToken))
            throw new ConflitoException("Já existe um usuário com esse nome.");

        usuario.Nome = nome;
        usuario.Permissao = request.Permissao;
        usuario.Ativo = request.Ativo;
        usuario.FotoPath = string.IsNullOrWhiteSpace(request.FotoPath) ? null : request.FotoPath.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return usuario.ToDto();
    }

    public async Task DesativarAsync(int id, CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Usuário {id} não encontrado.");

        usuario.Ativo = false; // soft delete
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AlterarSenhaAsync(int id, AlterarSenhaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Length < 6)
            throw new ValidacaoException("Nova senha deve ter pelo menos 6 caracteres.");

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Usuário {id} não encontrado.");

        if (!usuario.Autenticar(request.SenhaAtual))
            throw new ValidacaoException("Senha atual incorreta.");

        usuario.SetSenha(request.NovaSenha);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
