using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Services;

namespace Theatre.Api.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Validation failed", ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Conflict, "Conflict", ex.Message);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is SqlException sqlException)
        {
            logger.LogWarning(ex, "Database rejected a request with SQL error {ErrorNumber}", sqlException.Number);
            var (status, title, detail) = DatabaseProblem(sqlException);
            await WriteProblemAsync(context, status, title, detail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled request exception");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Server error", "An unexpected error occurred.");
        }
    }

    private static (HttpStatusCode Status, string Title, string Detail) DatabaseProblem(SqlException exception)
    {
        if (exception.Number is 2601 or 2627)
        {
            var detail = exception.Message switch
            {
                var message when message.Contains("People_FullName", StringComparison.OrdinalIgnoreCase) =>
                    "A person with this name already exists. Reuse the existing person or change the name.",
                var message when message.Contains("ShowTranslations", StringComparison.OrdinalIgnoreCase) =>
                    "Another play already uses this generated page address. Change the title and try again.",
                var message when message.Contains("NewsArticleTranslations", StringComparison.OrdinalIgnoreCase) =>
                    "Another news article already uses this page address. Change the title or slug and try again.",
                var message when message.Contains("ContentHash", StringComparison.OrdinalIgnoreCase) =>
                    "This exact file already exists in the Media Library. Choose the existing file instead.",
                var message when message.Contains("Email", StringComparison.OrdinalIgnoreCase) =>
                    "This email address is already registered.",
                _ => "Another record already uses the same value. Review duplicated names, titles, dates, or identifiers and try again."
            };
            return (HttpStatusCode.Conflict, "Duplicate information", detail);
        }

        if (exception.Number == 547)
            return (HttpStatusCode.Conflict, "Related content prevents this change",
                "This item is being used elsewhere, or one of the selected related items no longer exists. Refresh the page and review the selection.");

        if (exception.Number == 515)
            return (HttpStatusCode.BadRequest, "Required information is missing",
                "A required value was not provided. Review the form and complete all required fields.");

        return (HttpStatusCode.InternalServerError, "Unable to save changes",
            "The database could not save this change. Try again; if it continues, contact an administrator and include the time of the error.");
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string title, string detail)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
