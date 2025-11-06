using Microsoft.AspNetCore.Identity;
using NLog;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data
{
    public static class DbInitializer
    {
        private static readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();

        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Применяем миграции
            //await context.Database.MigrateAsync();

            // Заполняем роли
            //await SeedRoles(roleManager);

            // Заполняем категории
            //await SeedTestCategories(context);
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            try
            {
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

        private static async Task SeedTestCategories(ApplicationDbContext context)
        {
            try
            {
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
    }
}
