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
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Serilog;
using BiliTool.Vn.Web.Security;
using BiliTool.Vn.Web.Services.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Http.Timeouts;

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
    options.ForwardLimit = builder.Configuration.GetValue("ReverseProxy:ForwardLimit", 2);
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    foreach (var value in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
    {
        if (IPAddress.TryParse(value, out var address)) options.KnownProxies.Add(address);
    }

    foreach (var value in builder.Configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? Array.Empty<string>())
    {
        var parts = value.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var prefix) && int.TryParse(parts[1], out var prefixLength))
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
    }
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
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, new BiliTool.Vn.Web.Services.Hl7.Hl7V2InputFormatter());
}).ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Giá trị JSON không hợp lệ." : error.ErrorMessage).ToArray());
        var problem = new ProblemDetails
        {
            Type = "https://bilitool.vn/problems/invalid_json",
            Title = "invalid_json",
            Status = StatusCodes.Status400BadRequest,
            Detail = "JSON request không hợp lệ hoặc chứa trường không được hỗ trợ.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["errorCode"] = "invalid_json";
        problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
        problem.Extensions["retryable"] = false;
        problem.Extensions["errors"] = errors;
        return new BadRequestObjectResult(problem);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRequestTimeouts(options =>
    options.AddPolicy("HisApi", TimeSpan.FromSeconds(
        Math.Clamp(builder.Configuration.GetValue("Operations:HisRequestTimeoutSeconds", 5), 1, 30))));
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v3", new OpenApiInfo
    {
        Title = "BiliTool.Vn HIS/EMR Clinical API",
        Version = "v3",
        Description = "Canonical production contract for bilirubin clinical decision support."
    });
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Description = "Tenant-scoped HIS/EMR API credential."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("ApiKey", document, null)] = new List<string>()
    });
});

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
builder.Services.AddSingleton<BiliTool.Vn.Application.Services.IHisIntegrationMetrics>(services =>
    services.GetRequiredService<OperationalMetrics>());
builder.Services.AddHostedService<OperationalAlertService>();
builder.Services.AddScoped<HisOperationalHealthService>();

// ── Cấu hình HttpContext cho Blazor ──────────────────────────
builder.Services.AddScoped<BiliTool.Vn.Web.Services.NguoiDungHienTaiService>();
builder.Services.AddScoped<BiliTool.Vn.Web.Services.PhienLamViecService>();

// Bản địa hóa - Translation
builder.Services.AddSingleton<BiliTool.Vn.Web.Services.TranslationService>();

// Đăng ký ApiKeyAuthFilter cho API HIS
builder.Services.AddScoped<BiliTool.Vn.Web.Filters.ApiKeyAuthFilter>();
builder.Services.AddScoped<BiliTool.Vn.Web.Filters.HisIdempotencyFilter>();
builder.Services.AddScoped<BiliTool.Vn.Web.Filters.HisRolloutFilter>();
builder.Services.AddSingleton<BiliTool.Vn.Web.Services.Fhir.FhirR4BilirubinBundleAdapter>();
builder.Services.AddSingleton<BiliTool.Vn.Web.Services.Hl7.Hl7V251OruAdapter>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BiliTool.Vn.Application.Services.IClinicalRequestContext, BiliTool.Vn.Web.Services.ClinicalRequestContext>();

// ── Rate Limiting (Chống Spam/DDoS API) ──────────────────────
builder.Services.AddRateLimiter(options =>
{
    var hisPermitLimit = Math.Clamp(builder.Configuration.GetValue("Operations:HisRateLimitPermit", 30), 1, 10_000);
    var hisWindowSeconds = Math.Clamp(builder.Configuration.GetValue("Operations:HisRateLimitWindowSeconds", 60), 1, 3600);
    options.AddPolicy("ApiPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetHisRateLimitPartition(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = hisPermitLimit,
                Window = TimeSpan.FromSeconds(hisWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
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
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        context.HttpContext.Response.ContentType = "application/problem+json";
        var problem = new
        {
            type = "https://bilitool.vn/problems/rate_limit_exceeded",
            title = "rate_limit_exceeded",
            status = StatusCodes.Status429TooManyRequests,
            detail = "Đã vượt giới hạn request cho API client.",
            errorCode = "rate_limit_exceeded",
            correlationId = context.HttpContext.TraceIdentifier,
            retryable = true
        };
        await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(problem), cancellationToken);
    };
});

var app = builder.Build();

// ── Tự động migrate DB khi khởi động ─────────────────────────
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
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
    context.Response.Headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
    context.Response.Headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");
    context.Response.Headers.TryAdd(
        "Content-Security-Policy",
        "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; " +
        "form-action 'self' https://accounts.google.com; img-src 'self' data: https:; " +
        "font-src 'self' data: https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
        "connect-src 'self' ws: wss:; manifest-src 'self'; worker-src 'self'");
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
app.UseSwagger(options =>
{
    options.RouteTemplate = "openapi/{documentName}.json";
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
});
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v3.json", "BiliTool.Vn HIS/EMR API v3");
    options.RoutePrefix = "openapi";
});
app.UseRouting();
app.Use(async (context, next) =>
{
    var requestLimit = context.Request.Path.StartsWithSegments("/api/v3/clinical/bilirubin/calculate")
        ? 64 * 1024L
        : context.Request.Path.StartsWithSegments("/api/v3/fhir/R4/$bilirubin-calculate") ||
          context.Request.Path.StartsWithSegments("/api/v3/hl7/v251/oru-r01")
            ? 128 * 1024L
            : (long?)null;

    if (requestLimit is null)
    {
        await next();
        return;
    }

    var sizeFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
    if (sizeFeature is { IsReadOnly: false }) sizeFeature.MaxRequestBodySize = requestLimit;

    if (context.Request.ContentLength <= requestLimit)
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new
    {
        type = "https://bilitool.vn/problems/request_too_large",
        title = "request_too_large",
        status = StatusCodes.Status413PayloadTooLarge,
        detail = $"Request body vượt giới hạn {requestLimit} bytes.",
        errorCode = "request_too_large",
        correlationId = context.TraceIdentifier,
        retryable = false
    });
});
app.UseRequestTimeouts();

app.Use(async (context, next) =>
{
    const string headerName = "X-Correlation-ID";
    var supplied = context.Request.Headers[headerName].ToString().Trim();
    context.TraceIdentifier = IsValidCorrelationId(supplied)
        ? supplied
        : $"req_{Guid.NewGuid():N}";
    context.Response.Headers[headerName] = context.TraceIdentifier;
    await next();
});

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

app.MapGet("/health/ready", async (HisOperationalHealthService health, CancellationToken cancellationToken) =>
{
    var snapshot = await health.CheckAsync(cancellationToken);
    return snapshot.Ready
        ? Results.Ok(new
        {
            status = "Ready",
            clinicalEngine = snapshot.ClinicalEngine,
            checkedAt = snapshot.CheckedAt
        })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/admin/operations/health", async (HisOperationalHealthService health, CancellationToken cancellationToken) =>
        Results.Ok(await health.CheckAsync(cancellationToken)))
    .RequireAuthorization("AdminRead");

app.MapGet("/admin/operations/metrics", (OperationalMetrics metrics) => Results.Ok(metrics.Snapshot()))
    .RequireAuthorization("AdminRead");

app.MapPost("/admin/operations/outbox/{eventId:guid}/replay", async (
    Guid eventId,
    HttpContext context,
    BiliTool.Vn.Application.Services.IHisOutboxOperationsService outboxOperations,
    BiliTool.Vn.Application.Services.IAdminAuditService audit,
    CancellationToken cancellationToken) =>
{
    var result = await outboxOperations.ReplayDeadLetterAsync(eventId, cancellationToken);
    var succeeded = result == BiliTool.Vn.Application.Services.HisOutboxReplayResult.Replayed;
    await audit.RecordAsync(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown",
        context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty,
        "his.outbox.dead_letter.replay",
        "his.outbox_event",
        eventId.ToString("N"),
        succeeded,
        context.Connection.RemoteIpAddress?.ToString(),
        context.TraceIdentifier,
        cancellationToken);

    return result switch
    {
        BiliTool.Vn.Application.Services.HisOutboxReplayResult.Replayed => Results.Accepted($"/admin/operations/outbox/{eventId}"),
        BiliTool.Vn.Application.Services.HisOutboxReplayResult.NotFound => Results.NotFound(),
        BiliTool.Vn.Application.Services.HisOutboxReplayResult.NotDeadLetter => Results.Conflict(new { errorCode = "outbox_event_not_dead_letter" }),
        BiliTool.Vn.Application.Services.HisOutboxReplayResult.SubscriptionInactive => Results.Conflict(new { errorCode = "webhook_subscription_inactive" }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };
}).RequireAuthorization("AdminRead");

app.MapPost("/admin/operations/audit/legal-holds", async (
    ClinicalAuditLegalHoldRequest request,
    HttpContext context,
    BiliTool.Vn.Application.Services.IClinicalAuditGovernanceService governance,
    BiliTool.Vn.Application.Services.IAdminAuditService audit,
    CancellationToken cancellationToken) =>
{
    var actorId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    try
    {
        var holdId = await governance.PlaceLegalHoldAsync(
            request.TenantId,
            request.ResultId,
            request.Reason,
            actorId,
            cancellationToken);
        await audit.RecordAsync(
            actorId,
            context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty,
            "clinical.audit.legal_hold.place",
            "clinical_audit_legal_hold",
            holdId.ToString("N"),
            true,
            context.Connection.RemoteIpAddress?.ToString(),
            context.TraceIdentifier,
            cancellationToken);
        return Results.Created($"/admin/operations/audit/legal-holds/{holdId}", new { holdId });
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { errorCode = "invalid_legal_hold", detail = exception.Message });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { errorCode = "legal_hold_already_active", detail = exception.Message });
    }
}).RequireAuthorization("AdminRead");

app.MapDelete("/admin/operations/audit/legal-holds/{holdId:guid}", async (
    Guid holdId,
    HttpContext context,
    BiliTool.Vn.Application.Services.IClinicalAuditGovernanceService governance,
    BiliTool.Vn.Application.Services.IAdminAuditService audit,
    CancellationToken cancellationToken) =>
{
    var actorId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    var released = await governance.ReleaseLegalHoldAsync(holdId, actorId, cancellationToken);
    await audit.RecordAsync(
        actorId,
        context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty,
        "clinical.audit.legal_hold.release",
        "clinical_audit_legal_hold",
        holdId.ToString("N"),
        released,
        context.Connection.RemoteIpAddress?.ToString(),
        context.TraceIdentifier,
        cancellationToken);
    return released ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization("AdminRead");

app.MapPost("/admin/operations/audit/retention/dry-run", async (
    HttpContext context,
    BiliTool.Vn.Application.Services.IClinicalAuditGovernanceService governance,
    BiliTool.Vn.Application.Services.IAdminAuditService audit,
    CancellationToken cancellationToken) =>
{
    var actorId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    var report = await governance.RunRetentionAsync(true, cancellationToken);
    await audit.RecordAsync(
        actorId,
        context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty,
        "clinical.audit.retention.dry_run",
        "clinical_audit_purge_report",
        report.ReportId.ToString("N"),
        true,
        context.Connection.RemoteIpAddress?.ToString(),
        context.TraceIdentifier,
        cancellationToken);
    return Results.Ok(report);
}).RequireAuthorization("AdminRead");

app.MapPost("/admin/operations/his-clients", async (
    HisClientProvisionRequest request,
    HttpContext context,
    BiliTool.Vn.Application.Services.IHisClientProvisioningService provisioning,
    BiliTool.Vn.Application.Services.IAdminAuditService audit,
    CancellationToken cancellationToken) =>
{
    var actorId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    try
    {
        await provisioning.ProvisionAsync(new BiliTool.Vn.Application.Services.HisClientProvisioningRequest(
            request.TenantCode,
            request.TenantName,
            request.ClientCode,
            request.DisplayName,
            request.ApiKey,
            request.Scopes,
            request.ExpiresAt,
            request.RequireMutualTls,
            request.CertificateFingerprint), cancellationToken);
        await audit.RecordAsync(
            actorId,
            context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty,
            "his.client.provision",
            "his_api_client",
            $"{request.TenantCode.Trim().ToLowerInvariant()}/{request.ClientCode.Trim().ToLowerInvariant()}",
            true,
            context.Connection.RemoteIpAddress?.ToString(),
            context.TraceIdentifier,
            cancellationToken);
        return Results.Created("/admin/operations/his-clients", new
        {
            tenantCode = request.TenantCode.Trim().ToLowerInvariant(),
            clientCode = request.ClientCode.Trim().ToLowerInvariant()
        });
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { errorCode = "invalid_his_client", detail = exception.Message });
    }
}).RequireAuthorization("AdminRead");

app.MapPost("/admin/operations/his-clients/{tenantCode}/{clientCode}/rotate-certificate", async (
    string tenantCode,
    string clientCode,
    HisClientCertificateRotateRequest request,
    HttpContext context,
    BiliTool.Vn.Application.Services.IHisClientProvisioningService provisioning,
    BiliTool.Vn.Application.Services.IAdminAuditService audit,
    CancellationToken cancellationToken) =>
{
    var actorId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    try
    {
        await provisioning.RotateCertificateAsync(
            tenantCode,
            clientCode,
            request.NewCertificateFingerprint,
            TimeSpan.FromMinutes(request.OverlapMinutes),
            cancellationToken);
        await audit.RecordAsync(
            actorId,
            context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty,
            "his.client.rotate_certificate",
            "his_api_client",
            $"{tenantCode}/{clientCode}",
            true,
            context.Connection.RemoteIpAddress?.ToString(),
            context.TraceIdentifier,
            cancellationToken);
        return Results.NoContent();
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { errorCode = "invalid_his_client_certificate", detail = exception.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization("AdminRead");

app.MapPost("/admin/operations/his-clients/{tenantCode}/{clientCode}/rotate", async (
    string tenantCode,
    string clientCode,
    HisClientRotateRequest request,
    HttpContext context,
    BiliTool.Vn.Application.Services.IHisClientProvisioningService provisioning,
    BiliTool.Vn.Application.Services.IAdminAuditService audit,
    CancellationToken cancellationToken) =>
{
    var actorId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    try
    {
        await provisioning.RotateKeyAsync(
            tenantCode,
            clientCode,
            request.NewApiKey,
            TimeSpan.FromMinutes(request.OverlapMinutes),
            cancellationToken);
        await audit.RecordAsync(
            actorId,
            context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty,
            "his.client.rotate_key",
            "his_api_client",
            $"{tenantCode}/{clientCode}",
            true,
            context.Connection.RemoteIpAddress?.ToString(),
            context.TraceIdentifier,
            cancellationToken);
        return Results.NoContent();
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { errorCode = "invalid_his_client_rotation", detail = exception.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization("AdminRead");

app.MapDelete("/admin/operations/his-clients/{tenantCode}/{clientCode}", async (
    string tenantCode,
    string clientCode,
    HttpContext context,
    BiliTool.Vn.Application.Services.IHisClientProvisioningService provisioning,
    BiliTool.Vn.Application.Services.IAdminAuditService audit,
    CancellationToken cancellationToken) =>
{
    var actorId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    try
    {
        await provisioning.RevokeAsync(tenantCode, clientCode, cancellationToken);
        await audit.RecordAsync(
            actorId,
            context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty,
            "his.client.revoke",
            "his_api_client",
            $"{tenantCode}/{clientCode}",
            true,
            context.Connection.RemoteIpAddress?.ToString(),
            context.TraceIdentifier,
            cancellationToken);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization("AdminRead");

app.MapRazorPages();
app.MapBlazorHub();
app.MapControllers();
app.MapFallbackToPage("/_Host");

Log.Information("BiliTool.Vn đang khởi động...");
await app.RunAsync();

static bool IsValidCorrelationId(string value)
{
    if (string.IsNullOrWhiteSpace(value) || value.Length > 64) return false;
    return value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');
}

static string GetHisRateLimitPartition(HttpContext context)
{
    var apiKey = context.Request.Headers["X-API-Key"].ToString().Trim();
    if (!string.IsNullOrEmpty(apiKey))
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return $"client:{Convert.ToHexString(hash.AsSpan(0, 8))}";
    }

    return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

public partial class Program;

public sealed record ClinicalAuditLegalHoldRequest(string TenantId, string? ResultId, string Reason);
public sealed record HisClientProvisionRequest(
    string TenantCode,
    string TenantName,
    string ClientCode,
    string DisplayName,
    string ApiKey,
    string Scopes,
    DateTime? ExpiresAt,
    bool RequireMutualTls = false,
    string? CertificateFingerprint = null);
public sealed record HisClientRotateRequest(string NewApiKey, int OverlapMinutes);
public sealed record HisClientCertificateRotateRequest(string NewCertificateFingerprint, int OverlapMinutes);
