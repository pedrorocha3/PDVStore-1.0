namespace PDVStore.Api.Services;

/// <summary>
/// Normaliza períodos de consulta. As datas do banco são UTC (como no
/// PDVStore); quando não informado, usa os últimos 30 dias.
/// </summary>
public static class PeriodoHelper
{
    public static (DateTime Inicio, DateTime Fim) Normalizar(DateTime? inicio, DateTime? fim)
    {
        var ate = fim.HasValue ? ParaUtc(fim.Value) : DateTime.UtcNow;
        var de = inicio.HasValue ? ParaUtc(inicio.Value) : ate.AddDays(-30);

        if (de > ate)
            throw new ValidacaoException("A data inicial não pode ser maior que a data final.");

        return (de, ate);
    }

    private static DateTime ParaUtc(DateTime valor) => valor.Kind switch
    {
        DateTimeKind.Utc => valor,
        DateTimeKind.Local => valor.ToUniversalTime(),
        _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc)
    };
}
