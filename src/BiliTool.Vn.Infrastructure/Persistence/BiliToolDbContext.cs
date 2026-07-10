using BiliTool.Vn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BiliTool.Vn.Infrastructure.Persistence;

/// <summary>DbContext chính của ứng dụng BiliTool.Vn</summary>
public class BiliToolDbContext : DbContext
{
    public BiliToolDbContext(DbContextOptions<BiliToolDbContext> options)
        : base(options) { }

    public DbSet<PhienLamViec> PhienLamViec => Set<PhienLamViec>();
    public DbSet<LichSuTinhToan> LichSuTinhToan => Set<LichSuTinhToan>();
    public DbSet<MauBilirubinLuuTru> MauBilirubin => Set<MauBilirubinLuuTru>();
    public DbSet<HoSoNguoiDung> HoSoNguoiDung => Set<HoSoNguoiDung>();
    public DbSet<HoSoBenhNhan> HoSoBenhNhan => Set<HoSoBenhNhan>();
    public DbSet<XetNghiemBilirubin> XetNghiemBilirubin => Set<XetNghiemBilirubin>();
    public DbSet<ClinicalAuditLog> ClinicalAuditLogs => Set<ClinicalAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── PhienLamViec ──────────────────────────────────────
        modelBuilder.Entity<PhienLamViec>(e =>
        {
            e.ToTable("phien_lam_viec");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.NgayTao).HasColumnName("ngay_tao").IsRequired();
            e.Property(p => p.DiaChiIP).HasColumnName("dia_chi_ip").HasMaxLength(45);
            e.Property(p => p.ThietBi).HasColumnName("thiet_bi").HasMaxLength(500);
            e.Property(p => p.NguoiDungId).HasColumnName("nguoi_dung_id").HasMaxLength(256);

            e.HasMany(p => p.LichSuTinhToan)
             .WithOne(l => l.Phien)
             .HasForeignKey(l => l.PhienId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(p => p.MauBilirubin)
             .WithOne(m => m.Phien)
             .HasForeignKey(m => m.PhienId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── LichSuTinhToan ────────────────────────────────────
        modelBuilder.Entity<LichSuTinhToan>(e =>
        {
            e.ToTable("lich_su_tinh_toan");
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasColumnName("id");
            e.Property(l => l.PhienId).HasColumnName("phien_id").IsRequired();
            e.Property(l => l.TuoiGio).HasColumnName("tuoi_gio").IsRequired();
            e.Property(l => l.TuoiThaiTuan).HasColumnName("tuoi_thai_tuan").IsRequired();
            e.Property(l => l.BilirubinMgDl)
             .HasColumnName("bilirubin_mgdl")
             .HasPrecision(5, 2)
             .IsRequired();
            e.Property(l => l.CoNguyCoThanKinh).HasColumnName("co_nguyen_co_than_kinh");
            e.Property(l => l.NguongChieuDen)
             .HasColumnName("nguong_chieu_den")
             .HasPrecision(5, 2);
            e.Property(l => l.NguongChieuDenTichCuc)
             .HasColumnName("nguong_chieu_den_tich_cuc")
             .HasPrecision(5, 2);
            e.Property(l => l.NguongThayCuuMau)
             .HasColumnName("nguong_thay_cuu_mau")
             .HasPrecision(5, 2);
            e.Property(l => l.MucDoNguyHiem).HasColumnName("muc_do_nguy_hiem").HasMaxLength(100);
            e.Property(l => l.KhuyenNghiChinh).HasColumnName("khuyen_nghi_chinh");
            e.Property(l => l.NgayTinhToan).HasColumnName("ngay_tinh_toan").IsRequired();
        });

        // ── MauBilirubinLuuTru ────────────────────────────────
        modelBuilder.Entity<MauBilirubinLuuTru>(e =>
        {
            e.ToTable("mau_bilirubin");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.PhienId).HasColumnName("phien_id").IsRequired();
            e.Property(m => m.ThuTu).HasColumnName("thu_tu");
            e.Property(m => m.ThoiGianLayMau).HasColumnName("thoi_gian_lay_mau").IsRequired();
            e.Property(m => m.BilirubinMgDl)
             .HasColumnName("bilirubin_mgdl")
             .HasPrecision(5, 2)
             .IsRequired();
            e.Property(m => m.TuoiGioKhiLayMau).HasColumnName("tuoi_gio_khi_lay_mau");
            e.Property(m => m.TocDoThayDoi)
             .HasColumnName("toc_do_thay_doi")
             .HasPrecision(6, 3);
        });

        // ── HoSoNguoiDung ─────────────────────────────────────
        modelBuilder.Entity<HoSoNguoiDung>(e =>
        {
            e.ToTable("ho_so_nguoi_dung");
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).HasColumnName("id").HasMaxLength(256);
            e.Property(h => h.GoogleId).HasColumnName("google_id").HasMaxLength(256);
            e.Property(h => h.PasswordHash).HasColumnName("password_hash");
            e.Property(h => h.Salt).HasColumnName("salt");
            e.Property(h => h.IsEmailVerified).HasColumnName("is_email_verified");
            e.Property(h => h.OtpCode).HasColumnName("otp_code").HasMaxLength(20);
            e.Property(h => h.OtpExpiryTime).HasColumnName("otp_expiry_time");
            e.Property(h => h.HoTen).HasColumnName("ho_ten").HasMaxLength(255).IsRequired();
            e.Property(h => h.NgaySinh).HasColumnName("ngay_sinh");
            e.Property(h => h.SoDienThoai).HasColumnName("so_dien_thoai").HasMaxLength(20);
            e.Property(h => h.DonViCongTac).HasColumnName("don_vi_cong_tac").HasMaxLength(500);
            e.Property(h => h.ChuyenKhoa).HasColumnName("chuyen_khoa").HasMaxLength(200);
            e.Property(h => h.ChucDanh).HasColumnName("chuc_danh").HasMaxLength(200);
            e.Property(h => h.NgayCapNhat).HasColumnName("ngay_cap_nhat").IsRequired();
        });

        // ── HoSoBenhNhan ────────────────────────────────────────────────
        modelBuilder.Entity<HoSoBenhNhan>(e =>
        {
            e.ToTable("ho_so_benh_nhan");
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).HasColumnName("id");
            e.Property(h => h.NguoiDungId).HasColumnName("nguoi_dung_id").HasMaxLength(256).IsRequired();
            e.Property(h => h.HoTenBenhNhan).HasColumnName("ho_ten_benh_nhan").HasMaxLength(255).IsRequired();
            e.Property(h => h.NgayGioSinh).HasColumnName("ngay_gio_sinh").IsRequired();
            e.Property(h => h.TuoiThaiTuan).HasColumnName("tuoi_thai_tuan").IsRequired();
            e.Property(h => h.CoNguyCoThanKinh).HasColumnName("co_nguon_co_than_kinh");
            e.Property(h => h.GhiChu).HasColumnName("ghi_chu");
            e.Property(h => h.NgayTao).HasColumnName("ngay_tao").IsRequired();

            e.HasMany(h => h.DsXetNghiem)
             .WithOne(x => x.BenhNhan)
             .HasForeignKey(x => x.BenhNhanId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(h => h.NguoiDungId).HasDatabaseName("ix_ho_so_benh_nhan_nguoi_dung_id");
        });

        // ── XetNghiemBilirubin ──────────────────────────────────────────
        modelBuilder.Entity<XetNghiemBilirubin>(e =>
        {
            e.ToTable("xet_nghiem_bilirubin");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.BenhNhanId).HasColumnName("benh_nhan_id").IsRequired();
            e.Property(x => x.ThoiGianLayMau).HasColumnName("thoi_gian_lay_mau").IsRequired();
            e.Property(x => x.BilirubinMgDl).HasColumnName("bilirubin_mgdl").HasPrecision(5, 2).IsRequired();
            e.Property(x => x.TuoiGioTuDong).HasColumnName("tuoi_gio_tu_dong");
            e.Property(x => x.MucDoNguyHiem).HasColumnName("muc_do_nguy_hiem").HasMaxLength(100);
            e.Property(x => x.NguongChieuDen).HasColumnName("nguong_chieu_den").HasPrecision(5, 2);
            e.Property(x => x.NguongThayCuuMau).HasColumnName("nguong_thay_cuu_mau").HasPrecision(5, 2);
            e.Property(x => x.NgayTao).HasColumnName("ngay_tao").IsRequired();
        });

        // ── ClinicalAuditLog ───────────────────────────────────────────
        modelBuilder.Entity<ClinicalAuditLog>(e =>
        {
            e.ToTable("clinical_audit_logs");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.CalculatedAt).HasColumnName("calculated_at").IsRequired();
            e.Property(a => a.GuidelineCode).HasColumnName("guideline_code").HasMaxLength(100).IsRequired();
            e.Property(a => a.EngineMode).HasColumnName("engine_mode").HasMaxLength(100).IsRequired();
            e.Property(a => a.UserId).HasColumnName("user_id").HasMaxLength(256);
            e.Property(a => a.ApiClientId).HasColumnName("api_client_id").HasMaxLength(256);
            e.Property(a => a.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
            e.Property(a => a.RequestJson).HasColumnName("request_json").HasColumnType("jsonb").IsRequired();
            e.Property(a => a.ResponseJson).HasColumnName("response_json").HasColumnType("jsonb").IsRequired();
            e.Property(a => a.TraceJson).HasColumnName("trace_json").HasColumnType("jsonb").IsRequired();
            e.HasIndex(a => a.CalculatedAt).HasDatabaseName("ix_clinical_audit_logs_calculated_at");
            e.HasIndex(a => a.GuidelineCode).HasDatabaseName("ix_clinical_audit_logs_guideline_code");
        });
    }
}
