using FluentValidation;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Features.RequestUpload;

internal sealed class RequestUploadCommandValidator : AbstractValidator<RequestUploadCommand>
{
    public RequestUploadCommandValidator()
    {
        RuleFor(c => c.Purpose)
            .NotEmpty().WithErrorCode("media.purpose_required").WithMessage("Purpose is required.")
            .Must(MediaPurposeExtensions.IsValidCode).WithErrorCode("media.purpose_invalid")
                .WithMessage($"Purpose must be one of: {string.Join(", ", MediaPurposeExtensions.AllCodes)}.");

        RuleFor(c => c.ContentType)
            .NotEmpty().WithErrorCode("media.content_type_required").WithMessage("Content type is required.")
            .Must((command, contentType) => IsAllowed(command.Purpose, contentType))
                .When(c => MediaPurposeExtensions.IsValidCode(c.Purpose))
                .WithErrorCode("media.content_type_not_allowed")
                .WithMessage(c => $"Allowed types: {string.Join(", ", MediaUploadPolicy.AllowedContentTypes(MediaPurposeExtensions.Parse(c.Purpose)))}.");

        RuleFor(c => c.SizeBytes)
            .GreaterThan(0).WithErrorCode("media.size_invalid").WithMessage("File size must be greater than zero.")
            .Must((command, size) => size <= MediaUploadPolicy.MaxSizeBytes(MediaPurposeExtensions.Parse(command.Purpose)))
                .When(c => MediaPurposeExtensions.IsValidCode(c.Purpose))
                .WithErrorCode("media.too_large")
                .WithMessage(c => $"File cannot exceed {MediaUploadPolicy.MaxSizeBytes(MediaPurposeExtensions.Parse(c.Purpose)) / (1024 * 1024)} MB.");
    }

    private static bool IsAllowed(string purpose, string contentType) =>
        MediaUploadPolicy.AllowedContentTypes(MediaPurposeExtensions.Parse(purpose))
            .Contains(contentType.Trim().ToLowerInvariant());
}