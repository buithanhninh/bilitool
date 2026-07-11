using BiliTool.Vn.Application.Localization;
using FluentAssertions;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public class SmartRequestCultureProviderTests
{
    [Theory]
    [InlineData("fr-FR,fr;q=0.9,en;q=0.7", "fr")]
    [InlineData("en-US,en;q=0.9,vi;q=0.2", "en")]
    [InlineData("vi-VN,vi;q=0.9,en;q=0.5", "vi")]
    [InlineData("de-DE,de;q=0.9,fr;q=0.4", "fr")]
    [InlineData("ja-JP,ja;q=0.9", null)]
    public void PickFromAcceptLanguage_SelectsFirstSupportedLanguageByQuality(string header, string? expected)
    {
        SmartLanguageDetector.PickFromAcceptLanguage(header).Should().Be(expected);
    }

    [Theory]
    [InlineData("VN", "vi")]
    [InlineData("FR", "fr")]
    [InlineData("BE", "fr")]
    [InlineData("CH", "fr")]
    [InlineData("CA", "fr")]
    [InlineData("SN", "fr")]
    [InlineData("US", "en")]
    [InlineData("GB", "en")]
    [InlineData("AU", "en")]
    [InlineData("JP", null)]
    public void PickFromCountryCode_MapsSupportedCountries(string countryCode, string? expected)
    {
        SmartLanguageDetector.PickFromCountryCode(countryCode).Should().Be(expected);
    }
}
