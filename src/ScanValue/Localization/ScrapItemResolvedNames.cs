namespace Auuueser.ScanValue.Localization;

internal readonly struct ScrapItemResolvedNames
{
    public ScrapItemResolvedNames(string englishName, string chineseName)
    {
        EnglishName = englishName;
        ChineseName = chineseName;
    }

    public string EnglishName { get; }

    public string ChineseName { get; }
}
