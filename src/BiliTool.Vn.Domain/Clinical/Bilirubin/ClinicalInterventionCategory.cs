namespace BiliTool.Vn.Domain.Clinical.Bilirubin;

public enum ClinicalInterventionCategory
{
    KhongDuDuLieu = 0,
    BinhThuong = 1,
    ChieuDen = 2,
    EscalationOfCare = 3,
    ThayMau = 4
}

public static class ClinicalInterventionClassifier
{
    private const decimal AapEscalationMarginMgDl = 2m;

    public static ClinicalInterventionCategory Classify(
        decimal bilirubinMgDl,
        decimal? phototherapyThresholdMgDl,
        decimal? exchangeThresholdMgDl)
    {
        if (bilirubinMgDl < 0 || phototherapyThresholdMgDl is null || exchangeThresholdMgDl is null)
            return ClinicalInterventionCategory.KhongDuDuLieu;

        if (exchangeThresholdMgDl <= phototherapyThresholdMgDl)
            return ClinicalInterventionCategory.KhongDuDuLieu;

        if (bilirubinMgDl >= exchangeThresholdMgDl)
            return ClinicalInterventionCategory.ThayMau;

        if (bilirubinMgDl >= exchangeThresholdMgDl - AapEscalationMarginMgDl)
            return ClinicalInterventionCategory.EscalationOfCare;

        if (bilirubinMgDl >= phototherapyThresholdMgDl)
            return ClinicalInterventionCategory.ChieuDen;

        return ClinicalInterventionCategory.BinhThuong;
    }
}
