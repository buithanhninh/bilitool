using System;

namespace BiliTool.Vn.Application.DTOs;

public class NhatKyTinhToanAdminDto
{
    public Guid Id { get; set; }
    public Guid PhienId { get; set; }
    
    // Thong tin tu PhienLamViec
    public string? DiaChiIP { get; set; }
    public string? ThietBi { get; set; }
    public bool LaAnDanh { get; set; }
    public string NguoiDungId { get; set; } = string.Empty;
    public string NguoiDungEmail { get; set; } = string.Empty;
    public string NguoiDungTen { get; set; } = string.Empty;

    // Chi tiet tinh toan
    public int TuoiGio { get; set; }
    public int TuoiThaiTuan { get; set; }
    public decimal BilirubinMgDl { get; set; }
    public bool CoNguyCoThanKinh { get; set; }
    
    public decimal NguongChieuDen { get; set; }
    public decimal NguongChieuDenTichCuc { get; set; }
    public decimal NguongThayCuuMau { get; set; }
    public string MucDoNguyHiem { get; set; } = string.Empty;
    
    public string ChiTietYeuCauJson { get; set; } = string.Empty;
    
    public DateTime NgayTinhToan { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
