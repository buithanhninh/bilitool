using BiliTool.Vn.Domain.Enums;

namespace BiliTool.Vn.Domain.Clinical.Bilirubin;

public record BilirubinCalculationTrace
{
    public string GuidelineCode { get; init; } = "AAP2022+NICECG98";
    public string EngineMode { get; init; } = "BaselineMayTinhBilirubin";
    public double TuoiGio { get; init; }
    public int TuoiThaiTuan { get; init; }
    public bool CoNguyCoThanKinh { get; init; }
    public TrangThaiChieuDen TrangThaiChieuDen { get; init; }
    public PhacDo PhacDoQuyetDinh { get; init; }
    public decimal BilirubinMgDl { get; init; }
    public decimal BilirubinUmolL { get; init; }
    public decimal NguongChieuDenMgDl { get; init; }
    public decimal NguongChieuDenTichCucMgDl { get; init; }
    public decimal NguongThayCuuMauMgDl { get; init; }
    public decimal NguongChieuDenNiceUmolL { get; init; }
    public decimal NguongThayCuuMauNiceUmolL { get; init; }
}
