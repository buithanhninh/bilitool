namespace BiliTool.Vn.Application.Localization;

public static class SmartLanguageDetector
{
    public static readonly IReadOnlySet<string> SupportedCultureCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "vi", "en", "fr"
    };

    private static readonly IReadOnlyDictionary<string, string> CountryToCulture = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["VN"] = "vi",

        ["FR"] = "fr", ["BE"] = "fr", ["CH"] = "fr", ["LU"] = "fr", ["MC"] = "fr",
        ["CA"] = "fr", ["SN"] = "fr", ["CI"] = "fr", ["BJ"] = "fr", ["BF"] = "fr",
        ["TG"] = "fr", ["NE"] = "fr", ["ML"] = "fr", ["GN"] = "fr", ["CD"] = "fr",
        ["CG"] = "fr", ["CM"] = "fr", ["GA"] = "fr", ["MG"] = "fr", ["RW"] = "fr",
        ["BI"] = "fr", ["DJ"] = "fr", ["KM"] = "fr", ["SC"] = "fr", ["MU"] = "fr",
        ["HT"] = "fr", ["PF"] = "fr", ["NC"] = "fr", ["RE"] = "fr", ["GP"] = "fr",
        ["MQ"] = "fr", ["GF"] = "fr", ["YT"] = "fr", ["PM"] = "fr", ["WF"] = "fr",

        ["US"] = "en", ["GB"] = "en", ["IE"] = "en", ["AU"] = "en", ["NZ"] = "en",
        ["SG"] = "en", ["PH"] = "en", ["IN"] = "en", ["MY"] = "en", ["ZA"] = "en",
        ["NG"] = "en", ["KE"] = "en", ["GH"] = "en", ["UG"] = "en", ["TZ"] = "en",
        ["PK"] = "en", ["BD"] = "en", ["LK"] = "en", ["HK"] = "en", ["MT"] = "en"
    };

    public static string? PickFromAcceptLanguage(string? acceptLanguageHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguageHeader))
        {
            return null;
        }

        return acceptLanguageHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseLanguagePriority)
            .Where(item => item.Code is not null)
            .OrderByDescending(item => item.Quality)
            .Select(item => NormalizeCultureCode(item.Code!))
            .FirstOrDefault(code => code is not null);
    }

    public static string? PickFromCountryCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return null;
        }

        return CountryToCulture.TryGetValue(countryCode.Trim(), out var culture) ? culture : null;
    }

    private static (string? Code, decimal Quality) ParseLanguagePriority(string languageRange)
    {
        var parts = languageRange.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var code = parts.FirstOrDefault();
        var quality = 1.0m;

        foreach (var part in parts.Skip(1))
        {
            if (part.StartsWith("q=", StringComparison.OrdinalIgnoreCase) &&
                decimal.TryParse(part[2..], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                quality = parsed;
            }
        }

        return (code, quality);
    }

    private static string? NormalizeCultureCode(string rawCode)
    {
        var code = rawCode.Trim();
        if (code == "*" || code.Length < 2)
        {
            return null;
        }

        var neutralCode = code[..2].ToLowerInvariant();
        return SupportedCultureCodes.Contains(neutralCode) ? neutralCode : null;
    }
}
