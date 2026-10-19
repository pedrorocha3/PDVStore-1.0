using Microsoft.EntityFrameworkCore;
using PDVStore.Api.Models;

namespace PDVStore.Api.Data;

/// <summary>
/// Contexto EF Core da API. Mapeia exatamente o schema já existente da aplicação
/// Windows (SQL Server / PDV_StoreDB), com os mesmos nomes de tabelas, colunas,
/// relacionamentos e seeds. A API NÃO executa migrations e NÃO altera o schema.
/// </summary>
public class PdvStoreDbContext : DbContext
{
    public PdvStoreDbContext(DbContextOptions<PdvStoreDbContext> options) : base(options)
    {
    }

    public DbSet<UsuarioCaixa> Usuarios => Set<UsuarioCaixa>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVendas => Set<ItemVenda>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Caixa> Caixas => Set<Caixa>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<ItemCompra> ItensCompras => Set<ItemCompra>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Nomes de tabelas idênticos aos criados pelas migrations do PDVStore.
        modelBuilder.Entity<UsuarioCaixa>().ToTable("Usuarios");
        modelBuilder.Entity<Produto>().ToTable("Produtos");
        modelBuilder.Entity<Venda>().ToTable("Vendas");
        modelBuilder.Entity<ItemVenda>().ToTable("ItensVendas");
        modelBuilder.Entity<MovimentacaoEstoque>().ToTable("MovimentacoesEstoque");
        modelBuilder.Entity<Cliente>().ToTable("Clientes");
        modelBuilder.Entity<Caixa>().ToTable("Caixas");
        modelBuilder.Entity<Fornecedor>().ToTable("Fornecedores");
        modelBuilder.Entity<Compra>().ToTable("Compras");
        modelBuilder.Entity<ItemCompra>().ToTable("ItensCompras");

        // Precisão idêntica ao schema existente (decimal(18,2)).
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }

            }
        }

        modelBuilder.Entity<Caixa>()
            .HasOne(c => c.UsuarioCaixa)
            .WithMany()
            .HasForeignKey(c => c.UsuarioCaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Venda>()
            .HasOne(v => v.Caixa)
            .WithMany(c => c.Vendas)
            .HasForeignKey(v => v.CaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Venda>()
            .HasOne(v => v.Cliente)
            .WithMany()
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Venda>()
            .HasOne(v => v.UsuarioCaixa)
            .WithMany()
            .HasForeignKey(v => v.UsuarioCaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemVenda>()
            .HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemVenda>()
            .HasOne(i => i.Venda)
            .WithMany(v => v.Itens)
            .HasForeignKey(i => i.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasOne(m => m.Produto)
            .WithMany()
            .HasForeignKey(m => m.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasOne(m => m.Usuario)
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Compra>()
            .HasOne(c => c.Fornecedor)
            .WithMany(f => f.Compras)
            .HasForeignKey(c => c.FornecedorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Compra>()
            .HasOne(c => c.UsuarioCaixa)
            .WithMany()
            .HasForeignKey(c => c.UsuarioCaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemCompra>()
            .HasOne(i => i.Compra)
            .WithMany(c => c.Itens)
            .HasForeignKey(i => i.CompraId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemCompra>()
            .HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seeds idênticos aos da aplicação Windows. HasData só é usado pelo modelo
        // (EnsureCreated em desenvolvimento); nunca gera migration na API.
        modelBuilder.Entity<Caixa>().HasData(new Caixa
        {
            Id = 1,
            UsuarioCaixaId = 1,
            Abertura = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
            ValorInicial = 0,
            Status = "Fechado"
        });

        modelBuilder.Entity<UsuarioCaixa>().HasData(new UsuarioCaixa
        {
            Id = 1,
            Nome = "Admin",
            SenhaHash = "$2a$11$ZBR/L6.DiCjgBWjvnJOsMeVfeHtigReLCpU15E8R8FK/Kgn8n1nZa",
            Permissao = TipoPermissao.Administrador,
            CreatedAt = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
            Ativo = true
        });
    }
}
