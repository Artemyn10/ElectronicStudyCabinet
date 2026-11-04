using ElectronicStudyCabinet.Models;

namespace ElectronicStudyCabinet.Data;

public static class StudentQueries
{
    // Пользователи с ролью Student (преподаватели в выборку не попадают)
    public static IQueryable<ApplicationUser> OnlyStudents(this ApplicationDbContext db)
    {
        return db.Users.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
            && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Student)));
    }
}