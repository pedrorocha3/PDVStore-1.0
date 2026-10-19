namespace PDVStore.Api.Services;

/// <summary>
/// Exceções de domínio usadas pelos serviços para que o middleware central
/// traduza o erro em um status HTTP adequado, sem regras nos controllers.
/// </summary>
public class ValidacaoException : Exception
{
    public ValidacaoException(string message) : base(message) { }
}

public class ConflitoException : Exception
{
    public ConflitoException(string message) : base(message) { }
}

public class NaoEncontradoException : Exception
{
    public NaoEncontradoException(string message) : base(message) { }
}
