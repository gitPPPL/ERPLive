using Microsoft.AspNetCore.RateLimiting;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.ModuleService;
using travelexpensemanagement.Services;

// ADD THESE (Repository)
using travelexpensemanagement.Repositories.Interfaces;
using travelexpensemanagement.Repositories.Implementations;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.EncryptionHelper;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Middleware.GlobalErrorHandlingMiddleware;

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
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<ICourierTrackingEntryRepository, CourierTrackingEntryRepository>();

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
        limiter.PermitLimit = 2;
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

// ======================
// Routing
// ======================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();




//using Microsoft.AspNetCore.RateLimiting;
//using travelexpensemanagement.Controllers.DropdownService;
//using travelexpensemanagement.Controllers.Globalvariable;
//using travelexpensemanagement.Dbconnection;
//using travelexpensemanagement.DbHelper;
//using travelexpensemanagement.GlobalErrorHandlingMiddleware;
//using travelexpensemanagement.Helpers;
//using travelexpensemanagement.LogService;
//using travelexpensemanagement.ModuleService;
//using travelexpensemanagement.Services;

//var builder = WebApplication.CreateBuilder(args);

//// Add services
//builder.Services.AddControllersWithViews();
////builder.Services.AddHttpContextAccessor();

////builder.Services.AddDistributedMemoryCache();

//builder.Services.AddScoped<DataBaseConnection>();
//builder.Services.AddScoped<GlobalValidationdate>();
//builder.Services.AddHttpContextAccessor();
//builder.Services.AddScoped<DbHelper>();
////builder.Services.AddScoped<GlobalVariableService>();
//builder.Services.AddScoped<DropdownService>();
////builder.Services.AddScoped<ModuleService>();
//builder.Services.AddScoped<LogService>();
//builder.Services.AddScoped<ErrorLoggerService>();
//builder.Services.AddScoped<IMasterDataService, MasterDataService>();

//builder.Services.Configure<EncryptionSettings>(
//builder.Configuration.GetSection("EncryptionSettings"));
//builder.Services.AddScoped<EncryptionHelper>();



//builder.Services.AddDistributedMemoryCache();

//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(30);
//    options.Cookie.Name = ".TravelExpense.Session"; 

//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;

//    options.Cookie.SameSite = SameSiteMode.Lax; 
//    options.Cookie.SecurePolicy = CookieSecurePolicy.None; 
//});

//builder.Services.AddHttpContextAccessor();



//builder.Services.AddScoped<GlobalVariableService>();
//builder.Services.AddScoped<ModuleService>();



//builder.Services.AddRateLimiter(options =>
//{
//    options.AddFixedWindowLimiter("LoginLimiter", limiter =>
//    {
//        limiter.PermitLimit = 2;
//        limiter.Window = TimeSpan.FromMinutes(10);
//        limiter.QueueLimit = 0;
//    });

//    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
//});



//var app = builder.Build();

//// Use global error handling middleware
//app.UseMiddleware<GlobalErrorHandlingMiddleware>();

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();
//app.UseSession();
//app.UseRateLimiter();

//app.UseMiddleware<SessionTimeoutMiddleware>();
//app.UseAuthorization();


//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Login}/{action=Index}/{id?}");
//app.Run();