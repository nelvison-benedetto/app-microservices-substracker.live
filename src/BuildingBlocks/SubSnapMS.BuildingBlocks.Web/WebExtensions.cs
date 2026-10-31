using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Web.Authentication;
using SubSnapMS.BuildingBlocks.Web.Errors;
using SubSnapMS.BuildingBlocks.Web.Http;
using System.Text.Json.Serialization;

namespace SubSnapMS.BuildingBlocks.Web;

public static class WebExtensions
{
    /// <summary>
    /// Configurazione HTTP comune a ogni servizio: forwarded headers, JSON, gestione errori,
    /// autenticazione JWT (sezione "Auth"), authorization sicura per default, ICurrentUser.
    /// </summary>
    public static WebApplicationBuilder AddWebDefaults(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

        builder.Services.AddServiceForwardedHeaders();

        // Enum come stringhe nelle risposte ("Active" invece di 1)
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // AddProblemDetails è richiesto da UseExceptionHandler() senza parametri; la risposta la scrive ApiExceptionHandler
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();

        builder.Services.AddJwtAuthentication(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Primi middleware della pipeline: forwarded headers, eccezioni, risposte d'errore senza body.
    /// Da chiamare per primo, prima di UseObservability.
    /// </summary>
    public static WebApplication UseWebDefaults(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseExceptionHandler();

        app.UseStatusCodePages(statusCodeContext =>
        {
            var context = statusCodeContext.HttpContext;
            var statusCode = context.Response.StatusCode;

            return ApiErrorWriter.WriteAsync(context, new Responses.ApiError(
                statusCode,
                ApiErrorMessages.For(statusCode),
                ApiErrorMessages.CodeFor(statusCode)));
        });

        return app;
    }

    /// <summary>Autenticazione e autorizzazione. Da chiamare dopo UseObservability e prima della mappatura degli endpoint.</summary>
    public static WebApplication UseWebSecurity(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}