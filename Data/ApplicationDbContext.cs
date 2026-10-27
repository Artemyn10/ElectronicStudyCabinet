using ElectronicStudyCabinet.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Каждое свойство DbSet = одна таблица в базе
    public DbSet<Discipline> Disciplines => Set<Discipline>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Lecture> Lectures => Set<Lecture>();
    public DbSet<ControlQuestion> ControlQuestions => Set<ControlQuestion>();
    public DbSet<ControlQuestionOption> ControlQuestionOptions => Set<ControlQuestionOption>();
    public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<TestQuestion> TestQuestions => Set<TestQuestion>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<LectureProgress> LectureProgress => Set<LectureProgress>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Обязательно: настраивает таблицы Identity
        base.OnModelCreating(builder);

        // ===== Дисциплина → Темы → Лекции =====
        builder.Entity<Topic>(e =>
        {
            e.HasOne(t => t.Discipline)
                .WithMany(d => d.Topics)
                .HasForeignKey(t => t.DisciplineId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(t => new { t.DisciplineId, t.OrderIndex });
        });

        builder.Entity<Lecture>(e =>
        {
            e.HasOne(l => l.Topic)
                .WithMany(t => t.Lectures)
                .HasForeignKey(l => l.TopicId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(l => new { l.TopicId, l.OrderIndex });

            // Проходной процент 1–100, попыток не меньше одной
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Lectures_PassingPercentage",
                    "\"PassingPercentage\" BETWEEN 1 AND 100");
                t.HasCheckConstraint("CK_Lectures_MaxAttempts",
                    "\"MaxAttempts\" >= 1");
            });
        });

        // ===== Контрольные вопросы =====
        builder.Entity<ControlQuestion>(e =>
        {
            e.HasOne(q => q.Lecture)
                .WithMany(l => l.ControlQuestions)
                .HasForeignKey(q => q.LectureId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(q => new { q.LectureId, q.OrderIndex });
            e.Property(q => q.QuestionType).HasConversion<string>().HasMaxLength(30);
        });

        builder.Entity<ControlQuestionOption>(e =>
        {
            // Варианты удаляются вместе с вопросом
            e.HasOne(o => o.ControlQuestion)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.ControlQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StudentAnswer>(e =>
        {
            e.HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.ControlQuestion)
                .WithMany(q => q.StudentAnswers)
                .HasForeignKey(a => a.ControlQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Один ответ студента на один вопрос (защита от двойной отправки формы)
            e.HasIndex(a => new { a.StudentId, a.ControlQuestionId }).IsUnique();
            e.Property(a => a.ReviewStatus).HasConversion<string>().HasMaxLength(30);
        });

        // ===== Тесты =====
        builder.Entity<Test>(e =>
        {
            // Один тест на одну лекцию (для связи один-к-одному индекс уникальный)
            e.HasOne(t => t.Lecture)
                .WithOne(l => l.Test)
                .HasForeignKey<Test>(t => t.LectureId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TestQuestion>(e =>
        {
            e.HasOne(q => q.Test)
                .WithMany(t => t.Questions)
                .HasForeignKey(q => q.TestId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(q => new { q.TestId, q.OrderIndex });
        });

        builder.Entity<AnswerOption>(e =>
        {
            e.HasOne(o => o.TestQuestion)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.TestQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TestAttempt>(e =>
        {
            e.HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Test)
                .WithMany(t => t.Attempts)
                .HasForeignKey(a => a.TestId)
                .OnDelete(DeleteBehavior.Restrict);

            // Номер попытки у студента по тесту не повторяется
            e.HasIndex(a => new { a.StudentId, a.TestId, a.AttemptNumber }).IsUnique();

            e.ToTable(t => t.HasCheckConstraint("CK_TestAttempts_Score",
                "\"ScorePercentage\" BETWEEN 0 AND 100"));
        });

        // ===== Прогресс =====
        builder.Entity<LectureProgress>(e =>
        {
            e.HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.Lecture)
                .WithMany(l => l.ProgressRecords)
                .HasForeignKey(p => p.LectureId)
                .OnDelete(DeleteBehavior.Restrict);

            // Одна запись прогресса на студента и лекцию
            e.HasIndex(p => new { p.StudentId, p.LectureId }).IsUnique();
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
        });

        // ===== Журнал действий =====
        builder.Entity<AuditLog>(e =>
        {
            e.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(a => a.CreatedAt);
        });
    }
}