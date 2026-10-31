namespace SubSnapMS.Search.API.Features.Suggest;

public sealed record SuggestResponse(IReadOnlyList<SuggestionResponse> Suggestions);

public sealed record SuggestionResponse(Guid SubscriptionId, string Name);