namespace SubSnapMS.BuildingBlocks.Web.Authentication;

/// <summary>
/// Sezione "Auth" (stessi nomi del gateway). I servizi VERIFICANO i token, non li emettono.
/// </summary>
/// 

//con firma RS256, vengono usate uan coppia di key: private(c'è l'ha solo Identity e serve per firmare. quindi è l'unico che puo creare i tokens) e public(la puo avere chiunque, serve per verificare).  
//il Apigateway rifiuta i tokens non validi, e il service anche lui fa i suoi controlli.
public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Discovery di Identity sulla rete interna (es. http://identity:8080/.well-known/openid-configuration).
    /// Vuoto SOLO in Identity, che valida con le proprie chiavi senza chiamare se stesso via HTTP.
    /// </summary>
    public string? MetadataAddress { get; set; }  //indirizzo del documento di discovery di Identity (sempre su rete docker interna) dove, dove trovi dentro anche le keys. quindi middleware jwt scarica il doc di discovery dall'indirizzo -> legge key 'jwks_uri'(Jason Web Key Set) e SCARICA le key public -> le tiene in cache e le usa per verificare i tokens.  quindi ora le riscarica ancora solo quando arriva un token firmato con una key che non conosce (xk Identity ha ruotato le keys) (è l'opzione RefreshOnIssuerKeyNotFound = true in AddJwtAuthentication).

    public string Issuer { get; set; } = string.Empty;  //chi deve aver emesso il token

    public string Audience { get; set; } = string.Empty;  //per chi è il token

    public int ClockSkewSeconds { get; set; } = 30;  //tolleranza sugli orologio, di solito è 30sec, xk ricorda che gli orologi dei servers non sono mai perfettamente sincronizzati quindi puo capitare e.g.un token scaduto puo sembrare ancora valido.

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Configurazione 'Auth' incompleta: Issuer e Audience sono obbligatori.");
    }  //di default senza Issuer e Audience la verifica non potrebbe check iss e aud, quindi ho questo metodo che fa questo.
}

/*
                        ┌──────────── Identity ────────────┐
Login ───────────────► │ firma il JWT con la CHIAVE PRIVATA │
                       │ RSA (RS256)                         │
                       │ pubblica le CHIAVI PUBBLICHE su:    │
                       │  /.well-known/openid-configuration  │
                       │  /.well-known/jwks.json             │
                       └─────────────────────────────────────┘
                                      ▲ scarica le chiavi pubbliche (una volta, poi cache)
            ┌─────────────────────────┼───────────────────────────┐
         Gateway                Subscriptions                    Media …
   verifica ogni token      verifica ogni token           verifica ogni token
 
 
 ricorda che in appsettings.json hai:
"Auth": {
  "MetadataAddress": "http://identity:8080/.well-known/openid-configuration",
  "Issuer": "subsnapms-identity",
  "Audience": "subsnapms-api",
  "ClockSkewSeconds": 30
}

e in appsettings.Development.json hai:
"Auth": { "MetadataAddress": "http://localhost:5101/.well-known/openid-configuration" }
 */