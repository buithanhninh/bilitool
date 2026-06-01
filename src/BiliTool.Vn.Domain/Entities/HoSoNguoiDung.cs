namespace BiliTool.Vn.Domain.Entities;

/// <summary>
/// Hồ sơ thông tin bổ sung của bác sĩ (ngoài dữ liệu Google OAuth).
/// Khóa chính là Id để lookup nhanh, không cần bảng User riêng.
/// </summary>
public class HoSoNguoiDung
{
    public string Id { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? SoDienThoai { get; set; }
    public string? DonViCongTac { get; set; }
    public string? ChuyenKhoa { get; set; }
    public string? ChucDanh { get; set; }
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
    
    // Thuộc tính quản trị
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime NgayDangNhapCuoi { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // Thuộc tính xác thực Local (Email/Password)
    public string? GoogleId { get; set; }
    public string? PasswordHash { get; set; }
    public string? Salt { get; set; }
    public bool IsEmailVerified { get; set; } = false;
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiryTime { get; set; }
}
