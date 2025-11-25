using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Web;
using OfficeOpenXml;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Hubs;
using OnlineTutor2.Models;
using OnlineTutor2.Services;

namespace OnlineTutor2.Infrastructure
{
    /// <summary>
    /// Расширения для конфигурации сервисов
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Настраивает логирование
        /// </summary>
        public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
        {
            var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
            
            try
            {
                logger.Info("Инициализация приложения");
                builder.Logging.ClearProviders();
                builder.Host.UseNLog();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка настройки логирования");
                throw;
            }

            return builder;
        }

        /// <summary>
        /// Настраивает EPPlus
        /// </summary>
        public static WebApplicationBuilder ConfigureEPPlus(this WebApplicationBuilder builder)
        {
            var logger = LogManager.GetCurrentClassLogger();
            
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                logger.Info("EPPlus license configured successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "EPPlus license configuration failed");
            }

            return builder;
        }

        /// <summary>
        /// Настраивает базу данных
        /// </summary>
        public static WebApplicationBuilder ConfigureDatabase(this WebApplicationBuilder builder)
        {
            var logger = LogManager.GetCurrentClassLogger();
            
            logger.Info("Configuring database connection");
            try
            {
                // Регистрируем фабрику для создания NLog.Logger
                builder.Services.AddSingleton<Func<Type, Logger>>(serviceProvider => (type) => 
                    LogManager.GetLogger(type.FullName ?? type.Name));
                
                // Регистрируем прямое подключение к БД через ADO.NET
                builder.Services.AddScoped<IDatabaseConnection>(serviceProvider =>
                {
                    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                    var logger = LogManager.GetLogger(typeof(DatabaseConnection).FullName ?? nameof(DatabaseConnection));
                    return new DatabaseConnection(configuration, logger);
                });
                
                // Регистрируем репозитории
                builder.Services.AddScoped<IStudentRepository, StudentRepository>();
                builder.Services.AddScoped<IClassRepository, ClassRepository>();
                builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
                builder.Services.AddScoped<IRegularTestRepository, RegularTestRepository>();
                builder.Services.AddScoped<ISpellingTestRepository, SpellingTestRepository>();
                builder.Services.AddScoped<IPunctuationTestRepository, PunctuationTestRepository>();
                builder.Services.AddScoped<IOrthoeopyTestRepository, OrthoeopyTestRepository>();
                builder.Services.AddScoped<IRegularQuestionRepository, RegularQuestionRepository>();
                builder.Services.AddScoped<IRegularAnswerRepository, RegularAnswerRepository>();
                builder.Services.AddScoped<IRegularQuestionOptionRepository, RegularQuestionOptionRepository>();
                
                // Результаты тестов
                builder.Services.AddScoped<IRegularTestResultRepository, RegularTestResultRepository>();
                builder.Services.AddScoped<ISpellingTestResultRepository, SpellingTestResultRepository>();
                builder.Services.AddScoped<IPunctuationTestResultRepository, PunctuationTestResultRepository>();
                builder.Services.AddScoped<IOrthoeopyTestResultRepository, OrthoeopyTestResultRepository>();
                
                // Вопросы тестов
                builder.Services.AddScoped<ISpellingQuestionRepository, SpellingQuestionRepository>();
                builder.Services.AddScoped<IPunctuationQuestionRepository, PunctuationQuestionRepository>();
                builder.Services.AddScoped<IOrthoeopyQuestionRepository, OrthoeopyQuestionRepository>();
                
                // Ответы на тесты
                builder.Services.AddScoped<ISpellingAnswerRepository, SpellingAnswerRepository>();
                builder.Services.AddScoped<IPunctuationAnswerRepository, PunctuationAnswerRepository>();
                builder.Services.AddScoped<IOrthoeopyAnswerRepository, OrthoeopyAnswerRepository>();
                
                // Связи Test-Class
                builder.Services.AddScoped<IRegularTestClassRepository, RegularTestClassRepository>();
                builder.Services.AddScoped<ISpellingTestClassRepository, SpellingTestClassRepository>();
                builder.Services.AddScoped<IPunctuationTestClassRepository, PunctuationTestClassRepository>();
                builder.Services.AddScoped<IOrthoeopyTestClassRepository, OrthoeopyTestClassRepository>();
                
                // Другие сущности
                builder.Services.AddScoped<IMaterialRepository, MaterialRepository>();
                builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
                builder.Services.AddScoped<IGradeRepository, GradeRepository>();
                builder.Services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
                builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
                builder.Services.AddScoped<ITestCategoryRepository, TestCategoryRepository>();
                builder.Services.AddScoped<ITestAssignmentRepository, TestAssignmentRepository>();
                builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();
                
                // ApplicationDbContext используется ТОЛЬКО для ASP.NET Core Identity
                // Все бизнес-данные обрабатываются через ADO.NET репозитории
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to configure database");
                throw;
            }

            return builder;
        }

        /// <summary>
        /// Настраивает Identity
        /// </summary>
        public static WebApplicationBuilder ConfigureIdentity(this WebApplicationBuilder builder)
        {
            var logger = LogManager.GetCurrentClassLogger();
            
            logger.Info("Configuring Identity");
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Настройки пароля
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                // Настройки пользователя
                options.User.RequireUniqueEmail = true;

                // Настройки подтверждения email
                options.SignIn.RequireConfirmedEmail = true;

                // Настройки подтверждения phoneNumber
                options.SignIn.RequireConfirmedPhoneNumber = false;

                // Настройки блокировки
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 10;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Настройка токена для сброса пароля
            builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromHours(24);
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(24);
                options.SlidingExpiration = true;
            });

            return builder;
        }

        /// <summary>
        /// Настраивает сервисы приложения
        /// </summary>
        public static WebApplicationBuilder ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            var logger = LogManager.GetCurrentClassLogger();
            
            logger.Info("Configuring application services");
            
            builder.Services.AddScoped<IStudentImportService, StudentImportService>();
            builder.Services.AddScoped<ISpellingQuestionImportService, SpellingQuestionImportService>();
            builder.Services.AddScoped<IPunctuationQuestionImportService, PunctuationQuestionImportService>();
            builder.Services.AddScoped<IOrthoeopyQuestionImportService, OrthoeopyQuestionImportService>();
            builder.Services.AddScoped<IAuditLogService, AuditLogService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IExportService, ExportService>();
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddSignalR();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddControllersWithViews(options =>
            {
                // Добавляем глобальный фильтр для обработки исключений
                options.Filters.Add<ExceptionHandlingAttribute>();
            });

            return builder;
        }

        /// <summary>
        /// Настраивает middleware
        /// </summary>
        public static WebApplication ConfigureMiddleware(this WebApplication app)
        {
            var logger = LogManager.GetCurrentClassLogger();
            
            // Логируем текущее окружение
            logger.Info("Текущее окружение: {Environment}", app.Environment.EnvironmentName);

            // Глобальный обработчик исключений (должен быть первым)
            app.UseGlobalExceptionHandler();

            // Обработка ошибок в зависимости от окружения
            if (app.Environment.IsDevelopment())
            {
                // В Development режиме используем стандартный Developer Exception Page
                app.UseDeveloperExceptionPage();
                logger.Info("Developer Exception Page включен");
            }
            else
            {
                // В Production/Staging используем кастомный обработчик ошибок
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "text/html";

                        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                        var exception = exceptionHandlerPathFeature?.Error;

                        if (exception != null)
                        {
                            logger.Error(exception, "Необработанное исключение. Path: {Path}", context.Request.Path);
                        }

                        if (!context.Response.HasStarted)
                        {
                            context.Response.Redirect($"/Home/Error?statusCode=500");
                        }
                    });
                });
                
                // HSTS только для Production
                if (app.Environment.IsProduction())
                {
                    app.UseHsts();
                }
                
                logger.Info("Production error handler включен");
            }

            // Обработка статус кодов (404, 403 и т.д.)
            app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseSession();
            app.UseAuthorization();
            app.MapHub<TestAnalyticsHub>("/hubs/testAnalytics");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            logger.Info("Приложение готово к запуску");
            
            return app;
        }
    }
}

