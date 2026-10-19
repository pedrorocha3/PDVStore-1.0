using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using PDVStore.Api.Contracts;
using PDVStore.Api.Models;
using PDVStore.Api.Security;
using PDVStore.Api.Services;

namespace PDVStore.Api.Tests;

[TestFixture]
public class UsuarioServiceTests : TesteBanco
{
    private UsuarioService Service => new(Context, new TokenService(Options.Create(new JwtOptions
    {
        Key = "chave-de-teste-com-mais-de-32-caracteres-ok",
        Issuer = "PDVStore.Api",
        Audience = "PDVStore.Web"
    })));

    [Test]
    public async Task Autenticar_AdminSeed_RetornaToken()
    {
        var resposta = await Service.AutenticarAsync(
            new LoginRequest { Nome = "Admin", Senha = "admin123" }, CancellationToken.None);

        Assert.That(resposta, Is.Not.Null);
        Assert.That(resposta!.Token, Is.Not.Empty);
        Assert.That(resposta.Usuario.Permissao, Is.EqualTo(TipoPermissao.Administrador));
    }

    [Test]
    public async Task Autenticar_SenhaErrada_RetornaNull()
    {
        var resposta = await Service.AutenticarAsync(
            new LoginRequest { Nome = "Admin", Senha = "errada" }, CancellationToken.None);

        Assert.That(resposta, Is.Null);
    }

    [Test]
    public async Task Autenticar_UsuarioInativo_RetornaNull()
    {
        var admin = await Context.Usuarios.SingleAsync();
        admin.Ativo = false;
        await Context.SaveChangesAsync();

        var resposta = await Service.AutenticarAsync(
            new LoginRequest { Nome = "Admin", Senha = "admin123" }, CancellationToken.None);

        Assert.That(resposta, Is.Null);
    }

    [Test]
    public async Task Criar_GuardaHashNuncaSenhaEmTexto()
    {
        var dto = await Service.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "Operador 1",
            Senha = "senha123",
            Permissao = TipoPermissao.Operador
        }, CancellationToken.None);

        var noBanco = await Context.Usuarios.FindAsync(dto.Id);
        Assert.That(noBanco!.SenhaHash, Is.Not.EqualTo("senha123"));
        Assert.That(noBanco.SenhaHash, Does.StartWith("$2"));
        Assert.That(noBanco.Autenticar("senha123"), Is.True);
    }

    [Test]
    public void Criar_NomeDuplicado_RetornaConflito()
    {
        Assert.ThrowsAsync<ConflitoException>(() => Service.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "Admin",
            Senha = "senha123",
            Permissao = TipoPermissao.Operador
        }, CancellationToken.None));
    }

    [Test]
    public void Criar_SenhaCurta_Rejeita()
    {
        Assert.ThrowsAsync<ValidacaoException>(() => Service.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "Novo",
            Senha = "123",
            Permissao = TipoPermissao.Operador
        }, CancellationToken.None));
    }

    [Test]
    public async Task Atualizar_NaoAlteraSenha()
    {
        var usuario = new UsuarioCaixa { Nome = "Operador", Permissao = TipoPermissao.Operador, CreatedAt = DateTime.UtcNow, Ativo = true };
        usuario.SetSenha("senha123");
        Context.Usuarios.Add(usuario);
        await Context.SaveChangesAsync();
        var hashOriginal = usuario.SenhaHash;

        var dto = await Service.AtualizarAsync(usuario.Id, new AtualizarUsuarioRequest
        {
            Nome = "Operador Renomeado",
            Permissao = TipoPermissao.Estoquista,
            Ativo = true
        }, CancellationToken.None);

        Context.ChangeTracker.Clear();
        var atualizado = await Context.Usuarios.FindAsync(usuario.Id);
        Assert.That(dto.Nome, Is.EqualTo("Operador Renomeado"));
        Assert.That(atualizado!.SenhaHash, Is.EqualTo(hashOriginal));
        Assert.That(atualizado.Permissao, Is.EqualTo(TipoPermissao.Estoquista));
    }

    [Test]
    public async Task AlterarSenha_ExigeSenhaAtual()
    {
        var usuario = new UsuarioCaixa { Nome = "Operador", Permissao = TipoPermissao.Operador, CreatedAt = DateTime.UtcNow, Ativo = true };
        usuario.SetSenha("senha123");
        Context.Usuarios.Add(usuario);
        await Context.SaveChangesAsync();

        Assert.ThrowsAsync<ValidacaoException>(() => Service.AlterarSenhaAsync(usuario.Id,
            new AlterarSenhaRequest { SenhaAtual = "errada", NovaSenha = "nova123" }, CancellationToken.None));

        await Service.AlterarSenhaAsync(usuario.Id,
            new AlterarSenhaRequest { SenhaAtual = "senha123", NovaSenha = "nova123" }, CancellationToken.None);

        Context.ChangeTracker.Clear();
        var atualizado = await Context.Usuarios.FindAsync(usuario.Id);
        Assert.That(atualizado!.Autenticar("nova123"), Is.True);
    }

    [Test]
    public async Task Desativar_FazSoftDelete()
    {
        var usuario = new UsuarioCaixa { Nome = "Operador", SenhaHash = "x", Permissao = TipoPermissao.Operador, CreatedAt = DateTime.UtcNow, Ativo = true };
        Context.Usuarios.Add(usuario);
        await Context.SaveChangesAsync();

        await Service.DesativarAsync(usuario.Id, CancellationToken.None);

        Assert.That((await Context.Usuarios.FindAsync(usuario.Id))!.Ativo, Is.False);
    }
}
