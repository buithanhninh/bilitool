using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Domain.Enums;

namespace BiliTool.Vn.Application.Clinical;

public static class HisClinicalRequestMapper
{
    public static YeuCauTinhToanBilirubinDto Map(HisClinicalCalculationRequest request)
    {
        var status = request.Patient.PhototherapyStatus switch
        {
            "none" => TrangThaiChieuDen.KhongChieuDen,
            "phototherapy" => TrangThaiChieuDen.DangChieuDen,
            "intensive-phototherapy" => TrangThaiChieuDen.DangChieuDenTichCuc,
            "stopped" => TrangThaiChieuDen.DaDungChieuDen,
            _ => throw new InvalidOperationException("Trạng thái chiếu đèn chưa được validate.")
        };

        return new YeuCauTinhToanBilirubinDto
        {
            TuoiTheoGio = request.Patient.AgeHours,
            NgaySinh = request.Patient.BirthTime?.UtcDateTime,
            NgayLayMau = request.Observation.EffectiveAt.UtcDateTime,
            TongBilirubin = request.Observation.Value,
            DonViDo = request.Observation.Unit == "mg/dL" ? DonViDo.MgDl : DonViDo.UmolL,
            TuoiThaiTuan = request.Patient.GestationalAgeWeeks,
            TrangThaiChieuDen = status,
            YeuToNguyCo = MapRiskFactors(request.RiskFactors),
            IntegrationContext = new HisIntegrationContextDto
            {
                SourceSystem = request.Source.System,
                Facility = request.Source.Facility,
                MessageId = request.Source.MessageId,
                PatientIdentifier = request.Patient.Identifier,
                AssigningAuthority = request.Patient.AssigningAuthority,
                EncounterIdentifier = request.Encounter.Identifier,
                OrderIdentifier = request.Order.Identifier,
                SpecimenIdentifier = request.Specimen.Identifier,
                ObservationIdentifier = request.Observation.Identifier
            }
        };
    }

    private static YeuToNguyCoThanKinhDto MapRiskFactors(HisRiskFactorsDto source) => new()
    {
        BenhTanHuyetMienDichHoacThieuG6PD = source.BenhTanHuyetMienDichHoacThieuG6PD,
        NhiemKhuanHuyetHoacNghiNgo = source.NhiemKhuanHuyetHoacNghiNgo,
        AlbuminThapDuoi3gDl = source.AlbuminThapDuoi3gDl,
        ETCOcPpm = source.ETCOcPpm,
        TinhTrangLamSangKhongOnDinh = source.TinhTrangLamSangKhongOnDinh,
        AnhChiBiVangDaCanChieuDen = source.AnhChiBiVangDaCanChieuDen,
        MeBuMeHoanToan = source.MeBuMeHoanToan,
        VangDaTrong24hDau = source.VangDaTrong24hDau,
        BenhTanHuyetRh = source.BenhTanHuyetRh,
        BenhTanHuyetABO = source.BenhTanHuyetABO,
        DauHieuBenhNaoBilirubinCap = source.DauHieuBenhNaoBilirubinCap
    };
}
