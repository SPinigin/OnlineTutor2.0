using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.Services;

var builder = WebApplication.CreateBuilder(args);

try
{
    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    Console.WriteLine("[DEBUG] EPPlus license configured successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] EPPlus license configuration failed: {ex.Message}");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

builder.Services.AddScoped<IStudentImportService, StudentImportService>();
builder.Services.AddScoped<ISpellingQuestionImportService, SpellingQuestionImportService>();
builder.Services.AddScoped<IPunctuationQuestionImportService, PunctuationQuestionImportService>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    await SeedRoles(serviceProvider);
    //await SeedTestCategories(serviceProvider);
}

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

app.Run();

async Task SeedRoles(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in ApplicationRoles.AllRoles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
//async Task SeedTestCategories(IServiceProvider serviceProvider)
//{
//    var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

//    if (!await context.TestCategories.AnyAsync())
//    {
//        var categories = new List<TestCategory>
//        {
//            new TestCategory
//            {
//                Name = "Тесты на орфографию",
//                Description = "Проверка знаний орфографии: безударные гласные, пропущенные буквы в словах",
//                IconClass = "fas fa-spell-check",
//                ColorClass = "bg-primary",
//                OrderIndex = 1,
//                IsActive = true
//            },
//            new TestCategory
//            {
//                Name = "Тесты на пунктуацию",
//                Description = "Проверка знаний правил пунктуации",
//                IconClass = "fas fa-edit",
//                ColorClass = "bg-primary",
//                OrderIndex = 2,
//                IsActive = true
//            },
//            new TestCategory
//            {
//                Name = "Тесты с выбором ответа",
//                Description = "Классические тесты с множественным выбором ответов",
//                IconClass = "fas fa-list-ul",
//                ColorClass = "bg-info",
//                OrderIndex = 3,
//                IsActive = true
//            },
//            new TestCategory
//            {
//                Name = "Свободные ответы",
//                Description = "Тесты с развернутыми ответами и эссе",
//                IconClass = "fas fa-edit",
//                ColorClass = "bg-warning",
//                OrderIndex = 4,
//                IsActive = true
//            }
//        };

//        context.TestCategories.AddRange(categories);
//        await context.SaveChangesAsync();
//    }
//}
