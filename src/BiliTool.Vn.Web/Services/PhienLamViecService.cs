using System.Threading.Tasks;
using BiliTool.Vn.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Http;
using System;
using BiliTool.Vn.Domain.Entities;

namespace BiliTool.Vn.Web.Services;

/// <summary>
/// Service quản lý phiên làm việc trong Blazor
/// Sử dụng ProtectedLocalStorage để duy trì phiên qua các lần reload
/// </summary>
public class PhienLamViecService
{
    private readonly NguoiDungHienTaiService _nguoiDung;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ProtectedLocalStorage _localStorage;
    private readonly IMediator _mediator;

    private Guid? _currentSessionId;
    private const string SessionKey = "bilitool_session_id";

    public PhienLamViecService(
        NguoiDungHienTaiService nguoiDung,
        IHttpContextAccessor httpContext,
        ProtectedLocalStorage localStorage,
        IMediator mediator)
    {
        _nguoiDung = nguoiDung;
        _httpContext = httpContext;
        _localStorage = localStorage;
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy Session từ Storage, nếu chưa có thì cấp mới. 
    /// Đồng thời đồng bộ xuống Database.
    /// Có thể throw khi Prerendering do JSInterop chưa sẵn sàng.
    /// </summary>
    public async Task<Guid> LayHoacTaoPhienAsync()
    {
        if (_currentSessionId.HasValue)
            return _currentSessionId.Value;

        Guid phienId;
        try
        {
            var result = await _localStorage.GetAsync<Guid>(SessionKey);
            phienId = result.Success ? result.Value : Guid.NewGuid();
            if (!result.Success)
            {
                await _localStorage.SetAsync(SessionKey, phienId);
            }
        }
        catch (InvalidOperationException)
        {
            // Fallback khi prerendering (chưa có JSInterop): tạo tạm ID nhưng không lưu
            phienId = Guid.NewGuid();
            return phienId;
        }
        catch (Exception)
        {
            // Các lỗi khác (như CryptographicException do server đổi Data Protection Key)
            phienId = Guid.NewGuid();
            try 
            {
                // Bắt buộc lưu đè key mới để đồng bộ các lần tính toán tiếp theo
                await _localStorage.SetAsync(SessionKey, phienId);
            } 
            catch { /* fallback nếu không lưu được */ }
        }

        _currentSessionId = phienId;

        var ip = _httpContext.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = _httpContext.HttpContext?.Request?.Headers["User-Agent"].ToString();

        // Đảm bảo phiên tồn tại trong DB, gắn User ID nếu đăng nhập
        await _mediator.Send(new LayPhienLamViecQuery(phienId, _nguoiDung.Id, ip, userAgent));

        return phienId;
    }
    
    public async Task<PhienLamViec?> LayChiTietPhienAsync()
    {
        try
        {
            var id = await LayHoacTaoPhienAsync();
            return await _mediator.Send(new LayPhienLamViecQuery(id, _nguoiDung.Id));
        }
        catch
        {
            return null;
        }
    }
}
