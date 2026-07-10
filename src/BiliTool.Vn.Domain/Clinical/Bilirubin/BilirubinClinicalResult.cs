using BiliTool.Vn.Domain.ValueObjects;

namespace BiliTool.Vn.Domain.Clinical.Bilirubin;

public record BilirubinClinicalResult(
    KetQuaTinhToanBilirubin KetQua,
    BilirubinCalculationTrace Trace);
