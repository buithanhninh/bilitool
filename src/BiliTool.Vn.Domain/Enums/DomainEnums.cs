namespace BiliTool.Vn.Domain.Enums;

/// <summary>Đơn vị đo bilirubin</summary>
public enum DonViDo
{
    /// <summary>Milligrams per deciliter (hệ Mỹ)</summary>
    MgDl,
    /// <summary>Micromoles per liter (hệ quốc tế)</summary>
    UmolL
}

/// <summary>Mức độ nguy hiểm của kết quả</summary>
public enum MucDoNguyHiem
{
    /// <summary>Bình thường - không cần can thiệp</summary>
    BinhThuong,
    /// <summary>Cần theo dõi sát</summary>
    CanTheoDoiSat,
    /// <summary>Cần chiếu đèn</summary>
    CanChieuDen,
    /// <summary>Cần chiếu đèn tích cực</summary>
    CanChieuDenTichCuc,
    /// <summary>Cần xem xét thay máu</summary>
    CanXemXetThayMau,
    /// <summary>Khẩn cấp - cần thay máu ngay</summary>
    KhanCapThayCuuMau
}

/// <summary>Khuyến nghị tái khám</summary>
public enum ThoiGianTaiKham
{
    KhongCan,
    TrongVong2Gio,
    TrongVong4Gio,
    TrongVong6Gio,
    TrongVong8Gio,
    TrongVong12Gio,
    TrongVong18Gio,
    TrongVong24Gio,
    TrongVong48Gio,
    TrongVong72Gio
}

/// <summary>Phác đồ tham chiếu</summary>
public enum PhacDo
{
    /// <summary>AAP 2022 - Pediatrics 150(3):e2022058859</summary>
    AAP2022,
    /// <summary>NICE CG98 - Jaundice in newborn babies under 28 days</summary>
    NICE_CG98
}

/// <summary>Trạng thái chiếu đèn hiện tại của bệnh nhân</summary>
public enum TrangThaiChieuDen
{
    KhongChieuDen,
    DangChieuDen,
    DangChieuDenTichCuc,
    DaDungChieuDen
}
