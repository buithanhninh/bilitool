using BiliTool.Vn.Application.DTOs;
using FluentValidation;
using System.Linq.Expressions;

namespace BiliTool.Vn.Application.Validators;

public sealed class HisClinicalCalculationRequestValidator : AbstractValidator<HisClinicalCalculationRequest>
{
    private static readonly string[] SupportedUnits = ["mg/dL", "umol/L"];
    private static readonly string[] SupportedPhototherapyStatuses =
        ["none", "phototherapy", "intensive-phototherapy", "stopped"];

    public HisClinicalCalculationRequestValidator()
    {
        RuleFor(request => request.Source).NotNull().SetValidator(new SourceValidator());
        RuleFor(request => request.Patient).NotNull().SetValidator(new PatientValidator());
        RuleFor(request => request.Encounter).NotNull().SetValidator(new IdentifierObjectValidator<HisEncounterReferenceDto>(item => item.Identifier));
        RuleFor(request => request.Order).NotNull().SetValidator(new IdentifierObjectValidator<HisOrderReferenceDto>(item => item.Identifier));
        RuleFor(request => request.Specimen).NotNull().SetValidator(new SpecimenValidator());
        RuleFor(request => request.Observation).NotNull().SetValidator(new ObservationValidator());
        RuleFor(request => request.RiskFactors).NotNull();
    }

    private sealed class SourceValidator : AbstractValidator<HisSourceSystemDto>
    {
        public SourceValidator()
        {
            RuleFor(item => item.System).NotEmpty().MaximumLength(128);
            RuleFor(item => item.Facility).NotEmpty().MaximumLength(128);
            RuleFor(item => item.MessageId).NotEmpty().MaximumLength(128);
        }
    }

    private sealed class PatientValidator : AbstractValidator<HisPatientContextDto>
    {
        public PatientValidator()
        {
            RuleFor(item => item.Identifier).NotEmpty().MaximumLength(128);
            RuleFor(item => item.AssigningAuthority).NotEmpty().MaximumLength(128);
            RuleFor(item => item)
                .Must(item => item.AgeHours.HasValue || item.BirthTime.HasValue)
                .WithMessage("Phải cung cấp patient.ageHours hoặc patient.birthTime.");
            When(item => item.AgeHours.HasValue, () =>
                RuleFor(item => item.AgeHours!.Value).Must(double.IsFinite).InclusiveBetween(1, 336));
            RuleFor(item => item.GestationalAgeWeeks).InclusiveBetween(35, 45);
            RuleFor(item => item.PhototherapyStatus)
                .Must(value => SupportedPhototherapyStatuses.Contains(value, StringComparer.Ordinal))
                .WithMessage("patient.phototherapyStatus không được hỗ trợ.");
        }
    }

    private sealed class SpecimenValidator : AbstractValidator<HisSpecimenReferenceDto>
    {
        public SpecimenValidator()
        {
            RuleFor(item => item.Identifier).NotEmpty().MaximumLength(128);
            RuleFor(item => item.CollectedAt).NotEqual(default(DateTimeOffset));
        }
    }

    private sealed class ObservationValidator : AbstractValidator<HisBilirubinObservationDto>
    {
        public ObservationValidator()
        {
            RuleFor(item => item.Identifier).NotEmpty().MaximumLength(128);
            RuleFor(item => item.EffectiveAt).NotEqual(default(DateTimeOffset));
            RuleFor(item => item.Value).GreaterThan(0);
            RuleFor(item => item.Unit)
                .Must(value => SupportedUnits.Contains(value, StringComparer.Ordinal))
                .WithMessage("observation.unit phải là 'mg/dL' hoặc 'umol/L'.");
        }
    }

    private sealed class IdentifierObjectValidator<T> : AbstractValidator<T>
    {
        public IdentifierObjectValidator(Expression<Func<T, string>> selector) =>
            RuleFor(selector).NotEmpty().MaximumLength(128);
    }
}
