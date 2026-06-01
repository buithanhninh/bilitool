using BiliTool.Vn.Domain.Enums;

namespace BiliTool.Vn.Domain.ValueObjects;

/// <summary>
/// Các yếu tố nguy cơ gây độc thần kinh theo phác đồ AAP 2022
/// (Neurotoxicity Risk Factors)
/// </summary>
public record YeuToNguyCoThanKinh
{
    /// <summary>Bệnh tan huyết miễn dịch hoặc thiếu G6PD</summary>
    public bool BenhTanHuyetMienDichHoacThieuG6PD { get; init; }

    /// <summary>Nhiễm khuẩn huyết hoặc nghi ngờ nhiễm khuẩn huyết</summary>
    public bool NhiemKhuanHuyetHoacNghiNgo { get; init; }

    /// <summary>Albumin huyết tương thấp dưới 3.0 g/dL</summary>
    public bool AlbuminThapDuoi3gDl { get; init; }

    /// <summary>ETCOc cao (end-tidal carbon monoxide, hiệu chỉnh theo CO môi trường) tính bằng ppm</summary>
    public decimal? ETCOcPpm { get; init; }

    /// <summary>Tình trạng lâm sàng không ổn định (nhịp tim/hô hấp/nhiệt độ bất thường)</summary>
    public bool TinhTrangLamSangKhongOnDinh { get; init; }

    // ============================================================
    // YẾU TỐ NGUY CƠ LÂM SÀNG (NICE CG98 §1.2.1)
    // ============================================================

    /// <summary>Anh/chị ruột từng bị vàng da cần chiếu đèn (NICE 1.2.1)</summary>
    public bool AnhChiBiVangDaCanChieuDen { get; init; }

    /// <summary>Mẹ dự định bú mẹ hoàn toàn (NICE 1.2.1)</summary>
    public bool MeBuMeHoanToan { get; init; }

    /// <summary>Vàng da xuất hiện trong 24h đầu sau sinh (NICE 1.2.1)</summary>
    public bool VangDaTrong24hDau { get; init; }

    /// <summary>Bệnh tan huyết Rh (cho chỉ định IVIG - NICE 1.8.1)</summary>
    public bool BenhTanHuyetRh { get; init; }

    /// <summary>Bệnh tan huyết ABO (cho chỉ định IVIG - NICE 1.8.1)</summary>
    public bool BenhTanHuyetABO { get; init; }

    /// <summary>Có dấu hiệu lâm sàng bệnh não bilirubin cấp (NICE 1.5.1)</summary>
    public bool DauHieuBenhNaoBilirubinCap { get; init; }

    /// <summary>Kiểm tra xem có bất kỳ yếu tố nguy cơ thần kinh nào không (AAP 2022)</summary>
    public bool CoNguyCoThanKinh =>
        BenhTanHuyetMienDichHoacThieuG6PD ||
        NhiemKhuanHuyetHoacNghiNgo ||
        AlbuminThapDuoi3gDl ||
        (ETCOcPpm.HasValue && ETCOcPpm.Value > 1.7m) ||
        TinhTrangLamSangKhongOnDinh;

    /// <summary>Có yếu tố nguy cơ lâm sàng NICE (dùng cho lịch đo lặp)</summary>
    public bool CoNguyCoLamSangNICE =>
        AnhChiBiVangDaCanChieuDen ||
        MeBuMeHoanToan;

    /// <summary>Có bệnh tan huyết miễn dịch Rh hoặc ABO (cho IVIG)</summary>
    public bool CoBenhTanHuyetMienDich =>
        BenhTanHuyetRh || BenhTanHuyetABO;

    /// <summary>Không có yếu tố nguy cơ</summary>
    public static YeuToNguyCoThanKinh KhongCoNguyCoThanKinh => new();

    /// <summary>Mô tả các yếu tố nguy cơ hiện tại</summary>
    public IReadOnlyList<string> DanhSachNguyCoHienTai()
    {
        var ds = new List<string>();
        if (BenhTanHuyetMienDichHoacThieuG6PD)
            ds.Add("Bệnh tan huyết miễn dịch / Thiếu G6PD");
        if (NhiemKhuanHuyetHoacNghiNgo)
            ds.Add("Nhiễm khuẩn huyết hoặc nghi ngờ nhiễm khuẩn huyết");
        if (AlbuminThapDuoi3gDl)
            ds.Add("Albumin huyết thanh < 3.0 g/dL");
        if (ETCOcPpm.HasValue && ETCOcPpm.Value > 1.7m)
            ds.Add($"ETCOc tăng cao ({ETCOcPpm.Value:F1} ppm)");
        if (TinhTrangLamSangKhongOnDinh)
            ds.Add("Tình trạng lâm sàng không ổn định");
        if (AnhChiBiVangDaCanChieuDen)
            ds.Add("Anh/chị ruột từng bị vàng da cần chiếu đèn (NICE)");
        if (MeBuMeHoanToan)
            ds.Add("Mẹ dự định bú mẹ hoàn toàn (NICE)");
        if (VangDaTrong24hDau)
            ds.Add("Vàng da xuất hiện trong 24h đầu (NICE)");
        if (BenhTanHuyetRh)
            ds.Add("Bệnh tan huyết Rh (NICE)");
        if (BenhTanHuyetABO)
            ds.Add("Bệnh tan huyết ABO (NICE)");
        if (DauHieuBenhNaoBilirubinCap)
            ds.Add("Dấu hiệu bệnh não bilirubin cấp (NICE)");
        return ds.AsReadOnly();
    }
}
