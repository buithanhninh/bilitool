using BiliTool.Vn.Domain.Enums;
using BiliTool.Vn.Domain.Services;
using BiliTool.Vn.Domain.ValueObjects;
using FluentAssertions;
using Xunit;


namespace BiliTool.Vn.Domain.Tests;

/// <summary>
/// Unit tests cho MayTinhBilirubin - Engine tính toán ngưỡng bilirubin
/// Kiểm tra các ca lâm sàng quan trọng
/// </summary>
public class MayTinhBilirubinTests
{
    private readonly MayTinhBilirubin _mayTinh = new();
    private static readonly YeuToNguyCoThanKinh KhongNguyCo = YeuToNguyCoThanKinh.KhongCoNguyCoThanKinh;

    // ── Kiểm tra chuyển đổi đơn vị ────────────────────────────

    [Fact]
    public void ChuyenDoiUmolLSangMgDl_PhaiBinhThuong()
    {
        // 171 μmol/L ≈ 10 mg/dL
        decimal ket = _mayTinh.ChuyenDoiSangMgDl(171m, DonViDo.UmolL);
        ket.Should().BeApproximately(10.0m, 0.1m);
    }

    [Fact]
    public void ChuyenDoiMgDlGiuNguyen_KhiDonViDaMgDl()
    {
        decimal ket = _mayTinh.ChuyenDoiSangMgDl(15.5m, DonViDo.MgDl);
        ket.Should().Be(15.5m);
    }

    // ── Kiểm tra tính toán cơ bản ─────────────────────────────

    [Fact]
    public void TinhToan_TreBinhThuong_KhongCanChieuDen()
    {
        // Trẻ đủ tháng (38 tuần), 48h tuổi, bilirubin 10 mg/dL
        // 10 mg/dL < ngưỡng chiếu đèn 14 mg/dL → bình thường
        var ket = _mayTinh.TinhToan(48, 10.0m, 38, KhongNguyCo);

        ket.CanChieuDenNgay.Should().BeFalse();
        ket.CanXemXetThayCuuMau.Should().BeFalse();
        ket.MucDoNguyHiem.Should().Be(MucDoNguyHiem.BinhThuong);
    }

    [Fact]
    public void TinhToan_TreCanChieuDen_KhiVuotNguong()
    {
        // Trẻ 38 tuần, 48h, bilirubin 17.0 mg/dL > ngưỡng AAP 2022 (16.0) → cần chiếu đèn
        var ket = _mayTinh.TinhToan(48, 17.0m, 38, KhongNguyCo);

        ket.CanChieuDenNgay.Should().BeTrue();
        ket.CanXemXetThayCuuMau.Should().BeFalse();
    }

    [Fact]
    public void TinhToan_NguongGiamKhiCoNguyCo_ThanKinh()
    {
        // Ngưỡng chiếu đèn phải thấp hơn khi có nguy cơ thần kinh
        var ketKhongNguyCo = _mayTinh.TinhToan(48, 12.0m, 38, KhongNguyCo);
        var nguyCo = new YeuToNguyCoThanKinh { BenhTanHuyetMienDichHoacThieuG6PD = true };
        var ketCoNguyCo = _mayTinh.TinhToan(48, 12.0m, 38, nguyCo);

        ketCoNguyCo.NguongChieuDen.Should().BeLessThan(ketKhongNguyCo.NguongChieuDen);
    }

    [Fact]
    public void TinhToan_TreSinhNon_NguongThapHon_TreDuThang()
    {
        // Trẻ 35 tuần có ngưỡng thấp hơn trẻ 38 tuần
        var ket38 = _mayTinh.TinhToan(48, 10.0m, 38, KhongNguyCo);
        var ket35 = _mayTinh.TinhToan(48, 10.0m, 35, KhongNguyCo);

        ket35.NguongChieuDen.Should().BeLessThan(ket38.NguongChieuDen);
    }

    [Fact]
    public void TinhToan_BilirubinCao_KhanCapThayMau()
    {
        // Trẻ 36 tuần, 48h, bilirubin 22 mg/dL → khẩn cấp thay máu
        var ket = _mayTinh.TinhToan(48, 22.0m, 36, KhongNguyCo);

        ket.CanXemXetThayCuuMau.Should().BeTrue();
        ket.MucDoNguyHiem.Should().BeOneOf(
            MucDoNguyHiem.CanXemXetThayMau,
            MucDoNguyHiem.KhanCapThayCuuMau);
    }

    [Fact]
    public void TinhToan_NguongThayCuuMau_CaoHon_NguongChieuDen()
    {
        var ket = _mayTinh.TinhToan(72, 12.0m, 38, KhongNguyCo);
        ket.NguongThayCuuMau.Should().BeGreaterThan(ket.NguongChieuDen);
    }

    // ── Kiểm tra validation ───────────────────────────────────

    [Fact]
    public void TinhToan_TuoiGio_AmSo_NemException()
    {
        Action act = () => _mayTinh.TinhToan(-1, 10.0m, 38, KhongNguyCo);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TinhToan_TuoiThai_Duoi35_NemException()
    {
        // Phác đồ AAP 2022 chỉ áp dụng cho trẻ ≥ 35 tuần
        Action act = () => _mayTinh.TinhToan(48, 10.0m, 34, KhongNguyCo);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TinhToan_Bilirubin_AmSo_NemException()
    {
        Action act = () => _mayTinh.TinhToan(48, -1.0m, 38, KhongNguyCo);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Kiểm tra xu hướng ─────────────────────────────────────

    [Fact]
    public void TinhTocDoThayDoi_TangNhanh_Trong24hDau()
    {
        // 0.4 mg/dL/giờ trong 24h đầu → tăng nhanh (>= 0.3)
        var lichSu = new List<MauBilirubinTheoThoiGian>
        {
            new(DateTime.Today.AddHours(-5), 8.0m, 10),
            new(DateTime.Today, 10.0m, 15)  // +2.0 mg/dL trong 5 giờ = 0.4/giờ
        };

        var ket = _mayTinh.TinhTocDoThayDoi(lichSu);

        ket.LaTangNhanh.Should().BeTrue();
    }

    [Fact]
    public void TinhTocDoThayDoi_TangCham_KhongCanhBao()
    {
        // 0.1 mg/dL/giờ → không tăng nhanh
        var lichSu = new List<MauBilirubinTheoThoiGian>
        {
            new(DateTime.Today.AddHours(-10), 10.0m, 48),
            new(DateTime.Today, 11.0m, 58)  // +1.0 mg/dL trong 10h = 0.1/giờ
        };

        var ket = _mayTinh.TinhTocDoThayDoi(lichSu);

        ket.LaTangNhanh.Should().BeFalse();
    }

    [Fact]
    public void TinhTocDoThayDoi_DuoiHaiMau_TraVeDuLieuKhongDu()
    {
        var lichSu = new List<MauBilirubinTheoThoiGian>
        {
            new(DateTime.Today, 10.0m, 48)
        };

        var ket = _mayTinh.TinhTocDoThayDoi(lichSu);

        ket.DuLieuKhongDu.Should().BeTrue();
    }

    // ── Kiểm tra NICE CG98 ──────────────────────────────────────

    [Fact]
    public void TinhToan_NICE_NguyCoKernicterus_KhiBilirubinCao()
    {
        // Trẻ ≥37 tuần, bilirubin > 340 µmol/L (≈19.9 mg/dL) → nguy cơ kernicterus
        var ket = _mayTinh.TinhToan(48, 20.5m, 38, KhongNguyCo);
        ket.NguyCoKernicterus.Should().BeTrue();
        ket.LyDoNguyCoKernicterus.Should().Contain(l => l.Contains("340"));
    }

    [Fact]
    public void TinhToan_NICE_ChuThichThamChieu_LuonCoGiaTri()
    {
        var ket = _mayTinh.TinhToan(48, 12.0m, 38, KhongNguyCo);
        ket.ChuThichThamChieu.Should().NotBeEmpty();
        ket.NguongChieuDen_NICE_UmolL.Should().BeGreaterThan(0);
        ket.NguongThayCuuMau_NICE_UmolL.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TinhToan_NICE_PhacDoQuyetDinh_PhaiCoGiaTri()
    {
        var ket = _mayTinh.TinhToan(48, 12.0m, 38, KhongNguyCo);
        ket.PhacDoQuyetDinh.Should().BeOneOf(
            Domain.Enums.PhacDo.AAP2022,
            Domain.Enums.PhacDo.NICE_CG98);
    }
}
