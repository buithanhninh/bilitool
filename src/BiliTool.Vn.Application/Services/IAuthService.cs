namespace BiliTool.Vn.Application.Services;

public class RegisterRequest
{
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public interface IAuthService
{
    Task<(bool Success, string Message, string? UserId)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string Message, string? UserId)> LoginAsync(LoginRequest request);
    Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otpCode);
    Task<(bool Success, string Message)> SendOtpAsync(string email, bool forPasswordReset = false);
    Task<(bool Success, string Message)> ResetPasswordAsync(string email, string otpCode, string newPassword);
}
