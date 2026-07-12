namespace BiliTool.Vn.Domain.Entities;

/// <summary>
/// Hồ sơ bệnh nhân nhi - được tạo và quản lý bởi bác sĩ đã đăng nhập.
/// Một bác sĩ có thể có nhiều bệnh nhân.
/// </summary>
public class HoSoBenhNhan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Mã định danh bệnh nhi, tự động tạo nếu bỏ trống</summary>
    public string MaBenhNhan { get; set; } = string.Empty;

    /// <summary>Google ID (email/sub) của bác sĩ quản lý bệnh nhân này</summary>
    public string NguoiDungId { get; set; } = string.Empty;

    /// <summary>Họ và tên đầy đủ của bệnh nhân</summary>
    public string HoTenBenhNhan { get; set; } = string.Empty;

    /// <summary>Ngày giờ sinh của bệnh nhân — mốc gốc tính "Giờ tuổi" cho mọi xét nghiệm</summary>
    public DateTime NgayGioSinh { get; set; }

    /// <summary>Tuổi thai lúc sinh (tuần)</summary>
    public int TuoiThaiTuan { get; set; }

    /// <summary>Có yếu tố nguy cơ thần kinh hay không (lưu lâu dài)</summary>
    public bool CoNguyCoThanKinh { get; set; }

    // ============================================================
    // YẾU TỐ NGUY CƠ LÂM SÀNG — NICE CG98 (§1.2.1)
    // ============================================================

    /// <summary>Anh/chị ruột từng bị vàng da cần chiếu đèn (NICE 1.2.1)</summary>
    public bool AnhChiBiVangDaCanChieuDen { get; set; }

    /// <summary>Mẹ dự định bú mẹ hoàn toàn (NICE 1.2.1)</summary>
    public bool MeBuMeHoanToan { get; set; }

    /// <summary>Vàng da xuất hiện trong 24h đầu sau sinh (NICE 1.2.1)</summary>
    public bool VangDaTrong24hDau { get; set; }

    /// <summary>Bệnh tan huyết Rh (NICE 1.8.1)</summary>
    public bool BenhTanHuyetRh { get; set; }

    /// <summary>Bệnh tan huyết ABO (NICE 1.8.1)</summary>
    public bool BenhTanHuyetABO { get; set; }

    /// <summary>Ghi chú thêm (tùy chọn)</summary>
    public string? GhiChu { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    /// <summary>Dữ liệu kiểm thử/automation, bị loại khỏi thống kê production.</summary>
    public bool IsTestData { get; set; }

    /// <summary>Danh sách xét nghiệm Bilirubin của bệnh nhân này</summary>
    public List<XetNghiemBilirubin> DsXetNghiem { get; set; } = new();

    // ============================================================
    // COMPUTED PROPERTIES — NICE CG98 (§1.7)
    // ============================================================

    /// <summary>
    /// Kiểm tra vàng da kéo dài theo NICE CG98 §1.7.1:
    /// - GA ≥ 37 tuần: vàng da > 14 ngày
    /// - GA &lt; 37 tuần: vàng da > 21 ngày
    /// </summary>
    public bool LaVangDaKeoDai
    {
        get
        {
            double soNgayTuoi = (DateTime.UtcNow - NgayGioSinh).TotalDays;
            return TuoiThaiTuan >= 37 ? soNgayTuoi > 14 : soNgayTuoi > 21;
        }
    }

    /// <summary>Có yếu tố nguy cơ lâm sàng NICE (sibling + breastfeeding)</summary>
    public bool CoNguyCoLamSangNICE =>
        AnhChiBiVangDaCanChieuDen || MeBuMeHoanToan;
}
