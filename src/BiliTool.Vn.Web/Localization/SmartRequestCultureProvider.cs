using BiliTool.Vn.Application.Localization;
using Microsoft.AspNetCore.Localization;

namespace BiliTool.Vn.Web.Localization;

public sealed class SmartRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var languageCulture = SmartLanguageDetector.PickFromAcceptLanguage(httpContext.Request.Headers.AcceptLanguage.ToString());
        if (languageCulture is not null)
        {
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(languageCulture, languageCulture));
        }

        var countryCulture = PickFromCountryHeaders(httpContext.Request.Headers);
        if (countryCulture is not null)
        {
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(countryCulture, countryCulture));
        }

        return Task.FromResult<ProviderCultureResult?>(null);
    }

    private static string? PickFromCountryHeaders(IHeaderDictionary headers)
    {
        var cfCountry = SmartLanguageDetector.PickFromCountryCode(headers["CF-IPCountry"].FirstOrDefault());
        if (cfCountry is not null)
        {
            return cfCountry;
        }

        var geoCountry = SmartLanguageDetector.PickFromCountryCode(headers["X-Country-Code"].FirstOrDefault());
        if (geoCountry is not null)
        {
            return geoCountry;
        }

        return SmartLanguageDetector.PickFromCountryCode(headers["X-AppEngine-Country"].FirstOrDefault());
    }
}
