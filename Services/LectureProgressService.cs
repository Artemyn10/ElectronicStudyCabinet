using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface ILectureProgressService
{
    // Студент открыл лекцию: создаёт запись прогресса и пересчитывает статусы
    Task OpenAsync(string studentId, int lectureId);

    // Пересчитывает и сохраняет статусы лекций дисциплины (после ответа, теста, проверки)
    Task SyncAsync(string studentId, int lectureId);
}

public class LectureProgressService : ILectureProgressService
{
    private readonly ApplicationDbContext _db;
    private readonly ILectureAccessService _access;

    public LectureProgressService(ApplicationDbContext db, ILectureAccessService access)
    {
        _db = db;
        _access = access;
    }

    public async Task OpenAsync(string studentId, int lectureId)
    {
        var record = await _db.LectureProgress
            .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LectureId == lectureId);

        if (record == null)
        {
            _db.LectureProgress.Add(new LectureProgress
            {
                StudentId = studentId,
                LectureId = lectureId,
                Status = LectureStatus.InProgress
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Двойное открытие: уникальный индекс не дал создать вторую запись, и это нормально
                _db.ChangeTracker.Clear();
            }
        }
        else if (record.Status == LectureStatus.NotStudied)
        {
            // Лекцию вручную открыл преподаватель, студент открыл её впервые
            record.Status = LectureStatus.InProgress;
            await _db.SaveChangesAsync();
        }

        await SyncAsync(studentId, lectureId);
    }

    public async Task SyncAsync(string studentId, int lectureId)
    {
        var disciplineId = await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Id == lectureId)
            .Select(l => (int?)l.Topic.DisciplineId)
            .FirstOrDefaultAsync();

        if (disciplineId == null)
            return;

        var states = await _access.GetDisciplineStatesAsync(studentId, disciplineId.Value);
        var lectureIds = states.Select(s => s.LectureId).ToList();

        var records = await _db.LectureProgress
            .Where(p => p.StudentId == studentId && lectureIds.Contains(p.LectureId))
            .ToListAsync();

        var changed = false;

        foreach (var record in records)
        {
            // Завершённая лекция не откатывается назад
            if (record.Status == LectureStatus.Completed)
                continue;

            var state = states.FirstOrDefault(s => s.LectureId == record.LectureId);
            if (state == null)
                continue;

            var newStatus = state.Status switch
            {
                LectureStatus.Completed => LectureStatus.Completed,
                LectureStatus.PendingReview => LectureStatus.PendingReview,
                LectureStatus.InProgress => LectureStatus.InProgress,
                _ => record.Status // Заблокирована или Доступна: запись не трогаем
            };

            if (newStatus == record.Status)
                continue;

            record.Status = newStatus;

            if (newStatus == LectureStatus.Completed)
                record.CompletedAt = DateTime.UtcNow;

            changed = true;
        }

        if (changed)
            await _db.SaveChangesAsync();
    }
}