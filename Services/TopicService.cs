using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface ITopicService
{
    Task<TopicFormViewModel?> GetNewAsync(int disciplineId);
    Task<TopicFormViewModel?> GetFormAsync(int id);
    Task<string?> GetDisciplineNameAsync(int disciplineId);
    Task<bool> CreateAsync(TopicFormViewModel model);
    Task<bool> UpdateAsync(TopicFormViewModel model);
    Task<DeleteResult> DeleteAsync(int id);
}

public class TopicService : ITopicService
{
    private readonly ApplicationDbContext _db;

    public TopicService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Пустая форма для новой темы: порядок = следующий по счёту
    public async Task<TopicFormViewModel?> GetNewAsync(int disciplineId)
    {
        var name = await GetDisciplineNameAsync(disciplineId);
        if (name == null)
            return null;

        var maxOrder = await _db.Topics
            .Where(t => t.DisciplineId == disciplineId)
            .MaxAsync(t => (int?)t.OrderIndex) ?? 0;

        return new TopicFormViewModel
        {
            DisciplineId = disciplineId,
            DisciplineName = name,
            OrderIndex = maxOrder + 1
        };
    }

    public async Task<TopicFormViewModel?> GetFormAsync(int id)
    {
        return await _db.Topics
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TopicFormViewModel
            {
                Id = t.Id,
                DisciplineId = t.DisciplineId,
                DisciplineName = t.Discipline.Name,
                Name = t.Name,
                Description = t.Description,
                OrderIndex = t.OrderIndex
            })
            .FirstOrDefaultAsync();
    }

    public async Task<string?> GetDisciplineNameAsync(int disciplineId)
    {
        return await _db.Disciplines
            .AsNoTracking()
            .Where(d => d.Id == disciplineId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CreateAsync(TopicFormViewModel model)
    {
        if (!await _db.Disciplines.AnyAsync(d => d.Id == model.DisciplineId))
            return false;

        _db.Topics.Add(new Topic
        {
            DisciplineId = model.DisciplineId,
            Name = model.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            OrderIndex = model.OrderIndex
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(TopicFormViewModel model)
    {
        var topic = await _db.Topics.FindAsync(model.Id);
        if (topic == null)
            return false;

        // DisciplineId намеренно не меняем: тему нельзя "перебросить" подменой данных формы
        topic.Name = model.Name.Trim();
        topic.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        topic.OrderIndex = model.OrderIndex;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        var topic = await _db.Topics.FindAsync(id);
        if (topic == null)
            return DeleteResult.NotFound;

        // Нельзя удалить тему, в которой есть лекции
        if (await _db.Lectures.AnyAsync(l => l.TopicId == id))
            return DeleteResult.HasChildren;

        _db.Topics.Remove(topic);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }
}