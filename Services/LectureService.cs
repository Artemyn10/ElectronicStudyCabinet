using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public record LectureMaterialInfo(string StoredFileName, string FileName);

public interface ILectureService
{
    Task<List<LectureRowViewModel>> GetListAsync(string? search);
    Task<LectureFormViewModel?> GetNewAsync(int topicId);
    Task<LectureFormViewModel?> GetFormAsync(int id);
    Task FillContextAsync(LectureFormViewModel model);
    Task<int?> CreateAsync(LectureFormViewModel model);
    Task<bool> UpdateAsync(LectureFormViewModel model);
    Task<DeleteResult> DeleteAsync(int id);
    Task<bool> DeleteMaterialAsync(int id);
    Task<bool> MoveAsync(int id, bool up);
    Task<LectureMaterialInfo?> GetMaterialAsync(int id);
}

public class LectureService : ILectureService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;

    public LectureService(ApplicationDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<List<LectureRowViewModel>> GetListAsync(string? search)
    {
        var query = _db.Lectures.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(l => EF.Functions.ILike(l.Title, pattern));
        }

        return await query
            .OrderBy(l => l.Topic.Discipline.Name)
            .ThenBy(l => l.Topic.OrderIndex)
            .ThenBy(l => l.OrderIndex)
            .ThenBy(l => l.Id)
            .Select(l => new LectureRowViewModel
            {
                Id = l.Id,
                Title = l.Title,
                OrderIndex = l.OrderIndex,
                IsPublished = l.IsPublished,
                MaterialFileName = l.MaterialFileName,
                TopicId = l.TopicId,
                TopicName = l.Topic.Name,
                DisciplineId = l.Topic.DisciplineId,
                DisciplineName = l.Topic.Discipline.Name
            })
            .ToListAsync();
    }

    // Пустая форма для новой лекции
    public async Task<LectureFormViewModel?> GetNewAsync(int topicId)
    {
        var info = await _db.Topics
            .AsNoTracking()
            .Where(t => t.Id == topicId)
            .Select(t => new { t.Name, t.DisciplineId, DisciplineName = t.Discipline.Name })
            .FirstOrDefaultAsync();

        if (info == null)
            return null;

        var maxOrder = await _db.Lectures
            .Where(l => l.TopicId == topicId)
            .MaxAsync(l => (int?)l.OrderIndex) ?? 0;

        return new LectureFormViewModel
        {
            TopicId = topicId,
            TopicName = info.Name,
            DisciplineId = info.DisciplineId,
            DisciplineName = info.DisciplineName,
            OrderIndex = maxOrder + 1
        };
    }

    public async Task<LectureFormViewModel?> GetFormAsync(int id)
    {
        return await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new LectureFormViewModel
            {
                Id = l.Id,
                TopicId = l.TopicId,
                TopicName = l.Topic.Name,
                DisciplineId = l.Topic.DisciplineId,
                DisciplineName = l.Topic.Discipline.Name,
                CurrentMaterialFileName = l.MaterialFileName,
                Title = l.Title,
                Description = l.Description,
                OrderIndex = l.OrderIndex,
                IsPublished = l.IsPublished,
                PassingPercentage = l.PassingPercentage,
                MaxAttempts = l.MaxAttempts,
                RequireControlQuestions = l.RequireControlQuestions
            })
            .FirstOrDefaultAsync();
    }

    // Заново заполняет поля для показа, если форма вернулась с ошибкой
    public async Task FillContextAsync(LectureFormViewModel model)
    {
        var info = await _db.Topics
            .AsNoTracking()
            .Where(t => t.Id == model.TopicId)
            .Select(t => new { t.Name, t.DisciplineId, DisciplineName = t.Discipline.Name })
            .FirstOrDefaultAsync();

        if (info != null)
        {
            model.TopicName = info.Name;
            model.DisciplineId = info.DisciplineId;
            model.DisciplineName = info.DisciplineName;
        }

        if (model.Id > 0)
        {
            model.CurrentMaterialFileName = await _db.Lectures
                .AsNoTracking()
                .Where(l => l.Id == model.Id)
                .Select(l => l.MaterialFileName)
                .FirstOrDefaultAsync();
        }
    }

    public async Task<int?> CreateAsync(LectureFormViewModel model)
    {
        if (!await _db.Topics.AnyAsync(t => t.Id == model.TopicId))
            return null;

        var lecture = new Lecture
        {
            TopicId = model.TopicId,
            Title = model.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            OrderIndex = model.OrderIndex,
            IsPublished = model.IsPublished,
            PassingPercentage = model.PassingPercentage,
            MaxAttempts = model.MaxAttempts,
            RequireControlQuestions = model.RequireControlQuestions
        };

        string? storedName = null;
        if (model.MaterialFile is { Length: > 0 })
        {
            storedName = await _storage.SaveAsync(model.MaterialFile);
            lecture.MaterialPath = storedName;
            lecture.MaterialFileName = _storage.GetSafeFileName(model.MaterialFile);
        }

        try
        {
            _db.Lectures.Add(lecture);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Запись в базу не удалась: убираем уже сохранённый файл
            _storage.Delete(storedName);
            throw;
        }

        return lecture.Id;
    }

    public async Task<bool> UpdateAsync(LectureFormViewModel model)
    {
        var lecture = await _db.Lectures.FindAsync(model.Id);
        if (lecture == null)
            return false;

        // TopicId намеренно не меняем
        lecture.Title = model.Title.Trim();
        lecture.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        lecture.OrderIndex = model.OrderIndex;
        lecture.IsPublished = model.IsPublished;
        lecture.PassingPercentage = model.PassingPercentage;
        lecture.MaxAttempts = model.MaxAttempts;
        lecture.RequireControlQuestions = model.RequireControlQuestions;

        string? oldStoredName = null;
        string? newStoredName = null;

        // Если выбран новый файл, он заменяет старый
        if (model.MaterialFile is { Length: > 0 })
        {
            oldStoredName = lecture.MaterialPath;
            newStoredName = await _storage.SaveAsync(model.MaterialFile);
            lecture.MaterialPath = newStoredName;
            lecture.MaterialFileName = _storage.GetSafeFileName(model.MaterialFile);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            _storage.Delete(newStoredName);
            throw;
        }

        // Старый файл удаляем только после успешного сохранения
        _storage.Delete(oldStoredName);
        return true;
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        var lecture = await _db.Lectures.FindAsync(id);
        if (lecture == null)
            return DeleteResult.NotFound;

        // Нельзя удалить лекцию, у которой есть вопросы, тест или данные студентов
        var hasChildren =
            await _db.ControlQuestions.AnyAsync(q => q.LectureId == id) ||
            await _db.Tests.AnyAsync(t => t.LectureId == id) ||
            await _db.LectureProgress.AnyAsync(p => p.LectureId == id);

        if (hasChildren)
            return DeleteResult.HasChildren;

        var storedName = lecture.MaterialPath;

        _db.Lectures.Remove(lecture);
        await _db.SaveChangesAsync();

        _storage.Delete(storedName);
        return DeleteResult.Deleted;
    }

    public async Task<bool> DeleteMaterialAsync(int id)
    {
        var lecture = await _db.Lectures.FindAsync(id);
        if (lecture == null)
            return false;

        var storedName = lecture.MaterialPath;
        lecture.MaterialPath = null;
        lecture.MaterialFileName = null;

        await _db.SaveChangesAsync();

        _storage.Delete(storedName);
        return true;
    }

    // Сдвиг лекции вверх или вниз внутри темы
    public async Task<bool> MoveAsync(int id, bool up)
    {
        var lecture = await _db.Lectures.FindAsync(id);
        if (lecture == null)
            return false;

        var siblings = await _db.Lectures
            .Where(l => l.TopicId == lecture.TopicId)
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Id)
            .ToListAsync();

        // Сначала нумеруем лекции подряд: 1, 2, 3... (убираем повторы и пропуски)
        for (var i = 0; i < siblings.Count; i++)
            siblings[i].OrderIndex = i + 1;

        var index = siblings.FindIndex(l => l.Id == id);
        var swapIndex = up ? index - 1 : index + 1;

        if (swapIndex >= 0 && swapIndex < siblings.Count)
        {
            (siblings[index].OrderIndex, siblings[swapIndex].OrderIndex) =
                (siblings[swapIndex].OrderIndex, siblings[index].OrderIndex);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<LectureMaterialInfo?> GetMaterialAsync(int id)
    {
        var data = await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Id == id && l.MaterialPath != null)
            .Select(l => new { l.MaterialPath, l.MaterialFileName })
            .FirstOrDefaultAsync();

        if (data == null || data.MaterialPath == null)
            return null;

        return new LectureMaterialInfo(data.MaterialPath, data.MaterialFileName ?? data.MaterialPath);
    }
}