using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Не считать все не-nullable строки обязательными (мы задаём [Required] сами)
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Подключение к PostgreSQL через Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity: пользователи, роли, пароли
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Правила пароля
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    // Почта должна быть уникальной
    options.User.RequireUniqueEmail = true;

    // Блокировка после 5 неудачных попыток входа на 10 минут
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<RussianIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

// Настройки cookie авторизации
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Сервисы бизнес-логики
builder.Services.AddScoped<IDisciplineService, DisciplineService>();
builder.Services.AddScoped<ITopicService, TopicService>();

var app = builder.Build();

// Автоматическое создание базы, миграции, роли и первый преподаватель
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        await DbInitializer.InitializeAsync(scope.ServiceProvider);
        logger.LogInformation("База данных готова.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Не удалось подготовить базу данных.");
        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // кто вы? (читает cookie)
app.UseAuthorization();    // что вам можно?

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();