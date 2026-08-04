using Microsoft.AspNetCore.RateLimiting;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.EncryptionHelper;
using travelexpensemanagement.Common.GlobalFunction;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Middleware.GlobalErrorHandlingMiddleware;
using travelexpensemanagement.ModuleService;
using travelexpensemanagement.Repositories.Implementations;
using travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Implementations.Purchase.Transaction;
using travelexpensemanagement.Repositories.Implementations.QualityControl.Master;
using travelexpensemanagement.Repositories.Implementations.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Implementations.Weighbridge.Transaction;

// ADD THESE (Repository)
using travelexpensemanagement.Repositories.Interfaces;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction;
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
builder.Services.AddScoped<GlobalFunction>();

//Master page repositories
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
//Master page repositories
// Gete Entry Transaction repositories
builder.Services.AddScoped<ICourierTrackingEntryRepository, CourierTrackingEntryRepository>();
builder.Services.AddScoped<ICourierTrackingEntryListRepository, CourierTrackingEntryListRepository>();
builder.Services.AddScoped<IVehicleInwardRepository, VehicleInwardRepository>();
builder.Services.AddScoped<IVehicleInwardListRepository, VehicleInwardListRepository>();
builder.Services.AddScoped<ITransitEntryRepository, TransitEntryRepository>();
builder.Services.AddScoped<ITransitEntryListRepository, TransitEntryListRepository>();
builder.Services.AddScoped<IVisitorRepository, VisitorRepository>();
builder.Services.AddScoped<IVisitorListRepository, VisitorListRepository>();
builder.Services.AddScoped<IMiscConsumptionRepository, MiscConsumptionEntryRepository>();
builder.Services.AddScoped<IMiscConsumptionListRepository, MiscConsumptionListRepository>();
builder.Services.AddScoped<IStoreWeighbridgeEntryRepository, StoreWeighbridgeEntryRepository>();
builder.Services.AddScoped<IStoreWeighbridgeEntryListRepository, StoreWeighbridgeEntryListRepository>();
builder.Services.AddScoped<IQCTemperatureEntryRepository, QCTemperatureEntryRepository>();
builder.Services.AddScoped<IQCTemperatureEntryListRepository, QCTemperatureEntryListRepository>();
builder.Services.AddScoped<ILaminationQCEntryRepository, LaminationQCEntryRepository>();
builder.Services.AddScoped<IQCMasterRepository, QCMasterRepository>();
builder.Services.AddScoped<IQCMasterListRepository, QCMasterListRepository>();
builder.Services.AddScoped<ITapeAndFabricMasterListRepository, TapeAndFabricMasterListRepository>();
builder.Services.AddScoped<ITapeAndFabricMasterRepository, TapeAndFabricMasterRepository>();
builder.Services.AddScoped<IUOMMasterListRepository, UOMMasterListRepository>();
builder.Services.AddScoped<IUOMMasterRepository, UOMMasterRepository>();
builder.Services.AddScoped<IQCGroupMasterListRepository, QCGroupMasterListRepository>();
builder.Services.AddScoped<IQCGroupMasterRepository, QCGroupMasterRepository>();
builder.Services.AddScoped<IParameterMasterListRepository, ParameterMasterListRepository>();
builder.Services.AddScoped<IParameterMasterRepository, ParameterMasterRepository>();
builder.Services.AddScoped<IPurchaseRequestListRepository, PurchaseRequestListRepository>();
builder.Services.AddScoped<IPurchaseRequestRepository, PurchaseRequestRepository>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IPurchaseBillPassEntryRepository, PurchaseBillPassEntryRepository>();

// Gete Entry Transaction repositories



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
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(10);
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

app.UseMiddleware<SessionTimeoutMiddleware>();

app.UseAuthorization();
// Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();