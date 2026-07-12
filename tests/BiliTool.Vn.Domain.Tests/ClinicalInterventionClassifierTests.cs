using BiliTool.Vn.Domain.Clinical.Bilirubin;
using FluentAssertions;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public class ClinicalInterventionClassifierTests
{
    [Theory]
    [InlineData(10, 15, 22, ClinicalInterventionCategory.BinhThuong)]
    [InlineData(15, 15, 22, ClinicalInterventionCategory.ChieuDen)]
    [InlineData(19.9, 15, 22, ClinicalInterventionCategory.ChieuDen)]
    [InlineData(20, 15, 22, ClinicalInterventionCategory.EscalationOfCare)]
    [InlineData(22, 15, 22, ClinicalInterventionCategory.ThayMau)]
    public void Classify_UsesStoredAapThresholds(
        decimal bilirubin,
        decimal phototherapyThreshold,
        decimal exchangeThreshold,
        ClinicalInterventionCategory expected)
    {
        ClinicalInterventionClassifier.Classify(bilirubin, phototherapyThreshold, exchangeThreshold)
            .Should().Be(expected);
    }

    public static TheoryData<decimal, decimal?, decimal?> InvalidInputs => new()
    {
        { 10m, null, 22m },
        { 10m, 15m, null },
        { 10m, 22m, 15m },
        { -1m, 15m, 22m }
    };

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void Classify_ReturnsInsufficientData_ForInvalidInputs(
        decimal bilirubin,
        decimal? phototherapyThreshold,
        decimal? exchangeThreshold)
    {
        ClinicalInterventionClassifier.Classify(bilirubin, phototherapyThreshold, exchangeThreshold)
            .Should().Be(ClinicalInterventionCategory.KhongDuDuLieu);
    }
}
