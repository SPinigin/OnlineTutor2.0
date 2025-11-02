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
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            logger.Info($"Connection string: {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");
            options.UseSqlServer(connectionString);
        });
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

    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    builder.Services.AddControllersWithViews();

    logger.Info("Building application");
    var app = builder.Build();

    // Database initialization
    logger.Info("Initializing database");
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var serviceProvider = scope.ServiceProvider;

            // Проверка подключения к базе данных
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            logger.Info("Checking database connection...");

            if (context.Database.GetPendingMigrations().Any())
            {
                logger.Warn("Pending migrations found. Applying migrations...");
                context.Database.Migrate();
                logger.Info("Migrations applied successfully");
            }
            else
            {
                logger.Info("Database is up to date");
            }

            // Инициализация ролей и категорий (раскомментируйте когда будете готовы)
            // await SeedRoles(serviceProvider);
            // await SeedTestCategories(serviceProvider);
        }
    }
    catch (Exception ex)
    {
        logger.Error(ex, "An error occurred while initializing the database");
        throw;
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
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}

// Методы инициализации (раскомментируйте когда будете готовы использовать)
async Task SeedRoles(IServiceProvider serviceProvider)
{
    var logger = LogManager.GetCurrentClassLogger();
    try
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        logger.Info("Seeding roles...");

        foreach (var role in ApplicationRoles.AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                {
                    logger.Info($"Role '{role}' created successfully");
                }
                else
                {
                    logger.Error($"Failed to create role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.Info($"Role '{role}' already exists");
            }
        }
    }
    catch (Exception ex)
    {
        logger.Error(ex, "Error occurred while seeding roles");
        throw;
    }
}

async Task SeedTestCategories(IServiceProvider serviceProvider)
{
    var logger = LogManager.GetCurrentClassLogger();
    try
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        logger.Info("Seeding test categories...");

        if (!context.TestCategories.Any())
        {
            var categories = new List<TestCategory>
            {
                new TestCategory
                {
                    Name = "Орфография",
                    Description = "Тесты на правописание слов",
                    IconClass = "fas fa-spell-check",
                    ColorClass = "bg-primary",
                    OrderIndex = 1,
                    IsActive = true
                },
                new TestCategory
                {
                    Name = "Пунктуация",
                    Description = "Тесты на знаки препинания",
                    IconClass = "fas fa-quote-right",
                    ColorClass = "bg-danger",
                    OrderIndex = 2,
                    IsActive = true
                },
                new TestCategory
                {
                    Name = "Орфоэпия",
                    Description = "Тесты на правильное ударение",
                    IconClass = "fas fa-volume-up",
                    ColorClass = "bg-warning",
                    OrderIndex = 3,
                    IsActive = true
                },
                new TestCategory
                {
                    Name = "Классические",
                    Description = "Тесты с выбором ответов",
                    IconClass = "fas fa-list-ul",
                    ColorClass = "bg-warning",
                    OrderIndex = 5,
                    IsActive = true
                },
                new TestCategory
                {
                    Name = "Свободные ответы",
                    Description = "Тесты с развернутыми ответами и эссе",
                    IconClass = "fas fa-edit",
                    ColorClass = "bg-warning",
                    OrderIndex = 6,
                    IsActive = true
                },
                new TestCategory
                {
                    Name = "Средства выразительности",
                    Description = "Тесты по средствам выразительности",
                    IconClass = "fas fa-edit",
                    ColorClass = "bg-danger",
                    OrderIndex = 4,
                    IsActive = true
                }
            };

            context.TestCategories.AddRange(categories);
            await context.SaveChangesAsync();
            logger.Info($"Successfully seeded {categories.Count} test categories");
        }
        else
        {
            logger.Info("Test categories already exist");
        }
    }
    catch (Exception ex)
    {
        logger.Error(ex, "Error occurred while seeding test categories");
        throw;
    }
}
