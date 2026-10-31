namespace SubSnapMS.Graph.API.Features.GetServiceRecommendations;

/// <param name="BasedOn">"similar-users" (utenti con servizi in comune) o "popular" (fallback).</param>
public sealed record ServiceRecommendationsResponse(string BasedOn, IReadOnlyList<ServiceRecommendationResponse> Items);

/// <param name="ServiceKey">Chiave normalizzata (es. "netflix"): utile al frontend per icone e deduplica.</param>
/// <param name="Category">Categoria più usata dagli utenti per questo servizio.</param>
/// <param name="Users">Utenti distinti con questo servizio attivo (sempre ≥ MinUsersForRecommendation).</param>
/// <param name="Score">Forza della raccomandazione (più alto = più pertinente).</param>
public sealed record ServiceRecommendationResponse(string ServiceKey, string Name, string? Category, long Users, long Score);