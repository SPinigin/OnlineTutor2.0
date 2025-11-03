using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using OfficeOpenXml;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.Services;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Запуск приложения");

    var builder = WebApplication.CreateBuilder(args);

    // Настройка NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // EPPlus License
    try
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        logger.Info("EPPlus license configured successfully");
    }
    catch (Exception ex)
    {
        logger.Error(ex, "EPPlus license configuration failed");
    }

    // Database Context
    logger.Info("Configuring database connection");
    try
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    }
    catch (Exception ex)
    {
        logger.Error(ex, "Failed to configure database");
        throw;
    }

    // Identity
    logger.Info("Configuring Identity");
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.AddLogging(logging =>
    {
        logging.AddConsole();
        logging.AddDebug();
    });

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

    // Services
    logger.Info("Configuring application services");
    builder.Services.AddScoped<IStudentImportService, StudentImportService>();
    builder.Services.AddScoped<ISpellingQuestionImportService, SpellingQuestionImportService>();
    builder.Services.AddScoped<IPunctuationQuestionImportService, PunctuationQuestionImportService>();
    builder.Services.AddScoped<IOrthoeopyQuestionImportService, OrthoeopyQuestionImportService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();

    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    builder.Services.AddControllersWithViews();

    logger.Info("Building application");
    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var serviceProvider = scope.ServiceProvider;
        await DbInitializer.Initialize(scope.ServiceProvider);
    }

    // Middleware
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseSession();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    logger.Info("Приложение успешно запущено");
    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Приложение остановлено из-за критической ошибки");
    if (ex.InnerException != null)
    {
        logger.Error(ex.InnerException, "Inner exception details");
    }
    throw;
}
finally
{
    LogManager.Shutdown();
}
