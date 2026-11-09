namespace SubSnapMS.Graph.API.Features.GetServiceRecommendations;

public sealed record ServiceRecommendationsResponse(string BasedOn, IReadOnlyList<ServiceRecommendationResponse> Items);
//BasedOn è "similar-users" (utenti con servizi in comune) o "popular"

public sealed record ServiceRecommendationResponse(string ServiceKey, string Name, string? Category, long Users, long Score);
//ServiceKey è key normalizzata (es. "netflix"): utile al frontend per icone e deduplica, Category è categoria più usata dagli utenti per questo servizio, Users sono utenti distinti con questo servizio attivo (sempre ≥ MinUsersForRecommendation), Score è forza della raccomandazione (più alto = più pertinente)