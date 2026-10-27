using ElectronicStudyCabinet.Models;
using Microsoft.AspNetCore.Identity;

namespace ElectronicStudyCabinet.Data;

public static class DbInitializer
{
    // Начальные учётные записи (демо-данные для учебного проекта)
    private record SeedUser(
        string UserName,
        string Email,
        string FullName,
        string Password,
        string Role);

    private static readonly SeedUser[] SeedUsers =
    {
        new("teacher", "teacher@example.com", "Иванов Иван Иванович", "Teacher123", AppRoles.Teacher),
        new("student", "student@example.com", "Петров Пётр Петрович", "Student123", AppRoles.Student)
    };

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // 1. Роли
        foreach (var role in new[] { AppRoles.Student, AppRoles.Teacher })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Создана роль {Role}.", role);
            }
        }

        // 2. Начальные пользователи (создаются, только если ещё не существуют)
        foreach (var seed in SeedUsers)
        {
            if (await userManager.FindByNameAsync(seed.UserName) != null)
                continue;

            var user = new ApplicationUser
            {
                UserName = seed.UserName,
                Email = seed.Email,
                FullName = seed.FullName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, seed.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                logger.LogError("Не удалось создать пользователя {UserName}: {Errors}", seed.UserName, errors);
                continue;
            }

            await userManager.AddToRoleAsync(user, seed.Role);
            logger.LogInformation("Создан пользователь {UserName} с ролью {Role}.", seed.UserName, seed.Role);
        }
    }
}