using BiliTool.Vn.Application.Commands;
using BiliTool.Vn.Application.DTOs;
using FluentValidation;

namespace BiliTool.Vn.Application.Validators;

/// <summary>
/// Validator cho TinhToanBilirubinCommand để kích hoạt validation trong MediatR Pipeline.
/// </summary>
public class TinhToanBilirubinCommandValidator : AbstractValidator<TinhToanBilirubinCommand>
{
    public TinhToanBilirubinCommandValidator(IValidator<YeuCauTinhToanBilirubinDto> innerValidator)
    {
        RuleFor(x => x.YeuCau)
            .NotNull()
            .WithMessage("Yêu cầu tính toán không được để trống.")
            .SetValidator(innerValidator);
    }
}
