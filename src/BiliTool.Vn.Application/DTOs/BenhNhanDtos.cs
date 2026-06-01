namespace BiliTool.Vn.Application.DTOs;

/// <summary>DTO đại diện một hồ sơ bệnh nhân để hiển thị trên UI</summary>
public class HoSoBenhNhanDto
{
    public Guid Id { get; set; }
    public string MaBenhNhan { get; set; } = string.Empty;
    public string HoTenBenhNhan { get; set; } = string.Empty;
    public DateTime NgayGioSinh { get; set; }
    public int TuoiThaiTuan { get; set; }
    public bool CoNguyCoThanKinh { get; set; }

    // NICE CG98 Clinical Risk Factors
    public bool AnhChiBiVangDaCanChieuDen { get; set; }
    public bool MeBuMeHoanToan { get; set; }
    public bool VangDaTrong24hDau { get; set; }
    public bool BenhTanHuyetRh { get; set; }
    public bool BenhTanHuyetABO { get; set; }

    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }
    public List<XetNghiemBilirubinDto> DsXetNghiem { get; set; } = new();

    /// <summary>Tuổi hiện tại của bé (giờ)</summary>
    public double TuoiHienTaiGio => (DateTime.UtcNow - NgayGioSinh).TotalHours;

    /// <summary>Lần đo gần nhất</summary>
    public XetNghiemBilirubinDto? LanDoGanNhat =>
        DsXetNghiem.OrderByDescending(x => x.ThoiGianLayMau).FirstOrDefault();

    /// <summary>Vàng da kéo dài theo NICE CG98 §1.7.1</summary>
    public bool LaVangDaKeoDai
    {
        get
        {
            double soNgayTuoi = (DateTime.UtcNow - NgayGioSinh).TotalDays;
            return TuoiThaiTuan >= 37 ? soNgayTuoi > 14 : soNgayTuoi > 21;
        }
    }
}

/// <summary>DTO đại diện một lần xét nghiệm Bilirubin</summary>
public class XetNghiemBilirubinDto
{
    public Guid Id { get; set; }
    public Guid BenhNhanId { get; set; }
    public DateTime ThoiGianLayMau { get; set; }
    public decimal BilirubinMgDl { get; set; }
    public double TuoiGioTuDong { get; set; }
    public string? MucDoNguyHiem { get; set; }
    public decimal? NguongChieuDen { get; set; }
    public decimal? NguongThayCuuMau { get; set; }
    public DateTime NgayTao { get; set; }
}

/// <summary>Form tạo mới hồ sơ bệnh nhân</summary>
public class TaoBenhNhanDto
{
    public string HoTenBenhNhan { get; set; } = string.Empty;
    public string? MaBenhNhan { get; set; }
    public string NgayGioSinhStr { get; set; } = string.Empty;  // "dd/MM/yyyy HH:mm"
    public int TuoiThaiTuan { get; set; } = 40;

    // ── Yếu tố nguy cơ thần kinh (AAP 2022) ──
    public bool BenhTanHuyetMienDichHoacThieuG6PD { get; set; }
    public bool NhiemKhuanHuyetHoacNghiNgo { get; set; }
    public bool AlbuminThapDuoi3gDl { get; set; }
    public bool TinhTrangLamSangKhongOnDinh { get; set; }
    public decimal? ETCOcPpm { get; set; }

    // ── Yếu tố nguy cơ lâm sàng (NICE CG98 §1.2.1) ──
    public bool AnhChiBiVangDaCanChieuDen { get; set; }
    public bool MeBuMeHoanToan { get; set; }
    public bool VangDaTrong24hDau { get; set; }
    public bool BenhTanHuyetRh { get; set; }
    public bool BenhTanHuyetABO { get; set; }

    /// <summary>True nếu có ÍT NHẤT một yếu tố nguy cơ AAP</summary>
    public bool CoNguyCoThanKinh =>
        BenhTanHuyetMienDichHoacThieuG6PD ||
        NhiemKhuanHuyetHoacNghiNgo ||
        AlbuminThapDuoi3gDl ||
        TinhTrangLamSangKhongOnDinh ||
        (ETCOcPpm.HasValue && ETCOcPpm.Value > 1.7m);

    public string? GhiChu { get; set; }
}

/// <summary>Form thêm xét nghiệm mới cho bệnh nhân</summary>
public class ThemXetNghiemDto
{
    public Guid BenhNhanId { get; set; }
    public decimal BilirubinMgDl { get; set; }

    /// <summary>Nếu null → dùng DateTime.Now (mặc định). Nếu có giá trị → nhập ngược.</summary>
    public DateTime? ThoiGianLayMauTuyChon { get; set; }
}
