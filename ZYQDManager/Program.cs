using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Options;
using ZYQDManager.Components;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Services;
using ZYQDManager.Utilities;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(SelectOptions.FileName, optional: true, reloadOnChange: true);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<CircuitOptions>(options =>
{
    options.DetailedErrors = builder.Configuration.GetValue<bool>("CircuitOptions:DetailedErrors")
                             || builder.Environment.IsDevelopment();
});

const long excelUploadLimit = 32L * 1024 * 1024;
builder.Services.Configure<HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = excelUploadLimit;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = excelUploadLimit;
});

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = false;
});

builder.Services.AddControllers(options => { options.Filters.Add(new IgnoreAntiforgeryTokenAttribute()); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ZYQDManager API",
        Version = "v1",
        Description = "致优渠道管理 API"
    });
});

builder.Services.AddDbContext<SqlServerDbContext>(opt =>
{
    var conn = builder.Configuration.GetConnectionString("Sunlight_Management_BU");
    opt.UseSqlServer(conn);
});

builder.Services.AddAutoMapper(typeof(AutoMapperSetting));

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
};
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = jsonOptions.PropertyNameCaseInsensitive;
    options.SerializerOptions.PropertyNamingPolicy = jsonOptions.PropertyNamingPolicy;
    options.SerializerOptions.DictionaryKeyPolicy = jsonOptions.DictionaryKeyPolicy;
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("LegacyFiles", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseProxy = false,
        Proxy = null
    });
builder.Services.Configure<FileOperationOptions>(builder.Configuration.GetSection(FileOperationOptions.SectionName));
builder.Services.AddHttpClient(FileOperationOptions.HttpClientName, (sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<FileOperationOptions>>().Value;
        client.BaseAddress = new Uri(opts.ResolvedBaseUrl() + "/");
        client.Timeout = Timeout.InfiniteTimeSpan;
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseProxy = false,
        Proxy = null
    });
builder.Services.Configure<CentralApiOptions>(builder.Configuration.GetSection(CentralApiOptions.SectionName));
builder.Services.AddHttpClient(CentralApiOptions.HttpClientName, (sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<CentralApiOptions>>().Value;
        var baseUrl = (opts.BaseUrl ?? "").Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(baseUrl))
            client.BaseAddress = new Uri(baseUrl + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var opts = sp.GetRequiredService<IOptions<CentralApiOptions>>().Value;
        var handler = new HttpClientHandler
        {
            UseProxy = false,
            Proxy = null
        };
        if (opts.IgnoreSslCertificateErrors)
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return handler;
    });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<LocalStorageHelper>();
builder.Services.Configure<SelectOptions>(builder.Configuration.GetSection(SelectOptions.SectionName));
builder.Services.Configure<LegacyFilesOptions>(builder.Configuration.GetSection(LegacyFilesOptions.SectionName));
builder.Services.Configure<AdminNotifyOptions>(builder.Configuration.GetSection(AdminNotifyOptions.SectionName));
builder.Services.Configure<LibreOfficeOptions>(builder.Configuration.GetSection(LibreOfficeOptions.SectionName));
builder.Services.Configure<InvoiceKpOptions>(builder.Configuration.GetSection(InvoiceKpOptions.SectionName));
builder.Services.AddHttpClient(InvoiceKpOptions.RateHttpClientName, (sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<InvoiceKpOptions>>().Value;
        var baseUrl = (opts.RateApiBaseUrl ?? "").Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(baseUrl))
            client.BaseAddress = new Uri(baseUrl + "/");
        client.Timeout = TimeSpan.FromMinutes(2);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseProxy = false,
        Proxy = null,
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
builder.Services.AddSingleton<AdminEmailStore>();
builder.Services.AddScoped<IAppService, AppService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<CentralUserInformationService>();
builder.Services.AddScoped<CentralAuthService>();
builder.Services.AddScoped<CentralUserManagementService>();
builder.Services.AddScoped<UserCustomerAssignService>();
builder.Services.AddScoped<AdminNotifyService>();
builder.Services.AddScoped<ProjectListService>();
builder.Services.AddScoped<MotorLxService>();
builder.Services.AddScoped<FileOperationService>();
builder.Services.AddScoped<CotractBaseService>();
builder.Services.AddScoped<CrmCustomerService>();
builder.Services.AddScoped<CotractPiExcelService>();
builder.Services.AddScoped<LibreOfficePdfService>();
builder.Services.AddScoped<ExcelComPdfService>();
builder.Services.AddScoped<CotractPiService>();
builder.Services.AddScoped<InvoiceKpService>();
builder.Services.AddScoped<SnackbarHelper>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();
CotractPiTypes.Bind(app.Services.GetRequiredService<IOptionsMonitor<SelectOptions>>());

try
{
    using var schemaScope = app.Services.CreateScope();
    var db = schemaScope.ServiceProvider.GetRequiredService<SqlServerDbContext>();
    var schemaLog = schemaScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSchema");
    DbSchema.EnsureAsync(db, schemaLog).GetAwaiter().GetResult();
}
catch (Exception ex)
{
    Log.Warning(ex, "启动时检查数据表失败");
}

var fwd = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                       | ForwardedHeaders.XForwardedProto
                       | ForwardedHeaders.XForwardedHost
};
fwd.KnownNetworks.Clear();
fwd.KnownProxies.Clear();
app.UseForwardedHeaders(fwd);

app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        var scheme = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpReq.Scheme;
        var host = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault() ?? httpReq.Host.Value;
        var basePath = httpReq.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? httpReq.PathBase.Value;
        swagger.Servers = new List<OpenApiServer>
        {
            new() { Url = $"{scheme}://{host}{basePath}" }
        };
    });
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZYQDManager API V1");
    c.RoutePrefix = "swagger";
});

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsedMs, ex) =>
    {
        var path = httpContext.Request.Path.Value ?? "";
        if (path.StartsWith("/_blazor") || path.StartsWith("/_framework") ||
            path.EndsWith(".js") || path.EndsWith(".css") ||
            path.EndsWith(".png") || path.EndsWith(".jpg") ||
            path.EndsWith(".ico") || path.EndsWith(".map"))
        {
            return LogEventLevel.Verbose;
        }

        if (ex != null) return LogEventLevel.Error;
        if (elapsedMs > 500) return LogEventLevel.Warning;
        return LogEventLevel.Information;
    };
    options.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("RequestHost", http.Request.Host.Value ?? "");
        diag.Set("RequestScheme", http.Request.Scheme);
        diag.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
        diag.Set("RemoteIpAddress", http.Connection.RemoteIpAddress?.ToString() ?? "");
        diag.Set("RequestPath", http.Request.Path.Value ?? "");
        diag.Set("RequestMethod", http.Request.Method ?? "");
        diag.Set("ActionName", http.GetEndpoint()?.DisplayName ?? "");
        var wsMb = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024d / 1024d;
        diag.Set("MemoryUsageMB", Math.Round(wsMb, 1));
    };
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.MapGet("/api/db-test", async (IAppService appService, CancellationToken ct) =>
{
    var result = await appService.GetDbStatusAsync(ct);
    return Results.Json(result);
});

app.Run();
