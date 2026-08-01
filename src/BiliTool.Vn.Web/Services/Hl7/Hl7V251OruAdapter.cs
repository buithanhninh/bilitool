using System.Globalization;
using BiliTool.Vn.Application.DTOs;

namespace BiliTool.Vn.Web.Services.Hl7;

public sealed class Hl7V251OruAdapter
{
    public Hl7OruParseResult Parse(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) throw new Hl7ValidationException("HL7 message rỗng.", null);
        var segments = message.Replace("\r\n", "\r", StringComparison.Ordinal)
            .Replace('\n', '\r')
            .Split('\r', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Hl7Segment.Parse)
            .ToArray();

        var msh = RequireSingle(segments, "MSH");
        if (msh.Field(9).Split('^')[0] != "ORU" || msh.Field(9).Split('^').ElementAtOrDefault(1) != "R01")
            throw new Hl7ValidationException("MSH-9 phải là ORU^R01^ORU_R01.", msh.Field(10));
        if (msh.Field(12) != "2.5.1")
            throw new Hl7ValidationException("MSH-12 phải là 2.5.1.", msh.Field(10));

        var messageControlId = RequireValue(msh.Field(10), "MSH-10");
        var pid = RequireSingle(segments, "PID", messageControlId);
        var pv1 = RequireSingle(segments, "PV1", messageControlId);
        var orc = RequireSingle(segments, "ORC", messageControlId);
        var obr = RequireSingle(segments, "OBR", messageControlId);
        var obxSegments = segments.Where(segment => segment.Name == "OBX").ToArray();

        var patientCx = RequireValue(pid.Field(3), "PID-3").Split('^');
        var patientId = RequireValue(patientCx.ElementAtOrDefault(0), "PID-3.1");
        var assigningAuthority = patientCx.ElementAtOrDefault(3);
        if (string.IsNullOrWhiteSpace(assigningAuthority)) assigningAuthority = msh.Field(4);

        var bilirubin = RequireObservation(obxSegments, "1975-2", "14631-6");
        var unitComponents = bilirubin.Field(6).Split('^');
        var unit = NormalizeUnit(unitComponents.ElementAtOrDefault(0), messageControlId);
        if (!decimal.TryParse(bilirubin.Field(5), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            throw new Hl7ValidationException("OBX-5 bilirubin phải là số decimal.", messageControlId);

        var canonical = new HisClinicalCalculationRequest
        {
            Source = new HisSourceSystemDto
            {
                System = RequireValue(msh.Field(3), "MSH-3"),
                Facility = RequireValue(msh.Field(4), "MSH-4"),
                MessageId = messageControlId
            },
            Patient = new HisPatientContextDto
            {
                Identifier = patientId,
                AssigningAuthority = assigningAuthority!,
                AgeHours = (double)RequireNumericObservation(obxSegments, "BILI_AGE_HOURS", messageControlId),
                GestationalAgeWeeks = checked((int)RequireNumericObservation(obxSegments, "BILI_GA_WEEKS", messageControlId)),
                PhototherapyStatus = RequireTextObservation(obxSegments, "BILI_PHOTOTHERAPY_STATUS", messageControlId)
            },
            Encounter = new HisEncounterReferenceDto
            {
                Identifier = RequireValue(pv1.Field(19), "PV1-19")
            },
            Order = new HisOrderReferenceDto
            {
                Identifier = RequireValue(orc.Field(2), "ORC-2")
            },
            Specimen = new HisSpecimenReferenceDto
            {
                Identifier = RequireValue(obr.Field(3), "OBR-3"),
                CollectedAt = ParseTimestamp(RequireValue(obr.Field(7), "OBR-7"), "OBR-7", messageControlId)
            },
            Observation = new HisBilirubinObservationDto
            {
                Identifier = RequireValue(bilirubin.Field(1), "OBX-1"),
                EffectiveAt = ParseTimestamp(
                    string.IsNullOrWhiteSpace(bilirubin.Field(14)) ? obr.Field(7) : bilirubin.Field(14),
                    "OBX-14/OBR-7",
                    messageControlId),
                Value = value,
                Unit = unit
            },
            RiskFactors = new HisRiskFactorsDto
            {
                BenhTanHuyetMienDichHoacThieuG6PD = GetBooleanObservation(obxSegments, "BILI_IMMUNE_HEMOLYSIS_G6PD"),
                NhiemKhuanHuyetHoacNghiNgo = GetBooleanObservation(obxSegments, "BILI_SEPSIS"),
                AlbuminThapDuoi3gDl = GetBooleanObservation(obxSegments, "BILI_ALBUMIN_LOW"),
                TinhTrangLamSangKhongOnDinh = GetBooleanObservation(obxSegments, "BILI_CLINICAL_INSTABILITY"),
                VangDaTrong24hDau = GetBooleanObservation(obxSegments, "BILI_JAUNDICE_FIRST_24H"),
                BenhTanHuyetRh = GetBooleanObservation(obxSegments, "BILI_RH_HEMOLYSIS"),
                BenhTanHuyetABO = GetBooleanObservation(obxSegments, "BILI_ABO_HEMOLYSIS"),
                DauHieuBenhNaoBilirubinCap = GetBooleanObservation(obxSegments, "BILI_ACUTE_ENCEPHALOPATHY")
            }
        };

        return new Hl7OruParseResult(canonical, msh, messageControlId);
    }

    public string BuildAck(Hl7Segment? incomingMsh, string incomingControlId, string acknowledgmentCode, string text, string? zbr = null)
    {
        var sendingApplication = incomingMsh?.Field(5) ?? "UNKNOWN";
        var sendingFacility = incomingMsh?.Field(6) ?? "UNKNOWN";
        var receivingApplication = incomingMsh?.Field(3) ?? "UNKNOWN";
        var receivingFacility = incomingMsh?.Field(4) ?? "UNKNOWN";
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmsszzz", CultureInfo.InvariantCulture).Replace(":", string.Empty);
        var ackControlId = $"ACK{Guid.NewGuid():N}";
        var segments = new List<string>
        {
            $"MSH|^~\\&|{Escape(sendingApplication)}|{Escape(sendingFacility)}|{Escape(receivingApplication)}|{Escape(receivingFacility)}|{timestamp}||ACK^R01^ACK|{ackControlId}|P|2.5.1",
            $"MSA|{acknowledgmentCode}|{Escape(incomingControlId)}|{Escape(text)}"
        };
        if (acknowledgmentCode != "AA") segments.Add($"ERR|||207^Application internal error^HL70357|E|||{Escape(text)}");
        if (!string.IsNullOrWhiteSpace(zbr)) segments.Add(zbr);
        return string.Join('\r', segments) + '\r';
    }

    private static Hl7Segment RequireObservation(IEnumerable<Hl7Segment> segments, params string[] codes)
    {
        var observation = segments.FirstOrDefault(segment => codes.Contains(ObservationCode(segment), StringComparer.Ordinal));
        return observation ?? throw new Hl7ValidationException($"Thiếu OBX bilirubin ({string.Join(" hoặc ", codes)}).", null);
    }

    private static decimal RequireNumericObservation(IEnumerable<Hl7Segment> segments, string code, string controlId)
    {
        var text = RequireTextObservation(segments, code, controlId);
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            throw new Hl7ValidationException($"OBX {code} phải là số.", controlId);
        return value;
    }

    private static string RequireTextObservation(IEnumerable<Hl7Segment> segments, string code, string controlId)
    {
        var observation = segments.FirstOrDefault(segment => ObservationCode(segment) == code)
            ?? throw new Hl7ValidationException($"Thiếu OBX {code}.", controlId);
        return RequireValue(observation.Field(5), $"OBX-5 ({code})");
    }

    private static bool GetBooleanObservation(IEnumerable<Hl7Segment> segments, string code)
    {
        var value = segments.FirstOrDefault(segment => ObservationCode(segment) == code)?.Field(5);
        return value is "Y" or "1" or "true" or "TRUE";
    }

    private static string ObservationCode(Hl7Segment segment) => segment.Field(3).Split('^')[0];

    private static string NormalizeUnit(string? value, string controlId) => value switch
    {
        "mg/dL" => "mg/dL",
        "umol/L" or "µmol/L" => "umol/L",
        _ => throw new Hl7ValidationException("OBX-6 phải là UCUM mg/dL hoặc umol/L.", controlId)
    };

    private static DateTimeOffset ParseTimestamp(string value, string field, string? controlId)
    {
        if (value.Length >= 5 && value[^5] is '+' or '-' && value[^3] != ':')
            value = value.Insert(value.Length - 2, ":");
        var formats = new[] { "yyyyMMddHHmmsszzz", "yyyyMMddHHmmss", "yyyyMMddHHmm", "yyyyMMdd" };
        foreach (var format in formats)
        {
            if (DateTimeOffset.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
                return result;
        }
        throw new Hl7ValidationException($"{field} không phải HL7 DTM hợp lệ.", controlId);
    }

    private static Hl7Segment RequireSingle(IEnumerable<Hl7Segment> segments, string name, string? controlId = null) =>
        segments.SingleOrDefault(segment => segment.Name == name)
        ?? throw new Hl7ValidationException($"Thiếu segment {name}.", controlId);

    private static string RequireValue(string? value, string field) =>
        !string.IsNullOrWhiteSpace(value) ? value : throw new Hl7ValidationException($"Thiếu {field}.", null);

    private static string Escape(string value) => value
        .Replace("\\", "\\E\\", StringComparison.Ordinal)
        .Replace("|", "\\F\\", StringComparison.Ordinal)
        .Replace("^", "\\S\\", StringComparison.Ordinal)
        .Replace("~", "\\R\\", StringComparison.Ordinal)
        .Replace("&", "\\T\\", StringComparison.Ordinal);
}

public sealed record Hl7OruParseResult(HisClinicalCalculationRequest Request, Hl7Segment Msh, string MessageControlId);

public sealed class Hl7Segment
{
    private readonly string[] _fields;
    public string Name { get; }

    private Hl7Segment(string name, string[] fields)
    {
        Name = name;
        _fields = fields;
    }

    public string Field(int number)
    {
        if (Name == "MSH") return number == 1 ? "|" : _fields.ElementAtOrDefault(number - 1) ?? string.Empty;
        return _fields.ElementAtOrDefault(number) ?? string.Empty;
    }

    public static Hl7Segment Parse(string text)
    {
        if (text.Length < 3) throw new Hl7ValidationException("HL7 segment không hợp lệ.", null);
        var fields = text.Split('|');
        return new Hl7Segment(fields[0], fields);
    }
}

public sealed class Hl7ValidationException(string message, string? messageControlId) : Exception(message)
{
    public string? MessageControlId { get; } = messageControlId;
}
