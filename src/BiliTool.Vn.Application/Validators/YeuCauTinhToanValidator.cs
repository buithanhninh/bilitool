using BiliTool.Vn.Application.DTOs;
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
                .InclusiveBetween(1, 336)
                .WithMessage("Tuổi phải từ 1 đến 336 giờ (tối đa 14 ngày).");
        });

        // Kiểm tra ngày tháng hợp lệ
        When(x => x.NgaySinh.HasValue && x.NgayLayMau.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.NgayLayMau!.Value >= x.NgaySinh!.Value)
                .WithMessage("Ngày lấy mẫu phải sau ngày sinh.");

            RuleFor(x => x)
                .Must(x =>
                {
                    var diff = (x.NgayLayMau!.Value - x.NgaySinh!.Value).TotalHours;
                    return diff >= 1 && diff <= 336;
                })
                .WithMessage("Tuổi tính được phải từ 1 đến 336 giờ.");
        });

        // Kiểm tra bilirubin
        RuleFor(x => x.TongBilirubin)
            .GreaterThan(0)
            .WithMessage("Giá trị bilirubin phải lớn hơn 0.");

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
        When(x => x.YeuToNguyCo.ETCOcPpm.HasValue, () =>
        {
            RuleFor(x => x.YeuToNguyCo.ETCOcPpm!.Value)
                .GreaterThan(0)
                .LessThan(10)
                .WithMessage("Giá trị ETCOc không hợp lệ (0-10 ppm).");
        });
    }
}
