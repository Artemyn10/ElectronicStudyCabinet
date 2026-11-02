using System.Threading.RateLimiting;
using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Не считать все не-nullable строки обязательными (мы задаём [Required] сами)
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

    // Защита от подделки запросов: токен проверяется для всех POST-запросов автоматически
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Запрет по умолчанию: любая страница требует входа,
// кроме тех, где явно указано [AllowAnonymous]
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
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

    options.Cookie.Name = "StudyCabinet.Auth";
    options.Cookie.HttpOnly = true;                              // недоступна скриптам на странице
    options.Cookie.SameSite = SameSiteMode.Lax;                  // не отправляется с чужих сайтов при POST
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // при HTTPS cookie помечается как защищённая
});

// Ограничение частоты запросов: не более 10 в минуту с одного адреса
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Сервисы бизнес-логики
builder.Services.AddScoped<IDisciplineService, DisciplineService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<ILectureService, LectureService>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IControlQuestionService, ControlQuestionService>();
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<ILectureAccessService, LectureAccessService>();
builder.Services.AddScoped<IStudentTestService, StudentTestService>();
builder.Services.AddScoped<ILectureProgressService, LectureProgressService>();
builder.Services.AddScoped<IControlAnswerService, ControlAnswerService>();
builder.Services.AddScoped<IStudentLectureService, StudentLectureService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ITeacherDashboardService, TeacherDashboardService>();
builder.Services.AddScoped<IStudentCabinetService, StudentCabinetService>();
builder.Services.AddScoped<ITeacherReportService, TeacherReportService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IManualUnlockService, ManualUnlockService>();

var app = builder.Build();

// Автоматическое создание базы, миграции, роли, пользователи и демонстрационные данные
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        await DbInitializer.InitializeAsync(scope.ServiceProvider);
        await DemoDataSeeder.SeedAsync(scope.ServiceProvider);
        await DemoDataSeeder.SeedExtrasAsync(scope.ServiceProvider);
        await DemoDataSeeder.SeedTestsAsync(scope.ServiceProvider);
        logger.LogInformation("База данных готова.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Не удалось подготовить базу данных.");
        throw;
    }
}

var redirectToHttps = app.Configuration.GetValue<bool>("Security:RedirectToHttps", true);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    if (redirectToHttps)
        app.UseHsts();
}

// Страницы для ошибок 400, 404, 429 и других
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");

// Заголовки безопасности
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    // Для выдаваемых файлов (PDF) политику содержимого не добавляем,
    // чтобы встроенный просмотрщик PDF в браузере работал
    if (!context.Request.Path.StartsWithSegments("/Materials"))
    {
        headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    }

    await next();
});

if (redirectToHttps)
    app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();