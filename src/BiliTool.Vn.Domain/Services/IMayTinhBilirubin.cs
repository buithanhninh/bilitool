using BiliTool.Vn.Domain.Enums;
using BiliTool.Vn.Domain.ValueObjects;

namespace BiliTool.Vn.Domain.Services;

/// <summary>Interface cho engine tính toán ngưỡng bilirubin</summary>
public interface IMayTinhBilirubin
{
    /// <summary>
    /// Tính toán đánh giá bilirubin kết hợp AAP 2022 + NICE CG98
    /// </summary>
    /// <param name="tuoiGio">Tuổi trẻ khi lấy mẫu (tính bằng giờ kiểu phần thập phân, 0-672)</param>
    /// <param name="bilirubinMgDl">Nồng độ bilirubin toàn phần (mg/dL)</param>
    /// <param name="tuoiThaiTuan">Tuổi thai (tuần, ≥35)</param>
    /// <param name="yeuToNguyCo">Các yếu tố nguy cơ thần kinh + lâm sàng</param>
    /// <param name="trangThaiChieuDen">Trạng thái chiếu đèn hiện tại (để tính lịch đo/dừng)</param>
    /// <param name="tocDoTangBili">Tốc độ tăng bilirubin (mg/dL/h) - nếu có dữ liệu xu hướng</param>
    KetQuaTinhToanBilirubin TinhToan(
        double tuoiGio,
        decimal bilirubinMgDl,
        int tuoiThaiTuan,
        YeuToNguyCoThanKinh yeuToNguyCo,
        TrangThaiChieuDen trangThaiChieuDen = TrangThaiChieuDen.KhongChieuDen,
        decimal? tocDoTangBili = null);

    /// <summary>Chuyển đổi giá trị bilirubin về mg/dL</summary>
    decimal ChuyenDoiSangMgDl(decimal giaTri, DonViDo donViDo);

    /// <summary>Chuyển đổi mg/dL sang μmol/L</summary>
    decimal ChuyenDoiSangUmolL(decimal mgDl);

    /// <summary>Tính tốc độ thay đổi bilirubin theo thời gian</summary>
    TocDoThayDoiBilirubin TinhTocDoThayDoi(IList<MauBilirubinTheoThoiGian> lichSuMau);
}
