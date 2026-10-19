using Microsoft.AspNetCore.Mvc;
using PDVStore.Api.Services;

namespace PDVStore.Api.Middleware;

/// <summary>
/// Tratamento centralizado de erros. Converte exceções de domínio em
/// ProblemDetails com status coerente e registra erros inesperados sem expor
/// detalhes internos/segredos ao cliente.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidacaoException ex)
        {
            await EscreverAsync(context, StatusCodes.Status400BadRequest, "Requisição inválida", ex.Message);
        }
        catch (NaoEncontradoException ex)
        {
            await EscreverAsync(context, StatusCodes.Status404NotFound, "Recurso não encontrado", ex.Message);
        }
        catch (ConflitoException ex)
        {
            await EscreverAsync(context, StatusCodes.Status409Conflict, "Conflito de regra de negócio", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await EscreverAsync(context, StatusCodes.Status404NotFound, "Recurso não encontrado", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await EscreverAsync(context, StatusCodes.Status400BadRequest, "Requisição inválida", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado ao processar {Method} {Path}", context.Request.Method, context.Request.Path);
            await EscreverAsync(context, StatusCodes.Status500InternalServerError, "Erro interno",
                "Ocorreu um erro inesperado. Consulte o suporte.");
        }
    }

    private static async Task EscreverAsync(HttpContext context, int status, string titulo, string detalhe)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalhe,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        };

        await context.Response.WriteAsJsonAsync(problema);
    }
}
