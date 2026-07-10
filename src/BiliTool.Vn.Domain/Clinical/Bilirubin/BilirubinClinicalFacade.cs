using BiliTool.Vn.Domain.Enums;
using BiliTool.Vn.Domain.Services;
using BiliTool.Vn.Domain.ValueObjects;

namespace BiliTool.Vn.Domain.Clinical.Bilirubin;

public class BilirubinClinicalFacade : IBilirubinClinicalFacade
{
    private readonly IMayTinhBilirubin _mayTinh;

    public BilirubinClinicalFacade(IMayTinhBilirubin mayTinh)
    {
        _mayTinh = mayTinh;
    }

    public BilirubinClinicalResult TinhToan(
        double tuoiGio,
        decimal bilirubinMgDl,
        int tuoiThaiTuan,
        YeuToNguyCoThanKinh yeuToNguyCo,
        TrangThaiChieuDen trangThaiChieuDen = TrangThaiChieuDen.KhongChieuDen,
        decimal? tocDoTangBili = null)
    {
        var ketQua = _mayTinh.TinhToan(
            tuoiGio,
            bilirubinMgDl,
            tuoiThaiTuan,
            yeuToNguyCo,
            trangThaiChieuDen,
            tocDoTangBili);

        var trace = new BilirubinCalculationTrace
        {
            TuoiGio = ketQua.TuoiGio,
            TuoiThaiTuan = ketQua.TuoiThaiTuan,
            CoNguyCoThanKinh = ketQua.CoNguyCoThanKinh,
            TrangThaiChieuDen = trangThaiChieuDen,
            PhacDoQuyetDinh = ketQua.PhacDoQuyetDinh,
            BilirubinMgDl = ketQua.BilirubinMgDl,
            BilirubinUmolL = ketQua.BilirubinUmolL,
            NguongChieuDenMgDl = ketQua.NguongChieuDen,
            NguongChieuDenTichCucMgDl = ketQua.NguongChieuDenTichCuc,
            NguongThayCuuMauMgDl = ketQua.NguongThayCuuMau,
            NguongChieuDenNiceUmolL = ketQua.NguongChieuDen_NICE_UmolL,
            NguongThayCuuMauNiceUmolL = ketQua.NguongThayCuuMau_NICE_UmolL,
        };

        return new BilirubinClinicalResult(ketQua, trace);
    }
}
