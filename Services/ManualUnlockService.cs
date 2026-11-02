using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public enum UnlockOutcome
{
    Ok,               // лекция открыта
    NotFound,         // студент или лекция не найдены
    NotPublished,     // лекция или дисциплина не опубликованы
    AlreadyAvailable  // лекция и так доступна студенту
}

public interface IManualUnlockService
{
    Task<UnlockOutcome> UnlockAsync(string teacherId, string studentId, int lectureId, string? reason);
    Task<bool> RevokeAsync(string teacherId, string studentId, int lectureId);
}

public class ManualUnlockService : IManualUnlockService
{
    private readonly ApplicationDbContext _db;
    private readonly ILectureAccessService _access;

    public ManualUnlockService(ApplicationDbContext db, ILectureAccessService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<UnlockOutcome> UnlockAsync(string teacherId, string studentId, int lectureId, string? reason)
    {
        // Открывать можно только студенту (пользователю с ролью Student)
        var studentName = await GetStudentNameAsync(studentId);
        if (studentName == null)
            return UnlockOutcome.NotFound;

        var lecture = await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Id == lectureId)
            .Select(l => new
            {
                l.Title,
                l.IsPublished,
                DisciplineId = l.Topic.DisciplineId,
                DisciplineName = l.Topic.Discipline.Name,
                DisciplinePublished = l.Topic.Discipline.IsPublished
            })
            .FirstOrDefaultAsync();

        if (lecture == null)
            return UnlockOutcome.NotFound;

        if (!lecture.IsPublished || !lecture.DisciplinePublished)
            return UnlockOutcome.NotPublished;

        // Открывать имеет смысл только заблокированную лекцию
        var states = await _access.GetDisciplineStatesAsync(studentId, lecture.DisciplineId);
        var state = states.FirstOrDefault(s => s.LectureId == lectureId);

        if (state == null)
            return UnlockOutcome.NotFound;

        if (!state.IsLocked)
            return UnlockOutcome.AlreadyAvailable;

        var record = await _db.LectureProgress
            .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LectureId == lectureId);

        if (record == null)
        {
            _db.LectureProgress.Add(new LectureProgress
            {
                StudentId = studentId,
                LectureId = lectureId,
                Status = LectureStatus.NotStudied, // студент лекцию ещё не открывал
                IsManuallyUnlocked = true,
                UnlockedAt = DateTime.UtcNow
            });
        }
        else
        {
            record.IsManuallyUnlocked = true;
            record.UnlockedAt = DateTime.UtcNow;
        }

        var description = $"Студент {studentName}: лекция №{state.Number} «{lecture.Title}» " +
                          $"({lecture.DisciplineName}) открыта вручную";

        if (!string.IsNullOrWhiteSpace(reason))
        {
            var text = reason.Trim();
            description += ". Причина: " + (text.Length > 300 ? text[..300] : text);
        }

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = teacherId,
            Action = AuditActions.ManualUnlock,
            Description = description.Length > 1000 ? description[..1000] : description,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            // Прогресс и запись в журнале сохраняются одной транзакцией
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Двойная отправка формы: запись прогресса уже создана
            _db.ChangeTracker.Clear();
            return UnlockOutcome.AlreadyAvailable;
        }

        return UnlockOutcome.Ok;
    }

    // Отмена возможна, пока студент ещё не открывал эту лекцию
    public async Task<bool> RevokeAsync(string teacherId, string studentId, int lectureId)
    {
        var record = await _db.LectureProgress
            .Include(p => p.Lecture)
            .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LectureId == lectureId);

        if (record == null || !record.IsManuallyUnlocked || record.Status != LectureStatus.NotStudied)
            return false;

        var studentName = await GetStudentNameAsync(studentId) ?? studentId;

        _db.LectureProgress.Remove(record);

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = teacherId,
            Action = AuditActions.ManualUnlockRevoked,
            Description = $"Студент {studentName}: отменено ручное открытие лекции «{record.Lecture.Title}»",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<string?> GetStudentNameAsync(string studentId)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == studentId
                        && _db.UserRoles.Any(ur => ur.UserId == u.Id
                            && _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Student)))
            .Select(u => u.FullName)
            .FirstOrDefaultAsync();
    }
}