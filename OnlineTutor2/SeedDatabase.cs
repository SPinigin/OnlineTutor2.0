using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using Microsoft.AspNetCore.Identity;
using OnlineTutor2.Models;

namespace OnlineTutor2
{
    public class SeedDatabase
    {
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var services = new ServiceCollection();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                Console.WriteLine("Starting database seeding...");
                await DbInitializer.Initialize(scope.ServiceProvider);
                Console.WriteLine("Database seeding completed successfully!");
            }
        }
    }
}
