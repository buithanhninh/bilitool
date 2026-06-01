namespace BiliTool.Vn.Domain.Entities;

/// <summary>
/// Một lần đo xét nghiệm Bilirubin của bệnh nhân.
/// Tự động tính "Giờ tuổi" dựa trên mốc sinh của bệnh nhân.
/// </summary>
public class XetNghiemBilirubin
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Liên kết về hồ sơ bệnh nhân</summary>
    public Guid BenhNhanId { get; set; }
    public HoSoBenhNhan BenhNhan { get; set; } = null!;

    /// <summary>
    /// Thời gian lấy mẫu thực tế.
    /// Mặc định = DateTime.UtcNow (thời gian bác sĩ nhập),
    /// nhưng bác sĩ có thể điền ngược nếu kết quả trả về muộn.
    /// </summary>
    public DateTime ThoiGianLayMau { get; set; }

    /// <summary>Giá trị Bilirubin (mg/dL)</summary>
    public decimal BilirubinMgDl { get; set; }

    /// <summary>Giờ tuổi tự động tính: (ThoiGianLayMau - NgayGioSinh).TotalHours</summary>
    public double TuoiGioTuDong { get; set; }

    /// <summary>Mức độ nguy hiểm tại lần đo này (lưu để hiển thị lịch sử)</summary>
    public string? MucDoNguyHiem { get; set; }

    /// <summary>Ngưỡng chiếu đèn tại lần đo này</summary>
    public decimal? NguongChieuDen { get; set; }

    /// <summary>Ngưỡng thay máu tại lần đo này</summary>  
    public decimal? NguongThayCuuMau { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}
