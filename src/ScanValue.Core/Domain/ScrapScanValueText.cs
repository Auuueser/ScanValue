namespace Auuueser.ScanValue.Core.Domain;

public static class ScrapScanValueText
{
    public const string UnknownValue = "???";

    public static bool HasUnknownValue(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return text.Contains(UnknownValue);
    }

    public static bool TryParseKnownValue(string? text, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(text) || HasUnknownValue(text))
        {
            return false;
        }

        var foundDollar = false;
        var foundDigit = false;
        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (!foundDollar)
            {
                foundDollar = current == '$';
                continue;
            }

            if (current < '0' || current > '9')
            {
                if (foundDigit && (current == ',' || current == ' ' || current == '_'))
                {
                    continue;
                }

                if (foundDigit)
                {
                    break;
                }

                continue;
            }

            foundDigit = true;
            value = value * 10 + current - '0';
        }

        return foundDigit;
    }
}
