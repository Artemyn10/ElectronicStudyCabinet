using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface IGroupService
{
    Task<GroupsListViewModel> GetListAsync(string? search, int? specialtyId);
    Task<GroupFormViewModel> GetNewAsync();
    Task<GroupFormViewModel?> GetFormAsync(int id);
    Task FillOptionsAsync(GroupFormViewModel model);
    Task<(bool Success, string? Error, int Id)> CreateAsync(string teacherId, GroupFormViewModel model);
    Task<(bool Success, string? Error)> UpdateAsync(GroupFormViewModel model);
    Task<(DeleteResult Result, string? Name)> DeleteAsync(string teacherId, int id);
    Task<GroupDetailsViewModel?> GetDetailsAsync(int id);
    Task<(int Added, int Skipped)> AddMembersAsync(string teacherId, int groupId, List<string> studentIds);
    Task<bool> RemoveMemberAsync(string teacherId, int groupId, string studentId);
    Task<bool> AssignAsync(string teacherId, string studentId, int? groupId);
}

public class GroupService : IGroupService
{
    private const string DuplicateError = "Группа с таким названием уже существует";

    private readonly ApplicationDbContext _db;
    private readonly ITeacherReportService _reports;
    private readonly IAuditService _audit;

    public GroupService(ApplicationDbContext db, ITeacherReportService reports, IAuditService audit)
    {
        _db = db;
        _reports = reports;
        _audit = audit;
    }

    // ===== Список и формы =====

    public async Task<GroupsListViewModel> GetListAsync(string? search, int? specialtyId)
    {
        var query = _db.StudentGroups.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(g => EF.Functions.ILike(g.Name, pattern));
        }

        if (specialtyId != null)
            query = query.Where(g => g.SpecialtyId == specialtyId);

        var items = await query
            .OrderBy(g => g.Name)
            .Select(g => new GroupRowViewModel
            {
                Id = g.Id,
                Name = g.Name,
                SpecialtyName = g.Specialty.Name,
                Description = g.Description,
                StudentsCount = g.Members.Count
            })
            .ToListAsync();

        return new GroupsListViewModel
        {
            Search = search,
            SpecialtyId = specialtyId,
            Items = items,
            Specialties = await GetSpecialtyOptionsAsync()
        };
    }

    public async Task<GroupFormViewModel> GetNewAsync()
    {
        return new GroupFormViewModel { Specialties = await GetSpecialtyOptionsAsync() };
    }

    public async Task<GroupFormViewModel?> GetFormAsync(int id)
    {
        var model = await _db.StudentGroups
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new GroupFormViewModel
            {
                Id = g.Id,
                Name = g.Name,
                SpecialtyId = g.SpecialtyId,
                Description = g.Description
            })
            .FirstOrDefaultAsync();

        if (model != null)
            model.Specialties = await GetSpecialtyOptionsAsync();

        return model;
    }

    public async Task FillOptionsAsync(GroupFormViewModel model)
    {
        model.Specialties = await GetSpecialtyOptionsAsync();
    }

    // ===== Создание, изменение, удаление =====

    public async Task<(bool Success, string? Error, int Id)> CreateAsync(string teacherId, GroupFormViewModel model)
    {
        var name = model.Name.Trim();
        var lowered = name.ToLower();

        if (await _db.StudentGroups.AnyAsync(g => g.Name.ToLower() == lowered))
            return (false, DuplicateError, 0);

        var specialty = await _db.Specialties
            .AsNoTracking()
            .Where(s => s.Id == model.SpecialtyId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync();

        if (specialty == null)
            return (false, "Выберите специальность из списка", 0);

        var group = new StudentGroup
        {
            Name = name,
            SpecialtyId = model.SpecialtyId!.Value,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.StudentGroups.Add(group);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, DuplicateError, 0);
        }

        await _audit.LogAsync(teacherId, AuditActions.GroupCreated,
            $"Создана группа «{name}» (специальность «{specialty}»)");

        return (true, null, group.Id);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(GroupFormViewModel model)
    {
        var group = await _db.StudentGroups.FindAsync(model.Id);
        if (group == null)
            return (false, "Группа не найдена");

        var name = model.Name.Trim();
        var lowered = name.ToLower();

        if (await _db.StudentGroups.AnyAsync(g => g.Id != model.Id && g.Name.ToLower() == lowered))
            return (false, DuplicateError);

        if (!await _db.Specialties.AnyAsync(s => s.Id == model.SpecialtyId))
            return (false, "Выберите специальность из списка");

        group.Name = name;
        group.SpecialtyId = model.SpecialtyId!.Value;
        group.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

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

    public async Task<(DeleteResult Result, string? Name)> DeleteAsync(string teacherId, int id)
    {
        var group = await _db.StudentGroups.FindAsync(id);
        if (group == null)
            return (DeleteResult.NotFound, null);

        // Нельзя удалить группу, в которой есть студенты
        if (await _db.GroupMemberships.AnyAsync(m => m.StudentGroupId == id))
            return (DeleteResult.HasChildren, group.Name);

        var name = group.Name;

        _db.StudentGroups.Remove(group);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(teacherId, AuditActions.GroupDeleted, $"Удалена группа «{name}»");
        return (DeleteResult.Deleted, name);
    }

    // ===== Страница группы и состав =====

    public async Task<GroupDetailsViewModel?> GetDetailsAsync(int id)
    {
        var group = await _db.StudentGroups
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new GroupDetailsViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                SpecialtyName = g.Specialty.Name,
                SpecialtyCode = g.Specialty.Code
            })
            .FirstOrDefaultAsync();

        if (group == null)
            return null;

        // Состав группы берём из раздела "Студенты": там уже посчитан прогресс
        group.Members = (await _reports.GetStudentsAsync(null, null, id)).Items;

        group.AverageProgress = group.Members.Count == 0
            ? 0
            : (int)Math.Round(group.Members.Average(m => m.ProgressPercent));

        // Студенты, которых можно добавить: ещё не состоят ни в одной группе
        group.AvailableStudents = await _db.OnlyStudents()
            .AsNoTracking()
            .Where(u => !_db.GroupMemberships.Any(m => m.StudentId == u.Id))
            .OrderBy(u => u.FullName)
            .Select(u => new AvailableStudentViewModel { Id = u.Id, FullName = u.FullName, UserName = u.UserName })
            .ToListAsync();

        return group;
    }

    public async Task<(int Added, int Skipped)> AddMembersAsync(string teacherId, int groupId, List<string> studentIds)
    {
        var ids = studentIds.Distinct().ToList();

        var groupName = await _db.StudentGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => g.Name)
            .FirstOrDefaultAsync();

        if (groupName == null)
            return (0, ids.Count);

        // Берём только настоящих студентов, которые ещё не состоят в группе
        var students = await _db.OnlyStudents()
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync();

        var alreadyInGroup = (await _db.GroupMemberships
                .AsNoTracking()
                .Where(m => ids.Contains(m.StudentId))
                .Select(m => m.StudentId)
                .ToListAsync())
            .ToHashSet();

        var toAdd = students.Where(s => !alreadyInGroup.Contains(s.Id)).ToList();

        foreach (var student in toAdd)
        {
            _db.GroupMemberships.Add(new GroupMembership
            {
                StudentId = student.Id,
                StudentGroupId = groupId,
                AddedAt = DateTime.UtcNow
            });
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Двойная отправка формы: студент уже добавлен
            _db.ChangeTracker.Clear();
            return (0, ids.Count);
        }

        if (toAdd.Count > 0)
        {
            await _audit.LogAsync(teacherId, AuditActions.GroupMembersAdded,
                $"В группу «{groupName}» добавлены студенты: {string.Join(", ", toAdd.Select(s => s.FullName))}");
        }

        return (toAdd.Count, ids.Count - toAdd.Count);
    }

    public async Task<bool> RemoveMemberAsync(string teacherId, int groupId, string studentId)
    {
        var membership = await _db.GroupMemberships
            .Include(m => m.Student)
            .Include(m => m.StudentGroup)
            .FirstOrDefaultAsync(m => m.StudentGroupId == groupId && m.StudentId == studentId);

        if (membership == null)
            return false;

        var description = $"Студент {membership.Student.FullName} исключён из группы «{membership.StudentGroup.Name}»";

        _db.GroupMemberships.Remove(membership);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(teacherId, AuditActions.GroupMemberRemoved, description);
        return true;
    }

    // Назначение или перевод студента (groupId = null: убрать из группы)
    public async Task<bool> AssignAsync(string teacherId, string studentId, int? groupId)
    {
        var studentName = await _db.OnlyStudents()
            .AsNoTracking()
            .Where(u => u.Id == studentId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync();

        if (studentName == null)
            return false;

        var membership = await _db.GroupMemberships
            .Include(m => m.StudentGroup)
            .FirstOrDefaultAsync(m => m.StudentId == studentId);

        var oldName = membership?.StudentGroup.Name;

        if (groupId == null)
        {
            if (membership == null)
                return true;

            _db.GroupMemberships.Remove(membership);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(teacherId, AuditActions.GroupChanged,
                $"Студент {studentName}: исключён из группы «{oldName}»");
            return true;
        }

        var newName = await _db.StudentGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => g.Name)
            .FirstOrDefaultAsync();

        if (newName == null)
            return false;

        // Группа не изменилась
        if (membership != null && membership.StudentGroupId == groupId)
            return true;

        if (membership == null)
        {
            _db.GroupMemberships.Add(new GroupMembership
            {
                StudentId = studentId,
                StudentGroupId = groupId.Value,
                AddedAt = DateTime.UtcNow
            });
        }
        else
        {
            membership.StudentGroupId = groupId.Value;
            membership.AddedAt = DateTime.UtcNow;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return false;
        }

        var description = oldName == null
            ? $"Студент {studentName}: назначена группа «{newName}»"
            : $"Студент {studentName}: переведён из группы «{oldName}» в группу «{newName}»";

        await _audit.LogAsync(teacherId, AuditActions.GroupChanged, description);
        return true;
    }

    // ===== Вспомогательные методы =====

    private async Task<List<SimpleOptionViewModel>> GetSpecialtyOptionsAsync()
    {
        return await _db.Specialties
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SimpleOptionViewModel { Id = s.Id, Name = s.Name })
            .ToListAsync();
    }
}