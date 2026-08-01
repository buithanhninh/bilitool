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
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<HisTenant> HisTenants => Set<HisTenant>();
    public DbSet<HisApiClient> HisApiClients => Set<HisApiClient>();
    public DbSet<HisIdempotencyRecord> HisIdempotencyRecords => Set<HisIdempotencyRecord>();
    public DbSet<HisWebhookSubscription> HisWebhookSubscriptions => Set<HisWebhookSubscription>();
    public DbSet<HisOutboxEvent> HisOutboxEvents => Set<HisOutboxEvent>();
    public DbSet<ClinicalAuditLegalHold> ClinicalAuditLegalHolds => Set<ClinicalAuditLegalHold>();
    public DbSet<ClinicalAuditPurgeReport> ClinicalAuditPurgeReports => Set<ClinicalAuditPurgeReport>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RejectAuditMutation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RejectAuditMutation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

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
            e.Property(h => h.IsTestData).HasColumnName("is_test_data").HasDefaultValue(false);

            e.HasMany(h => h.DsXetNghiem)
             .WithOne(x => x.BenhNhan)
             .HasForeignKey(x => x.BenhNhanId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(h => h.NguoiDungId).HasDatabaseName("ix_ho_so_benh_nhan_nguoi_dung_id");
            e.HasIndex(h => new { h.IsTestData, h.NgayTao }).HasDatabaseName("ix_ho_so_benh_nhan_test_ngay_tao");
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
            e.Property(a => a.EngineVersion).HasColumnName("engine_version").HasMaxLength(100).IsRequired();
            e.Property(a => a.TenantId).HasColumnName("tenant_id").HasMaxLength(64);
            e.Property(a => a.UserId).HasColumnName("user_id").HasMaxLength(256);
            e.Property(a => a.ApiClientId).HasColumnName("api_client_id").HasMaxLength(256);
            e.Property(a => a.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
            e.Property(a => a.ResultId).HasColumnName("result_id").HasMaxLength(64);
            e.Property(a => a.RequestJson).HasColumnName("request_json").HasColumnType("jsonb").IsRequired();
            e.Property(a => a.ResponseJson).HasColumnName("response_json").HasColumnType("jsonb").IsRequired();
            e.Property(a => a.TraceJson).HasColumnName("trace_json").HasColumnType("jsonb").IsRequired();
            e.HasIndex(a => a.CalculatedAt).HasDatabaseName("ix_clinical_audit_logs_calculated_at");
            e.HasIndex(a => a.GuidelineCode).HasDatabaseName("ix_clinical_audit_logs_guideline_code");
            e.HasIndex(a => new { a.TenantId, a.ResultId }).HasDatabaseName("ix_clinical_audit_logs_tenant_result");
        });

        modelBuilder.Entity<AdminAuditLog>(e =>
        {
            e.ToTable("admin_audit_logs");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.OccurredAt).HasColumnName("occurred_at").IsRequired();
            e.Property(a => a.ActorId).HasColumnName("actor_id").HasMaxLength(256).IsRequired();
            e.Property(a => a.ActorEmail).HasColumnName("actor_email").HasMaxLength(320).IsRequired();
            e.Property(a => a.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
            e.Property(a => a.TargetType).HasColumnName("target_type").HasMaxLength(100).IsRequired();
            e.Property(a => a.TargetId).HasColumnName("target_id").HasMaxLength(256);
            e.Property(a => a.Succeeded).HasColumnName("succeeded").IsRequired();
            e.Property(a => a.RemoteIp).HasColumnName("remote_ip").HasMaxLength(45);
            e.Property(a => a.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
            e.Property(a => a.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            e.HasIndex(a => a.OccurredAt).HasDatabaseName("ix_admin_audit_logs_occurred_at");
            e.HasIndex(a => new { a.ActorId, a.OccurredAt }).HasDatabaseName("ix_admin_audit_logs_actor_occurred_at");
            e.HasIndex(a => new { a.Action, a.OccurredAt }).HasDatabaseName("ix_admin_audit_logs_action_occurred_at");
        });

        modelBuilder.Entity<ClinicalAuditLegalHold>(e =>
        {
            e.ToTable("clinical_audit_legal_holds");
            e.HasKey(item => item.Id);
            e.Property(item => item.Id).HasColumnName("id");
            e.Property(item => item.TenantId).HasColumnName("tenant_id").HasMaxLength(64).IsRequired();
            e.Property(item => item.ResultId).HasColumnName("result_id").HasMaxLength(64);
            e.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
            e.Property(item => item.PlacedBy).HasColumnName("placed_by").HasMaxLength(256).IsRequired();
            e.Property(item => item.PlacedAt).HasColumnName("placed_at").IsRequired();
            e.Property(item => item.ReleasedBy).HasColumnName("released_by").HasMaxLength(256);
            e.Property(item => item.ReleasedAt).HasColumnName("released_at");
            e.HasIndex(item => new { item.TenantId, item.ResultId, item.ReleasedAt })
                .HasDatabaseName("ix_clinical_audit_legal_holds_scope");
        });

        modelBuilder.Entity<ClinicalAuditPurgeReport>(e =>
        {
            e.ToTable("clinical_audit_purge_reports");
            e.HasKey(item => item.Id);
            e.Property(item => item.Id).HasColumnName("id");
            e.Property(item => item.ExecutedAt).HasColumnName("executed_at").IsRequired();
            e.Property(item => item.CutoffAt).HasColumnName("cutoff_at").IsRequired();
            e.Property(item => item.DryRun).HasColumnName("dry_run").IsRequired();
            e.Property(item => item.EligibleCount).HasColumnName("eligible_count").IsRequired();
            e.Property(item => item.ProtectedByLegalHoldCount).HasColumnName("protected_by_legal_hold_count").IsRequired();
            e.Property(item => item.DeletedCount).HasColumnName("deleted_count").IsRequired();
            e.HasIndex(item => item.ExecutedAt).HasDatabaseName("ix_clinical_audit_purge_reports_executed_at");
        });

        modelBuilder.Entity<HisTenant>(e =>
        {
            e.ToTable("his_tenants");
            e.HasKey(tenant => tenant.Id);
            e.Property(tenant => tenant.Id).HasColumnName("id");
            e.Property(tenant => tenant.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            e.Property(tenant => tenant.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            e.Property(tenant => tenant.IsActive).HasColumnName("is_active").IsRequired();
            e.Property(tenant => tenant.CreatedAt).HasColumnName("created_at").IsRequired();
            e.HasIndex(tenant => tenant.Code).IsUnique().HasDatabaseName("ux_his_tenants_code");
        });

        modelBuilder.Entity<HisApiClient>(e =>
        {
            e.ToTable("his_api_clients");
            e.HasKey(client => client.Id);
            e.Property(client => client.Id).HasColumnName("id");
            e.Property(client => client.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(client => client.ClientCode).HasColumnName("client_code").HasMaxLength(64).IsRequired();
            e.Property(client => client.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
            e.Property(client => client.KeyFingerprint).HasColumnName("key_fingerprint").HasMaxLength(16).IsRequired();
            e.Property(client => client.ApiKeyHash).HasColumnName("api_key_hash").IsRequired();
            e.Property(client => client.PreviousKeyFingerprint).HasColumnName("previous_key_fingerprint").HasMaxLength(16);
            e.Property(client => client.PreviousApiKeyHash).HasColumnName("previous_api_key_hash");
            e.Property(client => client.PreviousKeyExpiresAt).HasColumnName("previous_key_expires_at");
            e.Property(client => client.RequireMutualTls).HasColumnName("require_mutual_tls").IsRequired();
            e.Property(client => client.CertificateFingerprint).HasColumnName("certificate_fingerprint").HasMaxLength(64);
            e.Property(client => client.PreviousCertificateFingerprint).HasColumnName("previous_certificate_fingerprint").HasMaxLength(64);
            e.Property(client => client.PreviousCertificateExpiresAt).HasColumnName("previous_certificate_expires_at");
            e.Property(client => client.Scopes).HasColumnName("scopes").HasMaxLength(512).IsRequired();
            e.Property(client => client.IsActive).HasColumnName("is_active").IsRequired();
            e.Property(client => client.ExpiresAt).HasColumnName("expires_at");
            e.Property(client => client.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(client => client.LastUsedAt).HasColumnName("last_used_at");
            e.HasIndex(client => client.KeyFingerprint).HasDatabaseName("ix_his_api_clients_key_fingerprint");
            e.HasIndex(client => client.PreviousKeyFingerprint).HasDatabaseName("ix_his_api_clients_previous_key_fingerprint");
            e.HasIndex(client => new { client.TenantId, client.ClientCode })
                .IsUnique().HasDatabaseName("ux_his_api_clients_tenant_client_code");
            e.HasOne(client => client.Tenant)
                .WithMany(tenant => tenant.ApiClients)
                .HasForeignKey(client => client.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HisIdempotencyRecord>(e =>
        {
            e.ToTable("his_idempotency_records");
            e.HasKey(record => record.Id);
            e.Property(record => record.Id).HasColumnName("id");
            e.Property(record => record.TenantId).HasColumnName("tenant_id").HasMaxLength(64).IsRequired();
            e.Property(record => record.ApiClientId).HasColumnName("api_client_id").HasMaxLength(64).IsRequired();
            e.Property(record => record.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
            e.Property(record => record.RequestHash).HasColumnName("request_hash").HasMaxLength(64).IsRequired();
            e.Property(record => record.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(record => record.ResultId).HasColumnName("result_id").HasMaxLength(64);
            e.Property(record => record.ResponseStatusCode).HasColumnName("response_status_code");
            e.Property(record => record.ResponseJson).HasColumnName("response_json").HasColumnType("jsonb");
            e.Property(record => record.ResponseContentType).HasColumnName("response_content_type").HasMaxLength(128);
            e.Property(record => record.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(record => record.CompletedAt).HasColumnName("completed_at");
            e.Property(record => record.ExpiresAt).HasColumnName("expires_at").IsRequired();
            e.HasIndex(record => new { record.TenantId, record.ApiClientId, record.IdempotencyKey })
                .IsUnique().HasDatabaseName("ux_his_idempotency_client_key");
            e.HasIndex(record => record.ExpiresAt).HasDatabaseName("ix_his_idempotency_expires_at");
        });

        modelBuilder.Entity<HisWebhookSubscription>(e =>
        {
            e.ToTable("his_webhook_subscriptions");
            e.HasKey(item => item.Id);
            e.Property(item => item.Id).HasColumnName("id");
            e.Property(item => item.TenantId).HasColumnName("tenant_id").HasMaxLength(64).IsRequired();
            e.Property(item => item.ApiClientId).HasColumnName("api_client_id").HasMaxLength(64).IsRequired();
            e.Property(item => item.EndpointUrl).HasColumnName("endpoint_url").HasMaxLength(2048).IsRequired();
            e.Property(item => item.SecretProtected).HasColumnName("secret_protected").IsRequired();
            e.Property(item => item.EventTypes).HasColumnName("event_types").HasMaxLength(512).IsRequired();
            e.Property(item => item.IsActive).HasColumnName("is_active").IsRequired();
            e.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            e.HasIndex(item => new { item.TenantId, item.ApiClientId, item.EndpointUrl })
                .IsUnique().HasDatabaseName("ux_his_webhook_subscription_endpoint");
        });

        modelBuilder.Entity<HisOutboxEvent>(e =>
        {
            e.ToTable("his_outbox_events");
            e.HasKey(item => item.Id);
            e.Property(item => item.Id).HasColumnName("id");
            e.Property(item => item.WebhookSubscriptionId).HasColumnName("webhook_subscription_id").IsRequired();
            e.Property(item => item.TenantId).HasColumnName("tenant_id").HasMaxLength(64).IsRequired();
            e.Property(item => item.ApiClientId).HasColumnName("api_client_id").HasMaxLength(64).IsRequired();
            e.Property(item => item.EventType).HasColumnName("event_type").HasMaxLength(128).IsRequired();
            e.Property(item => item.ResultId).HasColumnName("result_id").HasMaxLength(64).IsRequired();
            e.Property(item => item.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
            e.Property(item => item.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
            e.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(item => item.AttemptCount).HasColumnName("attempt_count").IsRequired();
            e.Property(item => item.NextAttemptAt).HasColumnName("next_attempt_at").IsRequired();
            e.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(item => item.DeliveredAt).HasColumnName("delivered_at");
            e.Property(item => item.LastError).HasColumnName("last_error").HasMaxLength(2000);
            e.Property(item => item.LockId).HasColumnName("lock_id").HasMaxLength(64);
            e.Property(item => item.LockedUntil).HasColumnName("locked_until");
            e.HasIndex(item => new { item.Status, item.NextAttemptAt }).HasDatabaseName("ix_his_outbox_delivery_queue");
            e.HasIndex(item => item.ResultId).HasDatabaseName("ix_his_outbox_result_id");
            e.HasOne(item => item.WebhookSubscription)
                .WithMany(item => item.OutboxEvents)
                .HasForeignKey(item => item.WebhookSubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void RejectAuditMutation()
    {
        var mutation = ChangeTracker.Entries()
            .FirstOrDefault(entry =>
                entry.Entity is ClinicalAuditLog or AdminAuditLog or ClinicalAuditPurgeReport &&
                entry.State is EntityState.Modified or EntityState.Deleted);
        if (mutation is not null)
            throw new InvalidOperationException($"Audit entity {mutation.Metadata.ClrType.Name} là immutable.");
    }
}
