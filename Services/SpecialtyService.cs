using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface ISpecialtyService
{
    Task<List<SpecialtyRowViewModel>> GetListAsync(string? search);
    Task<SpecialtyFormViewModel?> GetFormAsync(int id);
    Task<(bool Success, string? Error)> CreateAsync(SpecialtyFormViewModel model);
    Task<(bool Success, string? Error)> UpdateAsync(SpecialtyFormViewModel model);
    Task<DeleteResult> DeleteAsync(int id);
}

public class SpecialtyService : ISpecialtyService
{
    private const string DuplicateError = "Специальность с таким названием уже существует";

    private readonly ApplicationDbContext _db;

    public SpecialtyService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<SpecialtyRowViewModel>> GetListAsync(string? search)
    {
        var query = _db.Specialties.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(s => EF.Functions.ILike(s.Name, pattern)
                                     || EF.Functions.ILike(s.Code ?? "", pattern));
        }

        return await query
            .OrderBy(s => s.Name)
            .Select(s => new SpecialtyRowViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                GroupsCount = s.Groups.Count,
                StudentsCount = s.Groups.SelectMany(g => g.Members).Count()
            })
            .ToListAsync();
    }

    public async Task<SpecialtyFormViewModel?> GetFormAsync(int id)
    {
        return await _db.Specialties
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SpecialtyFormViewModel { Id = s.Id, Name = s.Name, Code = s.Code })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string? Error)> CreateAsync(SpecialtyFormViewModel model)
    {
        var name = model.Name.Trim();
        var lowered = name.ToLower();

        if (await _db.Specialties.AnyAsync(s => s.Name.ToLower() == lowered))
            return (false, DuplicateError);

        _db.Specialties.Add(new Specialty
        {
            Name = name,
            Code = string.IsNullOrWhiteSpace(model.Code) ? null : model.Code.Trim()
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Двойная отправка формы: уникальный индекс не дал создать дубль
            return (false, DuplicateError);
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(SpecialtyFormViewModel model)
    {
        var specialty = await _db.Specialties.FindAsync(model.Id);
        if (specialty == null)
            return (false, "Специальность не найдена");

        var name = model.Name.Trim();
        var lowered = name.ToLower();

        if (await _db.Specialties.AnyAsync(s => s.Id != model.Id && s.Name.ToLower() == lowered))
            return (false, DuplicateError);

        specialty.Name = name;
        specialty.Code = string.IsNullOrWhiteSpace(model.Code) ? null : model.Code.Trim();

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, DuplicateError);
        }

        return (true, null);
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        var specialty = await _db.Specialties.FindAsync(id);
        if (specialty == null)
            return DeleteResult.NotFound;

        // Нельзя удалить специальность, у которой есть группы
        if (await _db.StudentGroups.AnyAsync(g => g.SpecialtyId == id))
            return DeleteResult.HasChildren;

        _db.Specialties.Remove(specialty);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }
}