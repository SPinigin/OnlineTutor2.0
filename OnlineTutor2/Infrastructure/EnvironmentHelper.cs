namespace OnlineTutor2.Infrastructure
{
    /// <summary>
    /// Вспомогательный класс для работы с переменными окружения
    /// </summary>
    public static class EnvironmentHelper
    {
        /// <summary>
        /// Получает текущее окружение приложения
        /// </summary>
        public static string GetEnvironment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Production"; // По умолчанию Production для безопасности
        }

        /// <summary>
        /// Проверяет, является ли текущее окружение Development
        /// </summary>
        public static bool IsDevelopment()
        {
            var env = GetEnvironment();
            return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Проверяет, является ли текущее окружение Production
        /// </summary>
        public static bool IsProduction()
        {
            var env = GetEnvironment();
            return string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Получает диагностическую информацию об окружении
        /// </summary>
        public static string GetEnvironmentDiagnostics()
        {
            var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var dotnetEnv = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var currentEnv = GetEnvironment();

            return $"ASPNETCORE_ENVIRONMENT: {aspnetEnv ?? "НЕ УСТАНОВЛЕНА"}, " +
                   $"DOTNET_ENVIRONMENT: {dotnetEnv ?? "НЕ УСТАНОВЛЕНА"}, " +
                   $"Текущее окружение: {currentEnv}";
        }
    }
}





