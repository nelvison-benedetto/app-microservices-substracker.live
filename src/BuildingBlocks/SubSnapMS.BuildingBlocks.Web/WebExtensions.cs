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
    //configurazione HTTP comune a ogni servizio: forwarded headers, JSON, gestione errori, autenticazione JWT (sezione "Auth"), authorization sicura per default, ICurrentUser.

    public static WebApplicationBuilder AddWebDefaults(this WebApplicationBuilder builder)  //extension method
    {
        builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
        builder.Services.AddServiceForwardedHeaders();

        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));  //Enum come stringhe nelle risposte ("Active" invece di 1)

        builder.Services.AddProblemDetails();  //AddProblemDetails è richiesto da UseExceptionHandler() senza parametri, la risposta la scrive ApiExceptionHandler
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddJwtAuthentication(builder.Configuration);
        return builder;
    }


    //primi middleware della pipeline: forwarded headers, eccezioni, risposte d'errore senza body. Da chiamare per primo, prima di UseObservability.
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

    //Autenticazione e autorizzazione. Da chiamare dopo UseObservability e prima della mappatura degli endpoint
    public static WebApplication UseWebSecurity(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }


}