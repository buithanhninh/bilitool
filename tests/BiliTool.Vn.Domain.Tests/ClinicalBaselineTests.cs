using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Domain.Services;
using BiliTool.Vn.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public class ClinicalBaselineTests
{
    private readonly MayTinhBilirubin _mayTinh = new();

    public static TheoryData<int, decimal> NguongChieuDen48hKhongNguyCo => new()
    {
        { 35, 10.5m },
        { 36, 10.9m },
        { 37, 11.3m },
        { 38, 14.6m },
        { 39, 14.6m },
        { 40, 14.6m },
    };

    [Theory]
    [MemberData(nameof(NguongChieuDen48hKhongNguyCo))]
    public void AAP2022_Baseline_NguongChieuDen48hKhongNguyCo_KhongDuocDrift(int tuoiThaiTuan, decimal expectedNguong)
    {
        var ketQua = _mayTinh.TinhToan(48, 10m, tuoiThaiTuan, YeuToNguyCoThanKinh.KhongCoNguyCoThanKinh);

        ketQua.NguongChieuDen.Should().Be(expectedNguong);
    }

    [Fact]
    public void AAP2022_Baseline_NoiSuyTuyenTinh_KhongDuocDrift()
    {
        var ketQua = _mayTinh.TinhToan(54, 10m, 38, YeuToNguyCoThanKinh.KhongCoNguyCoThanKinh);

        ketQua.NguongChieuDen.Should().Be(15.3m);
    }

    [Fact]
    public void AAP2022_Baseline_CoNguyCoThanKinh_KhongDuocDrift()
    {
        var nguyCo = new YeuToNguyCoThanKinh { BenhTanHuyetMienDichHoacThieuG6PD = true };

        var ketQua = _mayTinh.TinhToan(48, 10m, 38, nguyCo);

        ketQua.NguongChieuDen.Should().Be(14.0m);
    }

    [Fact]
    public void ClinicalFacade_BocBaselineEngine_KhongLamDoiNguong()
    {
        var facade = new BilirubinClinicalFacade(_mayTinh);

        var baseline = _mayTinh.TinhToan(48, 10m, 38, YeuToNguyCoThanKinh.KhongCoNguyCoThanKinh);
        var wrapped = facade.TinhToan(48, 10m, 38, YeuToNguyCoThanKinh.KhongCoNguyCoThanKinh);

        wrapped.KetQua.NguongChieuDen.Should().Be(baseline.NguongChieuDen);
        wrapped.KetQua.NguongChieuDenTichCuc.Should().Be(baseline.NguongChieuDenTichCuc);
        wrapped.KetQua.NguongThayCuuMau.Should().Be(baseline.NguongThayCuuMau);
        wrapped.Trace.EngineMode.Should().Be("BaselineMayTinhBilirubin");
        wrapped.Trace.PhacDoQuyetDinh.Should().Be(baseline.PhacDoQuyetDinh);
    }
}
