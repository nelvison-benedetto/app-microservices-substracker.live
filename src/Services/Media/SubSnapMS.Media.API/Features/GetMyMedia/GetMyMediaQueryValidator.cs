using FluentValidation;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Features.GetMyMedia;

internal sealed class GetMyMediaQueryValidator : AbstractValidator<GetMyMediaQuery>
{
    public GetMyMediaQueryValidator()
    {
        RuleFor(q => q.Purpose)
            .Must(MediaPurposeExtensions.IsValidCode).When(q => q.Purpose is not null)
            .WithErrorCode("media.purpose_invalid")
            .WithMessage($"Purpose must be one of: {string.Join(", ", MediaPurposeExtensions.AllCodes)}.");
    }
}