using Microsoft.AspNetCore.HttpOverrides;
using SubSnapMS.ApiGateway.Extensions;
using SubSnapMS.ApiGateway.Proxy;
using SubSnapMS.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);

//traefix(better than nginx x docker containers, auto rileva e non devi fare settaggi a mano) (che lo aggiungi sul server, che sta prima del gateway) decifra tls, e aggiunge X-Forwarded-For / X-Forwarded-Proto

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
//x non rileva 'server:Kestrel' nelle risposte

//lib OpenTelemetry (trace, metriche, log), Sentry, health check
builder.AddObservability(serviceName: "api-gateway");


builder.Services.AddGatewayForwardedHeaders();
builder.Services.AddGatewayCors(builder.Configuration);
builder.Services.AddGatewayAuthentication(builder.Configuration);
builder.Services.AddGatewayRateLimiting(builder.Configuration);


builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy")).AddConfigFilter<ClusterDefaultsConfigFilter>();
//yarp legge routes e clusters dalla sezione 'ReverseProxy', il filtro aggiunge ad ogni cluster health checks e timeout default

var app = builder.Build();

app.UseForwardedHeaders();  //UseForwardedHeaders  legge gli header di traefik
//visto che uso traefik davanti, ora nginx dice al mio gateway che la req originale dal web usava https e da anche l'ip address.
app.UseGatewayErrorHandling();    // 2. eccezioni e risposte d'errore senza body → envelope ApiResult
app.UseSecurityHeaders();         // 3. header di sicurezza su ogni risposta
app.UseObservability();  //Header X-Correlation-Id (= TraceId) + endpoint /health/live e /health/ready

app.UseRouting();                 // 5. sceglie la route (serve a CORS, rate limiter e authorization)
app.UseGatewayCors();             // 6. attivo solo se Cors:AllowedOrigins non è vuoto
app.UseAuthentication();          // 7. valida il JWT
app.UseRateLimiter();             // 8. dopo l'autenticazione: partiziona per utente
app.UseAuthorization();           // 9. FallbackPolicy: tutto richiede JWT tranne le route "anonymous"


app.MapReverseProxy(proxy =>
{
    proxy.UseProxyErrorEnvelope();   // 502/503/504 nel formato ApiResult del frontend
    proxy.UseSessionAffinity();
    proxy.UseLoadBalancing();
    proxy.UsePassiveHealthChecks();
});

app.Run();



/*
/api/**    → endpoint YARP della route "monolith-api" 
dopo che match trovato ok, ogni route yarp esegue  session affinity(se attivata, manada stesso client a stessa destination e.g.tramite cookie) -> load balancing(la policy di default PowerOfTwoChoices prende 2 destinazioni random e sceglie quella con meno req in corso), passive health checks (è come il CircuitBreaker policy, se una destinazione fallisce ripetutamente aspetta un po prima di riprovare)
 */







