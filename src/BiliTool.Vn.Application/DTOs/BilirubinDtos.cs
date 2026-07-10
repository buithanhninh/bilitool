using BiliTool.Vn.Domain.Enums;

namespace BiliTool.Vn.Application.DTOs;

// ============================================================
// DTO đầu vào
// ============================================================

/// <summary>Yêu cầu tính toán bilirubin từ UI hoặc API</summary>
public class YeuCauTinhToanBilirubinDto
{
    /// <summary>Ngày sinh của trẻ (tuỳ chọn nếu dùng tuổi tính bằng giờ)</summary>
    public DateTime? NgaySinh { get; set; }
    public TimeSpan? GioSinh { get; set; }

    /// <summary>Thời điểm lấy mẫu (tuỳ chọn nếu dùng tuổi tính bằng giờ)</summary>
    public DateTime? NgayLayMau { get; set; }
    public TimeSpan? GioLayMau { get; set; }

    /// <summary>Tuổi trẻ khi lấy mẫu theo giờ (nếu không có ngày sinh)</summary>
    public double? TuoiTheoGio { get; set; }

    /// <summary>Giá trị bilirubin toàn phần</summary>
    public decimal TongBilirubin { get; set; }

    /// <summary>Đơn vị đo (MgDl hoặc UmolL)</summary>
    public DonViDo DonViDo { get; set; } = DonViDo.UmolL;

    /// <summary>Tuổi thai (tuần, phải >= 35)</summary>
    public int TuoiThaiTuan { get; set; }

    /// <summary>Các yếu tố nguy cơ thần kinh + lâm sàng</summary>
    public YeuToNguyCoThanKinhDto YeuToNguyCo { get; set; } = new();

    /// <summary>Trạng thái chiếu đèn hiện tại (NICE §1.4.5)</summary>
    public TrangThaiChieuDen TrangThaiChieuDen { get; set; } = TrangThaiChieuDen.KhongChieuDen;
}

public class YeuToNguyCoThanKinhDto
{
    // AAP 2022 Neurotoxicity Risk Factors
    public bool BenhTanHuyetMienDichHoacThieuG6PD { get; set; }
    public bool NhiemKhuanHuyetHoacNghiNgo { get; set; }
    public bool AlbuminThapDuoi3gDl { get; set; }
    public decimal? ETCOcPpm { get; set; }
    public bool TinhTrangLamSangKhongOnDinh { get; set; }

    // NICE CG98 Clinical Risk Factors (§1.2.1)
    public bool AnhChiBiVangDaCanChieuDen { get; set; }
    public bool MeBuMeHoanToan { get; set; }
    public bool VangDaTrong24hDau { get; set; }

    // NICE CG98 IVIG/Kernicterus (§1.8.1, §1.5.1)
    public bool BenhTanHuyetRh { get; set; }
    public bool BenhTanHuyetABO { get; set; }
    public bool DauHieuBenhNaoBilirubinCap { get; set; }
}

/// <summary>Yêu cầu thêm mẫu bilirubin vào phiên xu hướng</summary>
public class ThemMauBilirubinDto
{
    public Guid PhienId { get; set; }
    public DateTime ThoiGianLayMau { get; set; }
    public decimal BilirubinMgDl { get; set; }
    public double TuoiGioKhiLayMau { get; set; }
}

// ============================================================
// DTO đầu ra
// ============================================================

/// <summary>Kết quả tính toán bilirubin trả về cho UI/API</summary>
public class KetQuaTinhToanDto
{
    // Đầu vào phản hồi
    public double TuoiGio { get; set; }
    public int TuoiThaiTuan { get; set; }
    public decimal BilirubinMgDl { get; set; }
    public decimal BilirubinUmolL { get; set; }
    public bool CoNguyCoThanKinh { get; set; }

    // Ngưỡng AAP 2022 (mg/dL)
    public decimal NguongChieuDen { get; set; }
    public decimal NguongChieuDenTichCuc { get; set; }
    public decimal NguongThayCuuMau { get; set; }
    public decimal KhoangCachDenNguongChieuDen { get; set; }
    public decimal KhoangCachDenNguongThayCuuMau { get; set; }

    // Ngưỡng NICE CG98
    public decimal NguongChieuDen_NICE_UmolL { get; set; }
    public decimal NguongThayCuuMau_NICE_UmolL { get; set; }
    public decimal NguongChieuDen_NICE_MgDl { get; set; }
    public decimal NguongThayCuuMau_NICE_MgDl { get; set; }
    public decimal KhoangCachDenNguongChieuDen_NICE { get; set; }
    public decimal KhoangCachDenNguongThayCuuMau_NICE { get; set; }

    // Trạng thái kết hợp
    public bool CanChieuDenNgay { get; set; }
    public bool CanChieuDenTichCuc { get; set; }
    public bool CanXemXetThayCuuMau { get; set; }
    public MucDoNguyHiem MucDoNguyHiemEnum { get; set; }
    public string MauNguyHiem { get; set; } = "#27ae60";
    public ThoiGianTaiKham ThoiGianTaiKhamEnum { get; set; }
    public PhacDo PhacDoQuyetDinh { get; set; }

    // NICE extras
    public bool NguyCoKernicterus { get; set; }
    public List<string> LyDoNguyCoKernicterus { get; set; } = new();
    public bool LaVangDaKeoDai { get; set; }
    public string? CanhBaoVangDaKeoDai { get; set; }
    public bool CanIVIG { get; set; }
    public string? MoTaIVIG { get; set; }
    public bool CoTheDungChieuDen { get; set; }
    public bool CanKiemTraRebound { get; set; }
    public string? LichDoLapNICE { get; set; }
    public int? GioDoLapTiepTheo { get; set; }

    // Chú thích nguồn tham chiếu
    public List<string> ChuThichThamChieu { get; set; } = new();
    public DateTime ThoiGianTinhToan { get; set; }
    
    // Dữ liệu vẽ biểu đồ nomogram
    public List<ChartDataPoint> ChartData { get; set; } = new();
}

public class ChartDataPoint
{
    public double Hour { get; set; }
    public decimal Phototherapy { get; set; }
    public decimal Escalation { get; set; }
    public decimal Exchange { get; set; }
}

/// <summary>Kết quả phân tích xu hướng bilirubin</summary>
public class KetQuaXuHuongDto
{
    public List<DiemXuHuongDto> CacDiem { get; set; } = new();
    public decimal TocDoTrungBinhMgDlMoiGio { get; set; }
    public bool LaTangNhanh { get; set; }
    public string ThongBaoXuHuong { get; set; } = string.Empty;
    public string MauCanhBao { get; set; } = "#27ae60";
}

public class DiemXuHuongDto
{
    public DateTime ThoiGianLayMau { get; set; }
    public double TuoiGio { get; set; }
    public decimal BilirubinMgDl { get; set; }
    public decimal? TocDoThayDoi { get; set; }
}
