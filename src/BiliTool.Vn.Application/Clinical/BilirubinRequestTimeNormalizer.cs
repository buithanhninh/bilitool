using BiliTool.Vn.Application.DTOs;

namespace BiliTool.Vn.Application.Clinical;

public static class BilirubinRequestTimeNormalizer
{
    public const double MinimumAgeHours = 1d;
    public const double MaximumAgeHours = 336d;
    public const double AgeConsistencyToleranceHours = 0.01d;

    public static double CalculateAgeHours(YeuCauTinhToanBilirubinDto request)
    {
        if (request.TuoiTheoGio.HasValue)
        {
            return request.TuoiTheoGio.Value;
        }

        if (TryGetClinicalInstants(request, out var birthTime, out var sampleTime))
        {
            return (sampleTime - birthTime).TotalHours;
        }

        throw new InvalidOperationException(
            "Phải cung cấp hoặc ngày giờ sinh + ngày giờ lấy mẫu, hoặc tuổi tính theo giờ.");
    }

    public static bool TryGetClinicalInstants(
        YeuCauTinhToanBilirubinDto request,
        out DateTime birthTime,
        out DateTime sampleTime)
    {
        birthTime = default;
        sampleTime = default;

        if (!request.NgaySinh.HasValue || !request.NgayLayMau.HasValue)
        {
            return false;
        }

        birthTime = Combine(request.NgaySinh.Value, request.GioSinh);
        sampleTime = Combine(request.NgayLayMau.Value, request.GioLayMau);
        return true;
    }

    public static DateTime? GetSampleTime(YeuCauTinhToanBilirubinDto request)
    {
        return request.NgayLayMau.HasValue
            ? Combine(request.NgayLayMau.Value, request.GioLayMau)
            : null;
    }

    private static DateTime Combine(DateTime dateTime, TimeSpan? explicitTime)
    {
        return explicitTime.HasValue
            ? dateTime.Date + explicitTime.Value
            : dateTime;
    }
}
