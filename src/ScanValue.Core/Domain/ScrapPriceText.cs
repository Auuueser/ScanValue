using System.Globalization;

namespace Auuueser.ScanValue.Core.Domain;

public static class ScrapPriceText
{
    public static string Format(int value)
    {
        return string.Concat("$", value.ToString(CultureInfo.InvariantCulture));
    }
}

