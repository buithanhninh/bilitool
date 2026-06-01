namespace BiliTool.Vn.Application.DTOs;

public class HoSoNguoiDungDto
{
    public string Id { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? SoDienThoai { get; set; }
    public string? DonViCongTac { get; set; }
    public string? ChuyenKhoa { get; set; }
    public string? ChucDanh { get; set; }
    public DateTime NgayCapNhat { get; set; }
}
