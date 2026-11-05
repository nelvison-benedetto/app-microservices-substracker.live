using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SubSnapMS.BuildingBlocks.Web.Errors;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;

namespace SubSnapMS.BuildingBlocks.Web.Authentication;

/*
 AddJwtAuthentication
├── 1. legge e valida la sezione "Auth"
├── 2. AddAuthentication + AddJwtBearer   → COME si verifica un token (+ risposte 401/503)
├── 3. AddAuthorization (FallbackPolicy)   → CHI può entrare: per default solo utenti autenticati
└── 4. ICurrentUser                        → l'utente corrente, comodo negli endpoint
 */

public static class AuthenticationExtensions
{

    private const string SigningKeysUnavailableKey = "web:signing-keys-unavailable";  //prefix, servirà x dire che il token non è stato verificato perché le chiavi di Identity non erano scaricabili

    //validazione JWT RS256 (stesse regole del gateway: difesa in profondità) + authorization sicura per default + ICurrentUser. Identity aggiunge le proprie chiavi con un PostConfigure su JwtBearerOptions.
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)  //extension method
    {
        var auth = configuration.GetSection(JwtAuthenticationOptions.SectionName).Get<JwtAuthenticationOptions>()
                   ?? throw new InvalidOperationException($"Sezione '{JwtAuthenticationOptions.SectionName}' mancante.");
        auth.Validate();  //Get<T>() crea un obj JwtAuthenticationOptions dalla sezione Auth, return null se non esiste ed mex log. Validate() is ur that verifica che Issuer e Audience ci siano.

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)  //schema di default è 'Bearer'
            .AddJwtBearer(options =>
            {
                if (!string.IsNullOrWhiteSpace(auth.MetadataAddress))
                    options.MetadataAddress = auth.MetadataAddress;  //l'indirizzo di discovery di Identity, da cui scaricare le key public

                options.RequireHttpsMetadata = false;   //di default .net accetta solo indirizzo di discovery in https. ora Identity è raggiunto sulla rete Docker interna
                options.MapInboundClaims = false;    //"sub" resta "sub" senza che microsoft rinomini con claim lunghi standart suoi (old style, ancora attivi).
                options.RefreshOnIssuerKeyNotFound = true;   //!🔥🔥!service(ms) riscarica il jwks da Identity se arriva un token firmato con key (kid) che non conosce.
                options.SaveToken = false;   //non conserva il token grezzo nelle properties di auth della req.
                 
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = auth.Issuer,  //"iss" deve essere "subsnapms-identity"
                    ValidateAudience = true,  
                    ValidAudience = auth.Audience,  //"aud" deve essere "subsnapms-api"
                    ValidateLifetime = true,  //"exp" non scaduto
                    RequireExpirationTime = true,  //"exp" deve esistere, altrimenti un token senza quello non scadrebbe mai!
                    ValidateIssuerSigningKey = true,   //la chiave che ha firmato è una di Identity
                    RequireSignedTokens = true,  //niente token senza firma
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],   //🔥🔥solo RS256
                    ClockSkew = TimeSpan.FromSeconds(auth.ClockSkewSeconds),  // tolleranza di 30sec
                    NameClaimType = "sub",  //User.Identity.Name restituisce il valore di sub, cioè l’id dell’utente
                    RoleClaimType = "role"  //User.IsInRole("admin") e [Authorize(Roles = …)] guardano il claim role
                };

                options.Events = new JwtBearerEvents  //gli events, come rispondere quando qualcosa va storto
                {
                    OnAuthenticationFailed = context =>
                    {
                        //Token non valido → SecurityTokenException → 401 normale
                        //Altro (chiavi non scaricabili, service Identity down) → 503, non "token non valido"
                        if (context.Exception is not SecurityTokenException)
                        {
                            context.HttpContext.Items[SigningKeysUnavailableKey] = true;  //mette un segnale in HttpContext.Items, un dizionario che vive per la durata della singola req, ti serve x OnChanllenge() successivo
                            context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("SubSnapMS.BuildingBlocks.Web.Authentication")
                                .LogError(context.Exception, "Cannot validate JWT: signing keys unavailable");
                            context.NoResult();  //auth termina senza risultato
                        }  
                        return Task.CompletedTask;
                    },

                    OnChallenge = async context =>  //x scrivere la res nel formato della platform
                    {
                        context.HandleResponse();
                        if (context.HttpContext.Items.ContainsKey(SigningKeysUnavailableKey))
                        {
                            await ApiErrorWriter.WriteAsync(context.HttpContext, new ApiError(
                                StatusCodes.Status503ServiceUnavailable,
                                "Authentication service temporarily unavailable.",
                                "auth.unavailable"));
                            return;
                        }  //se OnAuthenticationFailed ha lasciato il segnale, la risposta è 503 con codice auth.unavailable, il frontend sa che non deve fare logout, ma riprovare
                        
                        //negli altri casi è 401, distinguendo il token scaduto dali altri motivi...

                        var expired = context.AuthenticateFailure is SecurityTokenExpiredException;

                        context.Response.Headers.WWWAuthenticate = expired
                            ? "Bearer error=\"invalid_token\", error_description=\"The token expired\""
                            : "Bearer";

                        await ApiErrorWriter.WriteAsync(context.HttpContext, new ApiError(
                            StatusCodes.Status401Unauthorized,
                            expired ? "The access token expired." : "Authentication required.",
                            expired ? "auth.token_expired" : "unauthorized"));
                    }
                };
            });

        services.AddAuthorization(options =>   //
        {
            //Sicuro per default: un endpoint dimenticato senza attributi richiede comunque il login!
            //Gli endpoint pubblici dichiarano esplicitamente .AllowAnonymous().
            options.FallbackPolicy = new AuthorizationPolicyBuilder()  //la AuthorizationPolicyBuilder dice “deve essere autenticato”, “deve avere il ruolo X”…
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddHttpContextAccessor();  //🔥registra IHttpContextAccessor che permette di raggiungere HttpContext della req corrente anche da classi che non lo ricevono come parametro!
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}