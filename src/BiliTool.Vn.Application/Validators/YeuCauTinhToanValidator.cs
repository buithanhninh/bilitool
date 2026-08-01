using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Clinical;
using FluentValidation;

namespace BiliTool.Vn.Application.Validators;

/// <summary>Kiểm tra dữ liệu đầu vào trước khi tính toán</summary>
public class YeuCauTinhToanValidator : AbstractValidator<YeuCauTinhToanBilirubinDto>
{
    public YeuCauTinhToanValidator()
    {
        // Kiểm tra có cung cấp tuổi không
        RuleFor(x => x)
            .Must(x => x.TuoiTheoGio.HasValue ||
                       (x.NgaySinh.HasValue && x.NgayLayMau.HasValue))
            .WithMessage("Phải cung cấp tuổi theo giờ HOẶC ngày sinh và ngày lấy mẫu.");

        // Kiểm tra tuổi theo giờ
        When(x => x.TuoiTheoGio.HasValue, () =>
        {
            RuleFor(x => x.TuoiTheoGio!.Value)
                .Must(double.IsFinite)
                .WithMessage("Tuổi theo giờ phải là số hữu hạn.")
                .InclusiveBetween(
                    BilirubinRequestTimeNormalizer.MinimumAgeHours,
                    BilirubinRequestTimeNormalizer.MaximumAgeHours)
                .WithMessage("Tuổi phải từ 1 đến 336 giờ (tối đa 14 ngày).");
        });

        When(x => x.GioSinh.HasValue, () =>
        {
            RuleFor(x => x.GioSinh!.Value)
                .Must(IsTimeOfDay)
                .WithMessage("Giờ sinh phải nằm trong một ngày (00:00:00 đến trước 24:00:00).");
        });

        When(x => x.GioLayMau.HasValue, () =>
        {
            RuleFor(x => x.GioLayMau!.Value)
                .Must(IsTimeOfDay)
                .WithMessage("Giờ lấy mẫu phải nằm trong một ngày (00:00:00 đến trước 24:00:00).");
        });

        // Kiểm tra ngày tháng hợp lệ
        When(x => x.NgaySinh.HasValue && x.NgayLayMau.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x =>
                {
                    BilirubinRequestTimeNormalizer.TryGetClinicalInstants(x, out var birthTime, out var sampleTime);
                    return sampleTime >= birthTime;
                })
                .WithMessage("Ngày lấy mẫu phải sau ngày sinh.");

            RuleFor(x => x)
                .Must(x =>
                {
                    BilirubinRequestTimeNormalizer.TryGetClinicalInstants(x, out var birthTime, out var sampleTime);
                    var diff = (sampleTime - birthTime).TotalHours;
                    return diff >= BilirubinRequestTimeNormalizer.MinimumAgeHours &&
                           diff <= BilirubinRequestTimeNormalizer.MaximumAgeHours;
                })
                .WithMessage("Tuổi tính được phải từ 1 đến 336 giờ.");

            When(x => x.TuoiTheoGio.HasValue, () =>
            {
                RuleFor(x => x)
                    .Must(x =>
                    {
                        BilirubinRequestTimeNormalizer.TryGetClinicalInstants(x, out var birthTime, out var sampleTime);
                        var calculatedAge = (sampleTime - birthTime).TotalHours;
                        return Math.Abs(calculatedAge - x.TuoiTheoGio!.Value) <=
                               BilirubinRequestTimeNormalizer.AgeConsistencyToleranceHours;
                    })
                    .WithMessage("Tuổi theo giờ không khớp với ngày giờ sinh và ngày giờ lấy mẫu.");
            });
        });

        // Kiểm tra bilirubin
        RuleFor(x => x.TongBilirubin)
            .GreaterThan(0)
            .WithMessage("Giá trị bilirubin phải lớn hơn 0.");

        RuleFor(x => x.DonViDo)
            .IsInEnum()
            .WithMessage("Đơn vị đo bilirubin không hợp lệ.");

        RuleFor(x => x.TrangThaiChieuDen)
            .IsInEnum()
            .WithMessage("Trạng thái chiếu đèn không hợp lệ.");

        RuleFor(x => x)
            .Must(x =>
            {
                // mg/dL: thường không quá 60
                // μmol/L: thường không quá 1000
                if (x.DonViDo == Domain.Enums.DonViDo.MgDl)
                    return x.TongBilirubin <= 60;
                return x.TongBilirubin <= 1026; // 60 * 17.1
            })
            .WithMessage("Giá trị bilirubin quá cao, vui lòng kiểm tra lại đơn vị đo.");

        // Kiểm tra tuổi thai
        RuleFor(x => x.TuoiThaiTuan)
            .GreaterThanOrEqualTo(35)
            .WithMessage("Phác đồ AAP 2022 chỉ áp dụng cho trẻ có tuổi thai ≥ 35 tuần. Trẻ non tháng hơn cần đánh giá theo phác đồ riêng.")
            .LessThanOrEqualTo(45)
            .WithMessage("Tuổi thai không hợp lệ (tối đa 45 tuần).");

        // ETCOc nếu có
        RuleFor(x => x.YeuToNguyCo)
            .NotNull()
            .WithMessage("Yếu tố nguy cơ không được để trống.");

        When(x => x.YeuToNguyCo?.ETCOcPpm.HasValue == true, () =>
        {
            RuleFor(x => x.YeuToNguyCo!.ETCOcPpm!.Value)
                .GreaterThan(0)
                .LessThan(10)
                .WithMessage("Giá trị ETCOc không hợp lệ (0-10 ppm).");
        });
    }

    private static bool IsTimeOfDay(TimeSpan value) => value >= TimeSpan.Zero && value < TimeSpan.FromDays(1);
}
