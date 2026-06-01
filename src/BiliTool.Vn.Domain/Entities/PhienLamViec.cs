using BiliTool.Vn.Domain.Enums;

namespace BiliTool.Vn.Domain.Entities;

/// <summary>
/// Phiên làm việc — ghi nhận mỗi lần người dùng truy cập máy tính
/// (ẩn danh hoặc đã đăng nhập)
/// </summary>
public class PhienLamViec
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime NgayTao { get; private set; } = DateTime.UtcNow;
    public string? DiaChiIP { get; private set; }
    public string? ThietBi { get; private set; }

    /// <summary>ID người dùng đã đăng nhập (null nếu ẩn danh)</summary>
    public string? NguoiDungId { get; private set; }

    public bool LaAnDanh => NguoiDungId == null;

    /// <summary>Danh sách kết quả tính toán trong phiên này</summary>
    public ICollection<LichSuTinhToan> LichSuTinhToan { get; private set; } = new List<LichSuTinhToan>();

    /// <summary>Danh sách lần lấy mẫu bilirubin (cho tính năng xu hướng)</summary>
    public ICollection<MauBilirubinLuuTru> MauBilirubin { get; private set; } = new List<MauBilirubinLuuTru>();

    public static PhienLamViec TaoAnDanh(string? diaChiIP = null, string? thietBi = null)
        => new() { DiaChiIP = diaChiIP, ThietBi = thietBi };

    public static PhienLamViec TaoDaDangNhap(string nguoiDungId, string? diaChiIP = null, string? thietBi = null)
        => new() { NguoiDungId = nguoiDungId, DiaChiIP = diaChiIP, ThietBi = thietBi };
}

/// <summary>
/// Lịch sử một lần tính toán bilirubin trong một phiên
/// </summary>
public class LichSuTinhToan
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PhienId { get; set; }

    // Dữ liệu đầu vào
    public int TuoiGio { get; set; }
    public int TuoiThaiTuan { get; set; }
    public decimal BilirubinMgDl { get; set; }
    public bool CoNguyCoThanKinh { get; set; }

    // Kết quả
    public decimal NguongChieuDen { get; set; }
    public decimal NguongChieuDenTichCuc { get; set; }
    public decimal NguongThayCuuMau { get; set; }
    public string MucDoNguyHiem { get; set; } = string.Empty;
    public string KhuyenNghiChinh { get; set; } = string.Empty;

    /// <summary>Lưu trữ toàn bộ payload request chi tiết dưới dạng JSON</summary>
    public string ChiTietYeuCauJson { get; set; } = string.Empty;

    public DateTime NgayTinhToan { get; set; } = DateTime.UtcNow;

    // Navigation property
    public PhienLamViec? Phien { get; set; }
}

/// <summary>
/// Mẫu bilirubin được lưu trữ cho tính năng theo dõi xu hướng
/// </summary>
public class MauBilirubinLuuTru
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PhienId { get; set; }
    public int ThuTu { get; set; }
    public DateTime ThoiGianLayMau { get; set; }
    public decimal BilirubinMgDl { get; set; }
    public int TuoiGioKhiLayMau { get; set; }
    public decimal? TocDoThayDoi { get; set; } // mg/dL/giờ

    // Navigation property
    public PhienLamViec? Phien { get; set; }
}
