using System.Globalization;
using System.Text.Json;
using BiliTool.Vn.Application.DTOs;

namespace BiliTool.Vn.Web.Services.Fhir;

public sealed class FhirR4BilirubinBundleAdapter
{
    public const string ProfileBase = "https://bilitool.vn/fhir/StructureDefinition/";
    public const string FacilityCodeSystem = "https://bilitool.vn/fhir/CodeSystem/facility";
    private const string LoincSystem = "http://loinc.org";
    private const string UcumSystem = "http://unitsofmeasure.org";

    public HisClinicalCalculationRequest Parse(JsonElement bundle)
    {
        RequireString(bundle, "resourceType", "Bundle");
        RequireString(bundle, "type", "transaction");

        var bundleIdentifier = RequireObject(bundle, "identifier");
        var sourceSystem = RequireText(bundleIdentifier, "system");
        var messageId = RequireText(bundleIdentifier, "value");
        var facility = RequireFacilityTag(bundle);
        var patient = RequireResource(bundle, "Patient");
        var encounter = RequireResource(bundle, "Encounter");
        var serviceRequest = RequireResource(bundle, "ServiceRequest");
        var specimen = RequireResource(bundle, "Specimen");
        var observation = RequireResource(bundle, "Observation");

        ValidateObservationCode(observation);
        var patientIdentifier = RequireIdentifier(patient);
        var observationQuantity = RequireObject(observation, "valueQuantity");
        RequireString(observationQuantity, "system", UcumSystem);
        var unit = RequireText(observationQuantity, "code");
        if (unit is not "mg/dL" and not "umol/L")
            throw new FhirBundleValidationException("Observation.valueQuantity.code phải là 'mg/dL' hoặc 'umol/L'.");

        var canonical = new HisClinicalCalculationRequest
        {
            Source = new HisSourceSystemDto
            {
                System = sourceSystem,
                Facility = facility,
                MessageId = messageId
            },
            Patient = new HisPatientContextDto
            {
                Identifier = patientIdentifier.Value,
                AssigningAuthority = patientIdentifier.System,
                AgeHours = RequireExtensionDecimal(patient, "age-hours"),
                GestationalAgeWeeks = RequireExtensionInteger(serviceRequest, "gestational-age-weeks"),
                PhototherapyStatus = RequireExtensionString(serviceRequest, "phototherapy-status")
            },
            Encounter = new HisEncounterReferenceDto { Identifier = RequireId(encounter) },
            Order = new HisOrderReferenceDto { Identifier = RequireId(serviceRequest) },
            Specimen = new HisSpecimenReferenceDto
            {
                Identifier = RequireId(specimen),
                CollectedAt = RequireDateTime(RequireObject(specimen, "collection"), "collectedDateTime")
            },
            Observation = new HisBilirubinObservationDto
            {
                Identifier = RequireId(observation),
                EffectiveAt = RequireDateTime(observation, "effectiveDateTime"),
                Value = RequireDecimal(observationQuantity, "value"),
                Unit = unit
            },
            RiskFactors = ParseRiskFactors(serviceRequest)
        };

        ValidateReference(observation, "subject", "Patient", RequireId(patient));
        ValidateReference(observation, "encounter", "Encounter", canonical.Encounter.Identifier);
        ValidateReference(observation, "specimen", "Specimen", canonical.Specimen.Identifier);
        return canonical;
    }

    private static HisRiskFactorsDto ParseRiskFactors(JsonElement resource) => new()
    {
        BenhTanHuyetMienDichHoacThieuG6PD = GetExtensionBoolean(resource, "immune-hemolysis-or-g6pd"),
        NhiemKhuanHuyetHoacNghiNgo = GetExtensionBoolean(resource, "sepsis-or-suspected-sepsis"),
        AlbuminThapDuoi3gDl = GetExtensionBoolean(resource, "albumin-below-3-g-dl"),
        TinhTrangLamSangKhongOnDinh = GetExtensionBoolean(resource, "clinical-instability"),
        VangDaTrong24hDau = GetExtensionBoolean(resource, "jaundice-first-24-hours"),
        BenhTanHuyetRh = GetExtensionBoolean(resource, "rh-hemolysis"),
        BenhTanHuyetABO = GetExtensionBoolean(resource, "abo-hemolysis"),
        DauHieuBenhNaoBilirubinCap = GetExtensionBoolean(resource, "acute-bilirubin-encephalopathy")
    };

    private static void ValidateObservationCode(JsonElement observation)
    {
        var coding = RequireObject(RequireObject(observation, "code"), "coding", firstArrayItem: true);
        RequireString(coding, "system", LoincSystem);
        var code = RequireText(coding, "code");
        if (code is not "1975-2" and not "14631-6")
            throw new FhirBundleValidationException("Observation.code phải là LOINC 1975-2 hoặc 14631-6.");
    }

    private static void ValidateReference(JsonElement resource, string property, string resourceType, string id)
    {
        var reference = RequireText(RequireObject(resource, property), "reference");
        if (!string.Equals(reference, $"{resourceType}/{id}", StringComparison.Ordinal))
            throw new FhirBundleValidationException($"{resourceType} reference không khớp resource trong Bundle.");
    }

    private static JsonElement RequireResource(JsonElement bundle, string resourceType)
    {
        if (bundle.ValueKind != JsonValueKind.Object ||
            !bundle.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
            throw new FhirBundleValidationException("Bundle.entry là bắt buộc.");
        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object) continue;
            if (!entry.TryGetProperty("resource", out var resource)) continue;
            if (resource.ValueKind != JsonValueKind.Object) continue;
            if (resource.TryGetProperty("resourceType", out var type) && type.GetString() == resourceType)
                return resource;
        }
        throw new FhirBundleValidationException($"Bundle thiếu resource {resourceType}.");
    }

    private static (string System, string Value) RequireIdentifier(JsonElement resource)
    {
        var identifier = RequireObject(resource, "identifier", firstArrayItem: true);
        return (RequireText(identifier, "system"), RequireText(identifier, "value"));
    }

    private static string RequireId(JsonElement resource) => RequireText(resource, "id");

    private static JsonElement RequireObject(JsonElement element, string property, bool firstArrayItem = false)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
            throw new FhirBundleValidationException($"Thiếu trường FHIR '{property}'.");
        if (firstArrayItem)
        {
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
                throw new FhirBundleValidationException($"FHIR '{property}' phải là array không rỗng.");
            var first = value[0];
            if (first.ValueKind != JsonValueKind.Object)
                throw new FhirBundleValidationException($"FHIR '{property}' phải chứa object.");
            return first;
        }
        if (value.ValueKind != JsonValueKind.Object)
            throw new FhirBundleValidationException($"FHIR '{property}' phải là object.");
        return value;
    }

    private static string RequireText(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new FhirBundleValidationException($"Thiếu giá trị FHIR '{property}'.");
        return value.GetString()!;
    }

    private static void RequireString(JsonElement element, string property, string expected)
    {
        var actual = RequireText(element, property);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new FhirBundleValidationException($"FHIR '{property}' phải bằng '{expected}'.");
    }

    private static decimal RequireDecimal(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value) || !value.TryGetDecimal(out var result))
            throw new FhirBundleValidationException($"FHIR '{property}' phải là decimal.");
        return result;
    }

    private static DateTimeOffset RequireDateTime(JsonElement element, string property)
    {
        var value = RequireText(element, property);
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
            throw new FhirBundleValidationException($"FHIR '{property}' không phải dateTime hợp lệ.");
        return result;
    }

    private static JsonElement? FindExtension(JsonElement resource, string suffix)
    {
        if (resource.ValueKind != JsonValueKind.Object ||
            !resource.TryGetProperty("extension", out var extensions) || extensions.ValueKind != JsonValueKind.Array) return null;
        var url = ProfileBase + suffix;
        foreach (var extension in extensions.EnumerateArray())
        {
            if (extension.ValueKind != JsonValueKind.Object) continue;
            if (extension.TryGetProperty("url", out var candidate) && candidate.GetString() == url) return extension;
        }
        return null;
    }

    private static string RequireFacilityTag(JsonElement bundle)
    {
        var meta = RequireObject(bundle, "meta");
        if (!meta.TryGetProperty("tag", out var tags) || tags.ValueKind != JsonValueKind.Array)
            throw new FhirBundleValidationException("Bundle.meta.tag phải chứa mã cơ sở.");

        foreach (var tag in tags.EnumerateArray())
        {
            if (tag.ValueKind != JsonValueKind.Object) continue;
            if (tag.TryGetProperty("system", out var system) && system.GetString() == FacilityCodeSystem)
                return RequireText(tag, "code");
        }

        throw new FhirBundleValidationException($"Bundle.meta.tag thiếu system '{FacilityCodeSystem}'.");
    }

    private static string RequireExtensionString(JsonElement resource, string suffix)
    {
        var extension = FindExtension(resource, suffix) ?? throw new FhirBundleValidationException($"Thiếu extension '{suffix}'.");
        return RequireText(extension, "valueString");
    }

    private static double RequireExtensionDecimal(JsonElement resource, string suffix)
    {
        var extension = FindExtension(resource, suffix) ?? throw new FhirBundleValidationException($"Thiếu extension '{suffix}'.");
        if (!extension.TryGetProperty("valueDecimal", out var value) || !value.TryGetDouble(out var result))
            throw new FhirBundleValidationException($"Extension '{suffix}' phải có valueDecimal.");
        return result;
    }

    private static int RequireExtensionInteger(JsonElement resource, string suffix)
    {
        var extension = FindExtension(resource, suffix) ?? throw new FhirBundleValidationException($"Thiếu extension '{suffix}'.");
        if (!extension.TryGetProperty("valueInteger", out var value) || !value.TryGetInt32(out var result))
            throw new FhirBundleValidationException($"Extension '{suffix}' phải có valueInteger.");
        return result;
    }

    private static bool GetExtensionBoolean(JsonElement resource, string suffix)
    {
        var extension = FindExtension(resource, suffix);
        return extension.HasValue && extension.Value.ValueKind == JsonValueKind.Object &&
               extension.Value.TryGetProperty("valueBoolean", out var value) && value.ValueKind == JsonValueKind.True;
    }
}

public sealed class FhirBundleValidationException(string message) : Exception(message);
