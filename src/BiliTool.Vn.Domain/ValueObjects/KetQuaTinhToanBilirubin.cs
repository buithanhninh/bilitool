using BiliTool.Vn.Domain.Enums;

namespace BiliTool.Vn.Domain.ValueObjects;

/// <summary>
/// Kết quả tính toán ngưỡng bilirubin kết hợp AAP 2022 + NICE CG98.
/// Value Object - bất biến sau khi tạo.
/// </summary>
public record KetQuaTinhToanBilirubin
{
    // ============================================================
    // Thông tin đầu vào (ghi lại để tham chiếu)
    // ============================================================
    public double TuoiGio { get; init; }
    public int TuoiThaiTuan { get; init; }
    public decimal BilirubinMgDl { get; init; }
    public decimal BilirubinUmolL { get; init; }   // Tính sẵn khi tạo
    public bool CoNguyCoThanKinh { get; init; }
    public DateTime ThoiGianTinhToan { get; init; }

    // ============================================================
    // Ngưỡng điều trị AAP 2022 (mg/dL)
    // ============================================================
    /// <summary>Ngưỡng bắt đầu chiếu đèn AAP 2022 (mg/dL)</summary>
    public decimal NguongChieuDen { get; init; }

    /// <summary>Ngưỡng chiếu đèn tích cực AAP 2022 (= ET - 2 mg/dL)</summary>
    public decimal NguongChieuDenTichCuc { get; init; }

    /// <summary>Ngưỡng cân nhắc thay máu AAP 2022 (mg/dL)</summary>
    public decimal NguongThayCuuMau { get; init; }

    /// <summary>Khoảng cách đến ngưỡng chiếu đèn AAP (mg/dL). Âm = đã vượt.</summary>
    public decimal KhoangCachDenNguongChieuDen { get; init; }

    /// <summary>Khoảng cách đến ngưỡng thay máu AAP (mg/dL). Âm = đã vượt.</summary>
    public decimal KhoangCachDenNguongThayCuuMau { get; init; }

    // ============================================================
    // Ngưỡng điều trị NICE CG98 (µmol/L)
    // ============================================================
    /// <summary>Ngưỡng chiếu đèn NICE CG98 (µmol/L)</summary>
    public decimal NguongChieuDen_NICE_UmolL { get; init; }

    /// <summary>Ngưỡng thay máu NICE CG98 (µmol/L)</summary>
    public decimal NguongThayCuuMau_NICE_UmolL { get; init; }

    /// <summary>Ngưỡng chiếu đèn NICE quy đổi sang mg/dL</summary>
    public decimal NguongChieuDen_NICE_MgDl { get; init; }

    /// <summary>Ngưỡng thay máu NICE quy đổi sang mg/dL</summary>
    public decimal NguongThayCuuMau_NICE_MgDl { get; init; }

    /// <summary>Khoảng cách đến ngưỡng chiếu đèn NICE (µmol/L). Âm = đã vượt.</summary>
    public decimal KhoangCachDenNguongChieuDen_NICE { get; init; }

    /// <summary>Khoảng cách đến ngưỡng thay máu NICE (µmol/L). Âm = đã vượt.</summary>
    public decimal KhoangCachDenNguongThayCuuMau_NICE { get; init; }

    // ============================================================
    // Đánh giá và khuyến nghị (kết hợp)
    // ============================================================
    public MucDoNguyHiem MucDoNguyHiem { get; init; }
    public ThoiGianTaiKham ThoiGianTaiKham { get; init; }

    /// <summary>Phác đồ quyết định mức độ nguy hiểm (AAP hay NICE áp ngưỡng thấp hơn)</summary>
    public PhacDo PhacDoQuyetDinh { get; init; }

    /// <summary>Có cần chiếu đèn ngay không</summary>
    public bool CanChieuDenNgay { get; init; }

    /// <summary>Có cần cân nhắc thay máu không</summary>
    public bool CanXemXetThayCuuMau { get; init; }

    /// <summary>Có cần chiếu đèn tích cực không</summary>
    public bool CanChieuDenTichCuc => BilirubinMgDl >= NguongChieuDenTichCuc;

    // ============================================================
    // NICE CG98: Đánh giá nguy cơ Kernicterus (§1.5.1)
    // ============================================================
    /// <summary>Có nguy cơ kernicterus theo NICE 1.5.1</summary>
    public bool NguyCoKernicterus { get; init; }

    /// <summary>Lý do nguy cơ kernicterus</summary>
    public List<string> LyDoNguyCoKernicterus { get; init; } = new();

    // ============================================================
    // NICE CG98: Vàng da kéo dài (§1.7)
    // ============================================================
    /// <summary>Có phải vàng da kéo dài không (≥37w: >14 ngày; <37w: >21 ngày)</summary>
    public bool LaVangDaKeoDai { get; init; }

    /// <summary>Cảnh báo vàng da kéo dài</summary>
    public string? CanhBaoVangDaKeoDai { get; init; }

    // ============================================================
    // NICE CG98: Khuyến nghị IVIG (§1.8.1)
    // ============================================================
    /// <summary>Có chỉ định IVIG không</summary>
    public bool CanIVIG { get; init; }

    /// <summary>Mô tả chỉ định IVIG</summary>
    public string? MoTaIVIG { get; init; }

    // ============================================================
    // NICE CG98: Quy tắc dừng chiếu đèn (§1.4.5-1.4.6)
    // ============================================================
    /// <summary>Có thể dừng chiếu đèn (bili < ngưỡng chiếu đèn - 50 µmol/L)</summary>
    public bool CoTheDungChieuDen { get; init; }

    /// <summary>Cần kiểm tra rebound 12-18h sau dừng chiếu đèn</summary>
    public bool CanKiemTraRebound { get; init; }

    // ============================================================
    // NICE CG98: Lịch đo lặp thông minh (§1.4.1-1.4.4)
    // ============================================================
    /// <summary>Mô tả lịch đo lặp theo NICE</summary>
    public string? LichDoLapNICE { get; init; }

    /// <summary>Số giờ đến lần đo tiếp theo theo NICE</summary>
    public int? GioDoLapTiepTheo { get; init; }

    // ============================================================
    // Chú thích nguồn tham chiếu
    // ============================================================
    /// <summary>Danh sách các chú thích nguồn tham chiếu cho kết quả</summary>
    public List<string> ChuThichThamChieu { get; init; } = new();
}
