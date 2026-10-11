using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fimple.Atm.Api.Middlewares
{
    public sealed class ExceptionMiddleware
    {
        private const string ErrorCodeExtensionKey = "errorCode";
        private const string TraceIdExtensionKey = "traceId";
        private const string ProblemTypePrefix = "urn:fimple:atm:error:";
        private const string ProblemJsonContentType = "application/problem+json";
        private const string InternalErrorDetail = "An unexpected error occurred.";

        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation("Request was cancelled by the client: {Path}", httpContext.Request.Path);
            }
            catch (AtmException atmException)
            {
                var statusCode = MapStatusCode(atmException);
                _logger.LogWarning(atmException, "Business error {ErrorCode} on {Path}", atmException.ErrorCode, httpContext.Request.Path);
                await WriteProblem(httpContext, statusCode, atmException.ErrorCode, atmException.Message);
            }
            catch (DbUpdateException dbUpdateException) when (dbUpdateException.InnerException is PostgresException postgresException)
            {
                await HandlePostgresException(httpContext, dbUpdateException, postgresException);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
                await WriteProblem(httpContext, StatusCodes.Status500InternalServerError, ErrorCodes.InternalError, InternalErrorDetail);
            }
        }

        private async Task HandlePostgresException(HttpContext httpContext, DbUpdateException dbUpdateException, PostgresException postgresException)
        {
            var isUniqueViolation = postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
            var isCheckViolation = postgresException.SqlState == PostgresErrorCodes.CheckViolation;
            if (!isUniqueViolation && !isCheckViolation)
            {
                _logger.LogError(dbUpdateException, "Database error {SqlState} on {Path}", postgresException.SqlState, httpContext.Request.Path);
                await WriteProblem(httpContext, StatusCodes.Status500InternalServerError, ErrorCodes.InternalError, InternalErrorDetail);
                return;
            }

            var errorCode = isUniqueViolation ? ErrorCodes.UniqueViolation : ErrorCodes.ConstraintViolation;
            _logger.LogWarning(dbUpdateException, "Constraint {ConstraintName} violated on {Path}", postgresException.ConstraintName, httpContext.Request.Path);
            await WriteProblem(httpContext, StatusCodes.Status409Conflict, errorCode,
                $"The operation conflicts with existing data (constraint {postgresException.ConstraintName}).");
        }

        private static int MapStatusCode(AtmException atmException)
        {
            return atmException switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ForbiddenException => StatusCodes.Status403Forbidden,
                ConflictException => StatusCodes.Status409Conflict,
                BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status400BadRequest
            };
        }

        private static async Task WriteProblem(HttpContext httpContext, int statusCode, string errorCode, string detail)
        {
            if (httpContext.Response.HasStarted) return;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = ReasonPhrases.GetReasonPhrase(statusCode),
                Type = ProblemTypePrefix + errorCode,
                Detail = detail,
                Instance = httpContext.Request.Path
            };
            problemDetails.Extensions[ErrorCodeExtensionKey] = errorCode;
            problemDetails.Extensions[TraceIdExtensionKey] = httpContext.TraceIdentifier;

            httpContext.Response.Clear();
            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: ProblemJsonContentType);
        }
    }
}
