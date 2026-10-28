using ElectronicStudyCabinet.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public enum LectureAccessStatus
{
    Allowed,   // доступ разрешён
    NotFound,  // лекции не существует
    Denied     // доступ запрещён
}

public record LectureAccessResult(LectureAccessStatus Status, string? Message = null)
{
    public bool IsAllowed => Status == LectureAccessStatus.Allowed;
}

// Единое место, где решается, может ли студент открыть лекцию
public interface ILectureAccessService
{
    Task<LectureAccessResult> CheckAsync(string studentId, int lectureId);
}

public class LectureAccessService : ILectureAccessService
{
    private readonly ApplicationDbContext _db;

    public LectureAccessService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<LectureAccessResult> CheckAsync(string studentId, int lectureId)
    {
        var lecture = await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Id == lectureId)
            .Select(l => new
            {
                l.IsPublished,
                DisciplinePublished = l.Topic.Discipline.IsPublished
            })
            .FirstOrDefaultAsync();

        if (lecture == null)
            return new LectureAccessResult(LectureAccessStatus.NotFound);

        // Неопубликованные лекции и дисциплины студенту недоступны
        if (!lecture.IsPublished || !lecture.DisciplinePublished)
            return new LectureAccessResult(LectureAccessStatus.Denied, "Доступ к лекции запрещён");

        // Этап 12: здесь появится проверка последовательности (тест и задания предыдущей лекции)

        return new LectureAccessResult(LectureAccessStatus.Allowed);
    }
}