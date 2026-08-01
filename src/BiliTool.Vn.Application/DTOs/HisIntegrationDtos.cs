using System.Text.Json.Serialization;

namespace BiliTool.Vn.Application.DTOs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HisClinicalCalculationRequest
{
    public HisSourceSystemDto Source { get; set; } = new();
    public HisPatientContextDto Patient { get; set; } = new();
    public HisEncounterReferenceDto Encounter { get; set; } = new();
    public HisOrderReferenceDto Order { get; set; } = new();
    public HisSpecimenReferenceDto Specimen { get; set; } = new();
    public HisBilirubinObservationDto Observation { get; set; } = new();
    public HisRiskFactorsDto RiskFactors { get; set; } = new();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HisSourceSystemDto
{
    public string System { get; set; } = string.Empty;
    public string Facility { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HisPatientContextDto
{
    public string Identifier { get; set; } = string.Empty;
    public string AssigningAuthority { get; set; } = string.Empty;
    public DateTimeOffset? BirthTime { get; set; }
    public double? AgeHours { get; set; }
    public int GestationalAgeWeeks { get; set; }
    public string PhototherapyStatus { get; set; } = "none";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HisEncounterReferenceDto
{
    public string Identifier { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HisOrderReferenceDto
{
    public string Identifier { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HisSpecimenReferenceDto
{
    public string Identifier { get; set; } = string.Empty;
    public DateTimeOffset CollectedAt { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HisBilirubinObservationDto
{
    public string Identifier { get; set; } = string.Empty;
    public DateTimeOffset EffectiveAt { get; set; }
    public decimal Value { get; set; }
    public string Unit { get; set; } = "umol/L";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HisRiskFactorsDto
{
    public bool BenhTanHuyetMienDichHoacThieuG6PD { get; set; }
    public bool NhiemKhuanHuyetHoacNghiNgo { get; set; }
    public bool AlbuminThapDuoi3gDl { get; set; }
    public decimal? ETCOcPpm { get; set; }
    public bool TinhTrangLamSangKhongOnDinh { get; set; }
    public bool AnhChiBiVangDaCanChieuDen { get; set; }
    public bool MeBuMeHoanToan { get; set; }
    public bool VangDaTrong24hDau { get; set; }
    public bool BenhTanHuyetRh { get; set; }
    public bool BenhTanHuyetABO { get; set; }
    public bool DauHieuBenhNaoBilirubinCap { get; set; }
}

public sealed class HisIntegrationContextDto
{
    public string SourceSystem { get; set; } = string.Empty;
    public string Facility { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string PatientIdentifier { get; set; } = string.Empty;
    public string AssigningAuthority { get; set; } = string.Empty;
    public string EncounterIdentifier { get; set; } = string.Empty;
    public string OrderIdentifier { get; set; } = string.Empty;
    public string SpecimenIdentifier { get; set; } = string.Empty;
    public string ObservationIdentifier { get; set; } = string.Empty;
}
