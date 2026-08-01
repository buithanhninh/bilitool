using BiliTool.Vn.Application.Clinical;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Validators;
using BiliTool.Vn.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public class BilirubinRequestValidationTests
{
    private readonly YeuCauTinhToanValidator _validator = new();

    [Fact]
    public void SameDayBirthAndSample_UsesExplicitTimes()
    {
        var request = ValidRequest();
        request.NgaySinh = new DateTime(2026, 7, 1);
        request.GioSinh = new TimeSpan(8, 0, 0);
        request.NgayLayMau = new DateTime(2026, 7, 1);
        request.GioLayMau = new TimeSpan(10, 0, 0);
        request.TuoiTheoGio = null;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
        BilirubinRequestTimeNormalizer.CalculateAgeHours(request).Should().Be(2d);
    }

    [Fact]
    public void ExplicitAgeAndTimestamps_MustBeConsistent()
    {
        var request = ValidRequest();
        request.TuoiTheoGio = 24;
        request.NgaySinh = new DateTime(2026, 7, 1, 8, 0, 0);
        request.NgayLayMau = new DateTime(2026, 7, 2, 10, 0, 0);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage.Contains("không khớp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NullRiskFactors_ReturnsValidationError()
    {
        var request = ValidRequest();
        request.YeuToNguyCo = null!;

        var action = () => _validator.Validate(request);

        action.Should().NotThrow();
        action().IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(99, 0)]
    [InlineData(0, 99)]
    public void UndefinedEnums_ReturnValidationError(int unit, int phototherapyStatus)
    {
        var request = ValidRequest();
        request.DonViDo = (DonViDo)unit;
        request.TrangThaiChieuDen = (TrangThaiChieuDen)phototherapyStatus;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteAge_ReturnsValidationError(double ageHours)
    {
        var request = ValidRequest();
        request.TuoiTheoGio = ageHours;

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TimeOutsideSingleDay_ReturnsValidationError()
    {
        var request = ValidRequest();
        request.NgaySinh = new DateTime(2026, 7, 1);
        request.NgayLayMau = new DateTime(2026, 7, 3);
        request.GioSinh = TimeSpan.FromHours(25);

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    private static YeuCauTinhToanBilirubinDto ValidRequest() => new()
    {
        TuoiTheoGio = 48,
        TongBilirubin = 12m,
        DonViDo = DonViDo.MgDl,
        TuoiThaiTuan = 38,
        TrangThaiChieuDen = TrangThaiChieuDen.KhongChieuDen,
        YeuToNguyCo = new YeuToNguyCoThanKinhDto()
    };
}
