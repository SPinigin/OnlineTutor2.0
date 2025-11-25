using NLog;
using NLog.Web;
using OnlineTutor2.Data;
using OnlineTutor2.Infrastructure;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.ConfigureLogging()
           .ConfigureEPPlus()
           .ConfigureDatabase()
           .ConfigureIdentity()
           .ConfigureApplicationServices();

    builder.Services.AddLogging(logging =>
    {
        logging.AddConsole();
        logging.AddDebug();
    });

    logger.Info("Диагностика окружения: {Diagnostics}", EnvironmentHelper.GetEnvironmentDiagnostics());
    
    logger.Info("Building application. Environment: {Environment}", builder.Environment.EnvironmentName);
    var app = builder.Build();
    
    logger.Info("Application environment: {Environment}, IsDevelopment: {IsDev}, IsProduction: {IsProd}", 
        app.Environment.EnvironmentName, 
        app.Environment.IsDevelopment(), 
        app.Environment.IsProduction());
    
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) &&
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")))
    {
        logger.Warn("ВНИМАНИЕ: Переменная окружения ASPNETCORE_ENVIRONMENT не установлена! " +
                    "Используется значение по умолчанию. Для Production установите переменную в web.config или системных настройках.");
    }

    using (var scope = app.Services.CreateScope())
    {
        await DbInitializer.Initialize(scope.ServiceProvider);
    }

    app.ConfigureMiddleware();

    logger.Info("Приложение готово к запуску");
    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Приложение завершилось с ошибкой");
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
