using FluentValidation;
using MiniWms.Application.Common.Sorting;
using MiniWms.Domain.Common;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Locations;

/// <summary>Allowed sort fields. Zones sort the entity; bins sort the projected DTO because <c>zone</c> is the zone code.</summary>
public static class LocationSortFields
{
    public static SortFields<Zone> Zones { get; } = new SortFields<Zone>()
        .Add("code", z => z.Code)
        .Add("name", z => z.Name)
        .Add("type", z => z.Type)
        .Add("isActive", z => z.IsActive);

    public static SortFields<BinDto> Bins { get; } = new SortFields<BinDto>()
        .Add("code", b => b.Code)
        .Add("name", b => b.Name)
        .Add("zone", b => b.ZoneCode)
        .Add("isActive", b => b.IsActive);
}

/// <summary>Rules shared by the Locations validators. Messages match the Domain guards.</summary>
internal static class LocationRules
{
    public const int SearchMaxLength = 100;

    /// <summary>Exact, case-sensitive match to the enum names; numbers and other casings are refused.</summary>
    public static bool TryParseZoneType(string? text, out ZoneType type)
    {
        foreach (var value in Enum.GetValues<ZoneType>())
        {
            if (string.Equals(text, value.ToString(), StringComparison.Ordinal))
            {
                type = value;
                return true;
            }
        }

        type = default;
        return false;
    }

    public static void RequiredText<T>(this IRuleBuilder<T, string?> rule, string label, int maxLength) =>
        rule.Custom((value, context) =>
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                context.AddFailure($"{label} is required.");
            }
            else if (trimmed.Length > maxLength)
            {
                context.AddFailure($"{label} must be at most {maxLength} characters.");
            }
        });

    public static void ValidZoneType<T>(this IRuleBuilder<T, string?> rule, bool required) =>
        rule.Custom((value, context) =>
        {
            if (value is null)
            {
                if (required)
                {
                    context.AddFailure("Type is required.");
                }
            }
            else if (!TryParseZoneType(value, out _))
            {
                context.AddFailure("Type must be Receiving, Storage or Shipping.");
            }
        });

    public static void ValidVersion<T>(this IRuleBuilder<T, int> rule) =>
        rule.GreaterThan(0).WithMessage("Version is required and must be greater than 0.");

    public static void ValidSearch<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(q => q is null || q.Length <= SearchMaxLength)
            .WithMessage($"Search text must be at most {SearchMaxLength} characters.");

    /// <summary>The trimmed search text, or null when blank.</summary>
    public static string? NormalizeSearch(string? q) => string.IsNullOrWhiteSpace(q) ? null : q.Trim();

    public static ValidationFailedException DuplicateCode() =>
        new("code", "Code is already used in the warehouse.");
}
