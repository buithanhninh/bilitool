using BiliTool.Vn.Domain.Enums;
using BiliTool.Vn.Domain.ValueObjects;

namespace BiliTool.Vn.Domain.Clinical.Bilirubin;

public interface IBilirubinClinicalFacade
{
    BilirubinClinicalResult TinhToan(
        double tuoiGio,
        decimal bilirubinMgDl,
        int tuoiThaiTuan,
        YeuToNguyCoThanKinh yeuToNguyCo,
        TrangThaiChieuDen trangThaiChieuDen = TrangThaiChieuDen.KhongChieuDen,
        decimal? tocDoTangBili = null);
}
