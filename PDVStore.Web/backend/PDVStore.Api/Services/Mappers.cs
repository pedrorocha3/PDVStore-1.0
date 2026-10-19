using PDVStore.Api.Contracts;
using PDVStore.Api.Models;

namespace PDVStore.Api.Services;

/// <summary>Conversões entidade -> DTO (evita expor entidades e ciclos de navegação).</summary>
public static class Mappers
{
    public static ProdutoDto ToDto(this Produto p) => new(
        p.Id, p.Nome, p.CodigoBarras, p.Preco, p.PrecoCusto,
        p.Estoque, p.EstoqueMinimo, p.Categoria, p.Descricao, p.Ativo);

    public static ClienteDto ToDto(this Cliente c) => new(
        c.Id, c.Nome, c.CpfCnpj, c.Telefone, c.Email, c.Endereco,
        c.CadastradoEm, c.Ativo, c.LimiteCredito, c.SaldoDevedor);

    public static FornecedorDto ToDto(this Fornecedor f) => new(
        f.Id, f.Nome, f.Cnpj, f.Telefone, f.Email, f.Ativo);

    public static UsuarioDto ToDto(this UsuarioCaixa u) => new(
        u.Id, u.Nome, u.Permissao, UsuarioCaixa.DescreverPermissao(u.Permissao),
        u.Ativo, u.CreatedAt, u.FotoPath);

    public static CaixaDto ToDto(this Caixa c) => new(
        c.Id, c.UsuarioCaixaId, c.UsuarioCaixa?.Nome, c.Abertura, c.Fechamento,
        c.ValorInicial, c.ValorFinal, c.Sangria, c.Status);

    public static MovimentacaoEstoqueDto ToDto(this MovimentacaoEstoque m) => new(
        m.Id, m.ProdutoId, m.Produto?.Nome, m.Tipo, m.Quantidade, m.PrecoUnitario,
        m.DataMovimentacao, m.UsuarioId, m.Usuario?.Nome, m.Motivo, m.ReferenciaVendaId);

    public static ItemVendaDto ToDto(this ItemVenda i) => new(
        i.Id, i.ProdutoId, i.Produto?.Nome, i.Quantidade, i.PrecoUnitario, i.Subtotal);

    public static VendaDto ToDto(this Venda v) => new(
        v.Id, v.UsuarioCaixaId, v.UsuarioCaixa?.Nome, v.DataVenda, v.ValorTotal,
        v.Desconto, v.FormaPagamento, v.PixTxId, v.Status, v.CaixaId, v.ClienteId,
        v.Cliente?.Nome, v.Itens.OrderBy(i => i.Id).Select(i => i.ToDto()).ToList());

    public static ItemCompraDto ToDto(this ItemCompra i) => new(
        i.Id, i.ProdutoId, i.Produto?.Nome, i.Quantidade, i.PrecoCusto, i.Subtotal);

    public static CompraDto ToDto(this Compra c) => new(
        c.Id, c.FornecedorId, c.Fornecedor?.Nome, c.UsuarioCaixaId, c.UsuarioCaixa?.Nome,
        c.DataCompra, c.NumeroNota, c.ValorTotal, c.Status,
        c.Itens.OrderBy(i => i.Id).Select(i => i.ToDto()).ToList());
}
