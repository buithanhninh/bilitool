namespace BiliTool.Vn.Application.DTOs;

// ─── Existing DTOs ───────────────────────────────────────────────────────────

public class ThongKeHeThongDto
{
    public int TongNguoiDung { get; set; }
    public int NguoiDungMoiTrongThang { get; set; }
    public int TongBenhNhan { get; set; }
    public int BenhNhanMoiTrongThang { get; set; }
    public int TongPhienTinhToan { get; set; }
    public int PhienTinhToanTrongThang { get; set; }
}

public class TaiKhoanAdminDto
{
    public string Id { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ChuyenKhoa { get; set; }
    public string? DonViCongTac { get; set; }
    public string? SoDienThoai { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayDangNhapCuoi { get; set; }
    public bool IsActive { get; set; }
    public int TongBenhNhan { get; set; }
    public int TongPhienTinhToan { get; set; }
    public int TinhToanTrong30NgayQua { get; set; }
}

public class BenhNhanAdminDto
{
    public Guid Id { get; set; }
    public string HoTenBenhNhan { get; set; } = string.Empty;
    public DateTime NgayGioSinh { get; set; }
    public int TuoiThaiTuan { get; set; }
    public bool CoNguyCoThanKinh { get; set; }
    public DateTime NgayTao { get; set; }
    public string TenBacSi { get; set; } = string.Empty;
    public string EmailBacSi { get; set; } = string.Empty;
    public int SoLanXetNghiem { get; set; }
}

public class ThongKeTheoNgayDto
{
    public string Ngay { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public double GiaTri { get; set; }  // For float metrics like avg bilirubin
}

public class ThongKeBieuDoAdminDto
{
    public List<ThongKeTheoNgayDto> TaiKhoanMoi30Ngay { get; set; } = new();
    public List<ThongKeTheoNgayDto> BenhNhanMoi30Ngay { get; set; } = new();
    public List<ThongKeTheoNgayDto> LuotTinhToan30Ngay { get; set; } = new();
    public int TiLeBinhThuong { get; set; }
    public int TiLeChieuDen { get; set; }
    public int TiLeThayMau { get; set; }
}

public class TaiKhoanDetailAdminDto
{
    public TaiKhoanAdminDto ThongTinChung { get; set; } = new();
    public List<BenhNhanAdminDto> DanhSachBenhNhan { get; set; } = new();
    public List<ThongKeTheoNgayDto> HoatDong30Ngay { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════════════════
// NHÓM 1 — Thống kê Bác sĩ (14 chỉ số)
// ═══════════════════════════════════════════════════════════════════════════

public class PhanBoNhomDto
{
    public string NhanNhom { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public double PhanTram { get; set; }
}

public class LeaderboardBacSiDto
{
    public string Id { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ChuyenKhoa { get; set; }
    public string? DonViCongTac { get; set; }
    public int SoBenhNhan { get; set; }
    public int LuotTinhToan30Ngay { get; set; }
    public int TongLuotTinhToan { get; set; }
    public bool CoNguoiBenhNhiNguyCoThanKinh { get; set; }
    public DateTime NgayDangNhapCuoi { get; set; }
}

public class ThongKeBacSiAdminDto
{
    // KPI Tổng quan
    public int TongBacSi { get; set; }
    public int BacSiMoiThangNay { get; set; }
    public int BacSiMoiThangTruoc { get; set; }
    public int BacSiHoatDong7Ngay { get; set; }
    public int BacSiHoatDong30Ngay { get; set; }
    public int BacSiInactive30Ngay { get; set; }          // Silent Churn
    public double AvgBenhNhanPerBacSi { get; set; }
    public double AvgLuotTinhToanPerBacSiPerThang { get; set; }
    public int BacSiCoBenhNhiNguyCoThanKinh { get; set; }
    public double TiLeTinhToanVuotNguong { get; set; }    // % tính toán ≠ Bình thường
    public double AvgNgayDenBenhNhanDauTien { get; set; } // Time-to-Value (ngày)
    public int BacSiDuHoSo { get; set; }                  // có đủ SĐT + DonVi

    // Phân bố charts
    public List<PhanBoNhomDto> PhanBoChuyenKhoa { get; set; } = new();
    public List<PhanBoNhomDto> TopDonViCongTac { get; set; } = new();
    public List<PhanBoNhomDto> PhanBoCuongDoSuDung { get; set; } = new();  // 0, 1-5, 6-20, >20
    public List<ThongKeTheoNgayDto> OnboardingTuanQua { get; set; } = new(); // 12 tuần
    public List<LeaderboardBacSiDto> Top10BacSiHoatDong { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════════════════
// NHÓM 2 — Thống kê Bệnh nhi & Lâm sàng (23 chỉ số)
// ═══════════════════════════════════════════════════════════════════════════

public class TopBenhNhiNguyCoDto
{
    public Guid Id { get; set; }
    public string HoTenBenhNhan { get; set; } = string.Empty;
    public string TenBacSi { get; set; } = string.Empty;
    public string EmailBacSi { get; set; } = string.Empty;
    public int TuoiThaiTuan { get; set; }
    public bool CoNguyCoThanKinh { get; set; }
    public decimal BilirubinMax { get; set; }
    public int SoLanXetNghiem { get; set; }
    public int SoLanVuotNguongThayMau { get; set; }
    public DateTime NgayGioSinh { get; set; }
}

public class ThongKeBenhNhanAdminDto
{
    // 2.1 — Dân số học (Epidemiology)
    public int TongBenhNhi { get; set; }
    public int BenhNhiMoiThangNay { get; set; }
    public int BenhNhiMoiThangTruoc { get; set; }
    public int TongCoNguyCoThanKinh { get; set; }
    public double TiLeNguyCoThanKinh { get; set; }
    public int TongSinhNon { get; set; }               // GA < 37 tuần
    public double TiLeSinhNon { get; set; }
    public int TongCucKyNon { get; set; }              // GA < 28 tuần

    // 2.2 — Biochemical Analytics
    public double BilirubinDinhTrungBinh { get; set; }
    public double BilirubinDinhStdDev { get; set; }
    public double TiLeBilirubinNang { get; set; }      // % > 20 mg/dL
    public double AvgTocDoTangBilirubin { get; set; }  // mg/dL/h

    // 2.3 — AAP Outcome Analysis
    public int TongXetNghiem { get; set; }
    public double AvgLanDoPerBenhNhi { get; set; }
    public int TongPhotoTherapy { get; set; }
    public int TongExchangeTransfusion { get; set; }
    public double TiLePhotoTherapy { get; set; }
    public double TiLeExchangeTransfusion { get; set; }

    // 2.4 — Safety Indicators
    public int BenhNhiLostFollowUp { get; set; }       // chỉ 1 lần đo
    public double TiLeLostFollowUp { get; set; }
    public int BenhNhiCritical { get; set; }            // GA<35 + CoNguyCoTK
    public double HeSoKhaiThacHeThong { get; set; }    // TongXN / TongBN

    // Charts data
    public List<PhanBoNhomDto> PhanBoTuoiThai { get; set; } = new();
    public List<PhanBoNhomDto> PhanBoBilirubinDinh { get; set; } = new();
    public List<PhanBoNhomDto> PhanBoGioTuoiLanDoDau { get; set; } = new();
    public List<PhanBoNhomDto> PhanBoChiDinhAAP { get; set; } = new();
    public List<ThongKeTheoNgayDto> BilirubinTrungBinh30Ngay { get; set; } = new();
    public List<ThongKeTheoNgayDto> XetNghiem30Ngay { get; set; } = new();
    public int[] HeatmapGioTrongNgay { get; set; } = new int[24];
    public List<TopBenhNhiNguyCoDto> Top5BenhNhiNguyCoNhat { get; set; } = new();
}
