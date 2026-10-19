using PDVStore.Api.Models;

namespace PDVStore.Api.Services;

/// <summary>
/// Ponto de integração de pagamento. Mantém o mock didático do PDVStore
/// (sempre aprova e gera PixTxId no PIX), mas atrás de uma interface para que
/// testes possam substituí-lo sem latência e para permitir um gateway real.
/// </summary>
public interface IPagamentoIntegrator
{
    Task<bool> ProcessarPagamentoAsync(Venda venda, CancellationToken cancellationToken = default);
}

public class MockPagamentoIntegrator : IPagamentoIntegrator
{
    private readonly ILogger<MockPagamentoIntegrator> _logger;

    public MockPagamentoIntegrator(ILogger<MockPagamentoIntegrator> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ProcessarPagamentoAsync(Venda venda, CancellationToken cancellationToken = default)
    {
        if (venda.ValorTotal < 0)
            return false;

        await Task.Delay(200, cancellationToken);

        if (venda.FormaPagamento.Equals("PIX", StringComparison.OrdinalIgnoreCase))
            venda.PixTxId = Guid.NewGuid().ToString("N").ToUpperInvariant();

        _logger.LogInformation(
            "Pagamento {FormaPagamento} de {Valor} aprovado (mock). TxId={PixTxId}",
            venda.FormaPagamento, venda.ValorTotal, venda.PixTxId);

        return true;
    }
}
