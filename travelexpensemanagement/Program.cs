using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.RateLimiting;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.EncryptionHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Middleware.GlobalErrorHandlingMiddleware;
using travelexpensemanagement.ModuleService;
using travelexpensemanagement.Repositories.Implementations;
using travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Implementations.Inventory.Transaction;
using travelexpensemanagement.Repositories.Implementations.QualityControl;
using travelexpensemanagement.Repositories.Implementations.Weighbridge;
using travelexpensemanagement.Repositories.Interfaces;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge;
using travelexpensemanagement.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<DataBaseConnection>();
builder.Services.AddScoped<GlobalValidationdate>();
builder.Services.AddScoped<DbHelper>();
builder.Services.AddScoped<DropdownService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<ErrorLoggerService>();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();

//  Repository Registration (IMPORTANT)

builder.Services.AddScoped< travelexpensemanagement.Repositories.Interfaces.IApprovalService, travelexpensemanagement.Repositories.Implementations.ApprovalService>();

builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IOutwardEntryRepository, OutwardEntryRepository>();
builder.Services.AddScoped<IOutwardEntryListRepository, OutwardEntryListRepository>();
builder.Services.AddScoped<IInwardEntryRepository, InwardEntryRepository>();
builder.Services.AddScoped<IInHouseWeighbridgeEntryRepository, InHouseWeighbridgeEntryRepository>();
builder.Services.AddScoped<IFlakesQCEntryRepository, FlakesQCEntryRepository>();
builder.Services.AddScoped<IFlakesQCEntryListRepository, FlakesQCEntryListRepository>();
builder.Services.AddScoped<IFlakesQCEntryExcluRepository, FlakesQCEntryExcluRepository>();
builder.Services.AddScoped<IFlakesQCEntryExcluListRepository, FlakesQCEntryExcluListRepository>();
builder.Services.AddScoped<ITransitEntryRepository, TransitEntryRepository>();
builder.Services.AddScoped<ITransitEntryListRepository, TransitEntryListRepository>();
builder.Services.AddScoped<IInventoryDepartmentIssueRepository,  InventoryDepartmentIssueRepository>();
builder.Services.AddScoped< IInventoryDepartmentIssueListRepository, InventoryDepartmentIssuelistRepository>();

builder.Services.Configure<EncryptionSettings>(
builder.Configuration.GetSection("EncryptionSettings"));
builder.Services.AddScoped<EncryptionHelper>();
builder.Services.AddDistributedMemoryCache();
// Session Configuration

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".TravelExpense.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GlobalVariableService>();
builder.Services.AddScoped<ModuleService>();

// Rate Limiter

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginLimiter", limiter =>
    {
        limiter.PermitLimit = 6;
        limiter.Window = TimeSpan.FromMinutes(30);
        limiter.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// Middleware Pipeline
app.UseMiddleware<GlobalErrorHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseRateLimiter();
//app.UseMiddleware<SessionTimeoutMiddleware>();
app.UseAuthorization();

// ======================
// Routing
// ======================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();