using System.Security.Cryptography;
using System.Net;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BiliTool.Vn.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly BiliToolDbContext _dbContext;
    private readonly IEmailService _emailService;

    public AuthService(BiliToolDbContext dbContext, IEmailService emailService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
    }

    public async Task<(bool Success, string Message, string? UserId)> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _dbContext.HoSoNguoiDung.FirstOrDefaultAsync(x => x.Email.ToLower() == request.Email.ToLower());
        if (existingUser != null)
        {
            if (existingUser.IsEmailVerified)
                return (false, "Email này đã được đăng ký.", null);
            
            // Re-register unverified user
            existingUser.HoTen = request.HoTen;
            UpdatePassword(existingUser, request.Password);
            await GenerateAndSendOtp(existingUser, "Xác thực tài khoản BiliTool.Vn");
            
            await _dbContext.SaveChangesAsync();
            return (true, "Mã xác thực mới đã được gửi đến email của bạn.", existingUser.Id);
        }

        var newUser = new HoSoNguoiDung
        {
            Id = Guid.NewGuid().ToString(),
            HoTen = request.HoTen,
            Email = request.Email,
            NgayTao = DateTime.UtcNow,
            NgayCapNhat = DateTime.UtcNow,
            IsEmailVerified = false
        };
        UpdatePassword(newUser, request.Password);

        _dbContext.HoSoNguoiDung.Add(newUser);
        await GenerateAndSendOtp(newUser, "Xác thực tài khoản BiliTool.Vn");
        await _dbContext.SaveChangesAsync();

        return (true, "Vui lòng kiểm tra email để nhận mã xác thực.", newUser.Id);
    }

    public async Task<(bool Success, string Message, string? UserId)> LoginAsync(LoginRequest request)
    {
        var user = await _dbContext.HoSoNguoiDung.FirstOrDefaultAsync(x => x.Email.ToLower() == request.Email.ToLower());
        
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return (false, "Email hoặc mật khẩu không chính xác.", null);

        if (!user.IsActive)
            return (false, "Tài khoản của bạn đã bị khóa.", null);

        if (!user.IsEmailVerified)
            return (false, "Vui lòng xác thực email trước khi đăng nhập.", null);

        if (!VerifyPassword(request.Password, user.PasswordHash, user.Salt!))
            return (false, "Email hoặc mật khẩu không chính xác.", null);

        user.NgayDangNhapCuoi = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return (true, "Đăng nhập thành công", user.Id);
    }

    public async Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otpCode)
    {
        var user = await _dbContext.HoSoNguoiDung.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
        if (user == null)
            return (false, "Không tìm thấy người dùng.");

        if (user.OtpCode != otpCode)
            return (false, "Mã OTP không chính xác.");

        if (user.OtpExpiryTime < DateTime.UtcNow)
            return (false, "Mã OTP đã hết hạn.");

        user.IsEmailVerified = true;
        user.OtpCode = null;
        user.OtpExpiryTime = null;
        await _dbContext.SaveChangesAsync();

        return (true, "Xác thực email thành công.");
    }

    public async Task<(bool Success, string Message)> SendOtpAsync(string email, bool forPasswordReset = false)
    {
        var user = await _dbContext.HoSoNguoiDung.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
        if (user == null)
            return (false, "Không tìm thấy tài khoản với email này.");

        if (forPasswordReset && !user.IsEmailVerified)
            return (false, "Tài khoản này chưa được xác thực.");

        await GenerateAndSendOtp(user, forPasswordReset ? "Mã xác nhận đặt lại mật khẩu BiliTool.Vn" : "Xác thực tài khoản BiliTool.Vn");
        await _dbContext.SaveChangesAsync();

        return (true, "Mã OTP đã được gửi đến email của bạn.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string otpCode, string newPassword)
    {
        var user = await _dbContext.HoSoNguoiDung.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
        if (user == null)
            return (false, "Không tìm thấy tài khoản.");

        if (user.OtpCode != otpCode || user.OtpExpiryTime < DateTime.UtcNow)
            return (false, "Mã OTP không hợp lệ hoặc đã hết hạn.");

        UpdatePassword(user, newPassword);
        user.OtpCode = null;
        user.OtpExpiryTime = null;
        await _dbContext.SaveChangesAsync();

        return (true, "Mật khẩu đã được đặt lại thành công.");
    }

    private void UpdatePassword(HoSoNguoiDung user, string password)
    {
        var salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(salt);

        user.Salt = Convert.ToBase64String(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        user.PasswordHash = Convert.ToBase64String(pbkdf2.GetBytes(32));
    }

    private bool VerifyPassword(string password, string hash, string saltString)
    {
        if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(saltString))
            return false;

        var salt = Convert.FromBase64String(saltString);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var computedHash = pbkdf2.GetBytes(32);
        var storedHash = Convert.FromBase64String(hash);

        return storedHash.Length == computedHash.Length &&
               CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }

    private async Task GenerateAndSendOtp(HoSoNguoiDung user, string subject)
    {
        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        user.OtpCode = otp;
        user.OtpExpiryTime = DateTime.UtcNow.AddMinutes(15);
        var safeHoTen = WebUtility.HtmlEncode(user.HoTen);

        string htmlTemplate = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 10px;'>
            <h2 style='color: #1a6b9a; text-align: center;'>BiliTool.Vn</h2>
            <p style='font-size: 16px; color: #374151;'>Xin chào {safeHoTen},</p>
            <p style='font-size: 16px; color: #374151;'>Đây là mã OTP của bạn để hoàn tất quá trình xác minh:</p>
            <div style='text-align: center; margin: 30px 0;'>
                <span style='font-size: 32px; font-weight: bold; color: #1a6b9a; letter-spacing: 5px; padding: 10px 20px; background-color: #f0fdfa; border-radius: 8px; border: 2px dashed #1a6b9a;'>{otp}</span>
            </div>
            <p style='font-size: 14px; color: #6b7280; text-align: center;'>Mã này sẽ hết hạn sau 15 phút.</p>
            <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;' />
            <p style='font-size: 12px; color: #9ca3af; text-align: center;'>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.</p>
        </div>";

        await _emailService.SendEmailAsync(user.Email, subject, htmlTemplate);
    }
}
