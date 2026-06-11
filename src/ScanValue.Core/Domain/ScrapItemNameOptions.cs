using System;

namespace Auuueser.ScanValue.Core.Domain;

public readonly record struct ScrapItemNameOptions(bool ShowItemNames, ScrapItemNameLanguage Language)
{
    public static ScrapItemNameOptions Default { get; } = new(true, ScrapItemNameLanguage.Auto);

    public static ScrapItemNameOptions Create(bool showItemNames, string? language)
    {
        return new ScrapItemNameOptions(showItemNames, ParseLanguage(language));
    }

    public static ScrapItemNameLanguage ParseLanguage(string? value)
    {
        return Enum.TryParse<ScrapItemNameLanguage>(value, ignoreCase: true, out var parsed)
            ? parsed
            : ScrapItemNameLanguage.Auto;
    }
}
