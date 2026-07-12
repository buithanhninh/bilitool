using BiliTool.Vn.Application;
using BiliTool.Vn.Infrastructure;
using BiliTool.Vn.Infrastructure.Persistence;
using BiliTool.Vn.Web.Localization;
using Blazored.Toast;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Serilog;
using BiliTool.Vn.Web.Security;
using BiliTool.Vn.Web.Services.Operations;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog Logging ───────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/bilitool-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// ── ForwardedHeaders (Cloudflare Tunnel / Reverse Proxy) ────────
// Cho phép app nhận biết scheme https từ header X-Forwarded-Proto
// do Cloudflare Tunnel gửi, để tạo đúng redirect_uri cho Google OAuth.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust tất cả proxies (Cloudflare Tunnel chạy local)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── Blazor Server & API ───────────────────────────────────────
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageApplicationModelConvention("/AdminLogin", model =>
        model.EndpointMetadata.Add(new EnableRateLimitingAttribute("AdminLoginPolicy")));
});
builder.Services.AddServerSideBlazor(options =>
{
    options.DisconnectedCircuitMaxRetained = 100;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    options.MaxBufferedUnacknowledgedRenderBatches = 10;
}).AddHubOptions(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.EnableDetailedErrors = false;
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 1024 * 1024;
});
builder.Services.AddControllers();

// ── Application + Infrastructure ─────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ── Blazored Toast ────────────────────────────────────────────
builder.Services.AddBlazoredToast();

// ── Authentication: Google OAuth + Cookie ─────────────────────
var googleClientId     = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var coGoogleAuth       = !string.IsNullOrWhiteSpace(googleClientId) && googleClientId != "YOUR_GOOGLE_CLIENT_ID";

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = coGoogleAuth
        ? GoogleDefaults.AuthenticationScheme
        : CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath         = "/dang-nhap";
    options.LogoutPath        = "/dang-xuat";
    options.Cookie.Name       = "BiliToolVn.Auth";
    options.ExpireTimeSpan    = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

if (coGoogleAuth)
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId     = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.CallbackPath = "/signin-google";

        // Claims cần thiết
        options.Scope.Add("email");
        options.Scope.Add("profile");

        // Lưu tokens để dùng sau nếu cần
        options.SaveTokens = true;

        // Map claim avatar (picture) từ Google
        options.ClaimActions.MapJsonKey("picture",   "picture");
        options.ClaimActions.MapJsonKey("locale",     "locale");
        options.ClaimActions.MapJsonKey("given_name", "given_name");
        options.ClaimActions.MapJsonKey("family_name","family_name");

        // Xử lý sau khi xác thực thành công
        options.Events.OnCreatingTicket = async ctx =>
        {
            var id = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (!string.IsNullOrEmpty(id))
            {
                var dbContext = ctx.HttpContext.RequestServices.GetRequiredService<BiliToolDbContext>();
                var user = await dbContext.HoSoNguoiDung.FindAsync(id);

                if (user != null)
                {
                    if (!user.IsActive)
                    {
                        ctx.Fail("Tài khoản của bạn đã bị quản trị viên khóa.");
                        return;
                    }

                    user.NgayDangNhapCuoi = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    // Lần đầu đăng nhập, tạo hồ sơ mới
                    var hoTen = ctx.Principal?.FindFirst("name")?.Value ?? email ?? "Người dùng";
                    var newUser = new BiliTool.Vn.Domain.Entities.HoSoNguoiDung
                    {
                        Id = id,
                        GoogleId = id,
                        Email = email ?? "",
                        HoTen = hoTen,
                        NgayTao = DateTime.UtcNow,
                        NgayDangNhapCuoi = DateTime.UtcNow,
                        IsActive = true,
                        IsEmailVerified = true, // Trusted from Google
                        NgayCapNhat = DateTime.UtcNow
                    };
                    dbContext.HoSoNguoiDung.Add(newUser);
                    await dbContext.SaveChangesAsync();
                }

                // Log for audit
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Người dùng đăng nhập Google: {Email} - ID: {Id}", email, id);
            }
        };
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminRead", policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));
    options.AddPolicy("AdminWrite", policy => policy.RequireAuthenticatedUser().RequireRole("SuperAdmin"));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AdminCredentialVerifier>();
builder.Services.AddSingleton<OperationalMetrics>();
builder.Services.AddHostedService<OperationalAlertService>();

// ── Cấu hình HttpContext cho Blazor ──────────────────────────
builder.Services.AddScoped<BiliTool.Vn.Web.Services.NguoiDungHienTaiService>();
builder.Services.AddScoped<BiliTool.Vn.Web.Services.PhienLamViecService>();

// Bản địa hóa - Translation
builder.Services.AddSingleton<BiliTool.Vn.Web.Services.TranslationService>();

// Đăng ký ApiKeyAuthFilter cho API HIS
builder.Services.AddScoped<BiliTool.Vn.Web.Filters.ApiKeyAuthFilter>();

// ── Rate Limiting (Chống Spam/DDoS API) ──────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ApiPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                AutoReplenishment = true
            }));
    options.AddPolicy("AdminLoginPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// ── Tự động migrate DB khi khởi động ─────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<BiliToolDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database migration hoàn tất.");
    }
    catch (Exception ex)
    {
        Log.Warning("Không thể kết nối database: {Error}. Ứng dụng tiếp tục chạy.", ex.Message);
    }
}

// ── Middleware Pipeline ───────────────────────────────────────
// PHẢI đặt đầu tiên để đọc X-Forwarded-Proto từ Cloudflare Tunnel
app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    if (context.Request.IsHttps)
    {
        context.Response.Headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/loi");
    // Không dùng HSTS và HttpsRedirection vì Cloudflare Tunnel handle HTTPS
    // app.UseHsts();
}

// ----- Thiết lập Đa ngôn ngữ (i18n): Cookie > query > browser language > Cloudflare country -----
var supportedCultures = new[] { "vi", "en", "fr" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("vi")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

var cookieProvider = localizationOptions.RequestCultureProviders.OfType<Microsoft.AspNetCore.Localization.CookieRequestCultureProvider>().FirstOrDefault();
var queryProvider = localizationOptions.RequestCultureProviders.OfType<Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider>().FirstOrDefault();
localizationOptions.RequestCultureProviders.Clear();

if (cookieProvider != null)
{
    localizationOptions.RequestCultureProviders.Add(cookieProvider);
}

if (queryProvider != null)
{
    localizationOptions.RequestCultureProviders.Add(queryProvider);
}

localizationOptions.RequestCultureProviders.Add(new SmartRequestCultureProvider());

app.UseRequestLocalization(localizationOptions);
// -----------------------------------------------------------

// Không redirect HTTPS - Cloudflare Tunnel đã xử lý SSL
// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.Use(async (context, next) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    await next();
    stopwatch.Stop();
    var bucket = context.Request.Path.StartsWithSegments("/admin") ? "/admin" :
        context.Request.Path.StartsWithSegments("/api") ? "/api" :
        context.Request.Path.StartsWithSegments("/health") ? "/health" : "/other";
    context.RequestServices.GetRequiredService<OperationalMetrics>().Record(bucket, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
});

// Sử dụng Rate Limiter sau UseRouting và trước các Endpoint mapping
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    await next();

    if (!context.Request.Path.StartsWithSegments("/admin") ||
        context.Request.Path.StartsWithSegments("/admin/login") ||
        context.User.Identity?.IsAuthenticated != true ||
        !context.User.IsInRole("Admin"))
    {
        return;
    }

    var audit = context.RequestServices.GetRequiredService<BiliTool.Vn.Application.Services.IAdminAuditService>();
    var actorId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    var actorEmail = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
    await audit.RecordAsync(
        actorId,
        actorEmail,
        context.Request.Method == HttpMethods.Get ? "admin.page.view" : "admin.request",
        "admin.route",
        context.Request.Path.Value,
        context.Response.StatusCode < 400,
        context.Connection.RemoteIpAddress?.ToString(),
        context.TraceIdentifier,
        context.RequestAborted);
});

app.MapGet("/health/live", () => Results.Ok(new
{
    status = "Healthy",
    service = "BiliTool.Vn",
    checkedAt = DateTimeOffset.UtcNow
}));

app.MapGet("/health/ready", async (IServiceProvider services) =>
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BiliToolDbContext>();
    var canConnect = await db.Database.CanConnectAsync();

    return canConnect
        ? Results.Ok(new { status = "Ready", checkedAt = DateTimeOffset.UtcNow })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/admin/operations/metrics", (OperationalMetrics metrics) => Results.Ok(metrics.Snapshot()))
    .RequireAuthorization("AdminRead");

app.MapRazorPages();
app.MapBlazorHub();
app.MapControllers();
app.MapFallbackToPage("/_Host");

Log.Information("BiliTool.Vn đang khởi động...");
await app.RunAsync();
