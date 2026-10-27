using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface IDisciplineService
{
    Task<List<DisciplineListItemViewModel>> GetListAsync(string? search);
    Task<DisciplineDetailsViewModel?> GetDetailsAsync(int id);
    Task<DisciplineFormViewModel?> GetFormAsync(int id);
    Task<int> CreateAsync(DisciplineFormViewModel model);
    Task<bool> UpdateAsync(DisciplineFormViewModel model);
    Task<DeleteResult> DeleteAsync(int id);
}

public class DisciplineService : IDisciplineService
{
    private readonly ApplicationDbContext _db;

    public DisciplineService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<DisciplineListItemViewModel>> GetListAsync(string? search)
    {
        var query = _db.Disciplines.AsNoTracking();

        // Поиск по названию без учёта регистра
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(d => EF.Functions.ILike(d.Name, pattern));
        }

        return await query
            .OrderBy(d => d.Name)
            .Select(d => new DisciplineListItemViewModel
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                IsPublished = d.IsPublished,
                CreatedAt = d.CreatedAt,
                TopicsCount = d.Topics.Count,
                LecturesCount = d.Topics.SelectMany(t => t.Lectures).Count()
            })
            .ToListAsync();
    }

    public async Task<DisciplineDetailsViewModel?> GetDetailsAsync(int id)
    {
        return await _db.Disciplines
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DisciplineDetailsViewModel
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                IsPublished = d.IsPublished,
                CreatedAt = d.CreatedAt,
                Topics = d.Topics
                    .OrderBy(t => t.OrderIndex).ThenBy(t => t.Id)
                    .Select(t => new TopicListItemViewModel
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        OrderIndex = t.OrderIndex,
                        LecturesCount = t.Lectures.Count,
                        Lectures = t.Lectures
                            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Id)
                            .Select(l => new LectureListItemViewModel
                            {
                                Id = l.Id,
                                Title = l.Title,
                                OrderIndex = l.OrderIndex,
                                IsPublished = l.IsPublished,
                                MaterialFileName = l.MaterialFileName,
                                PassingPercentage = l.PassingPercentage,
                                MaxAttempts = l.MaxAttempts,
                                ControlQuestionsCount = l.ControlQuestions.Count
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<DisciplineFormViewModel?> GetFormAsync(int id)
    {
        return await _db.Disciplines
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DisciplineFormViewModel
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                IsPublished = d.IsPublished
            })
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateAsync(DisciplineFormViewModel model)
    {
        var discipline = new Discipline
        {
            Name = model.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            IsPublished = model.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        _db.Disciplines.Add(discipline);
        await _db.SaveChangesAsync();
        return discipline.Id;
    }

    public async Task<bool> UpdateAsync(DisciplineFormViewModel model)
    {
        var discipline = await _db.Disciplines.FindAsync(model.Id);
        if (discipline == null)
            return false;

        discipline.Name = model.Name.Trim();
        discipline.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        discipline.IsPublished = model.IsPublished;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        var discipline = await _db.Disciplines.FindAsync(id);
        if (discipline == null)
            return DeleteResult.NotFound;

        // Нельзя удалить дисциплину, в которой есть темы
        if (await _db.Topics.AnyAsync(t => t.DisciplineId == id))
            return DeleteResult.HasChildren;

        _db.Disciplines.Remove(discipline);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }
}