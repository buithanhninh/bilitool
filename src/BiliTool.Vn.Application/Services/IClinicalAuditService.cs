using BiliTool.Vn.Domain.Clinical.Bilirubin;

namespace BiliTool.Vn.Application.Services;

public interface IClinicalAuditService
{
    Task TryRecordCalculationAsync(
        object request,
        object response,
        BilirubinCalculationTrace trace,
        CancellationToken cancellationToken = default);
}
