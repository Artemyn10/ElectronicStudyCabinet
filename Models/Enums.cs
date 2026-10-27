namespace ElectronicStudyCabinet.Models;

/// <summary>Тип контрольного вопроса.</summary>
public enum QuestionType
{
    ShortText = 1,    // краткий текстовый ответ
    SingleChoice = 2  // выбор одного правильного ответа
}

/// <summary>Статус проверки ответа на контрольный вопрос.</summary>
public enum ReviewStatus
{
    AutoChecked = 1,    // проверен автоматически
    PendingReview = 2,  // ждёт проверки преподавателем
    Accepted = 3,       // принят преподавателем
    Rejected = 4        // отклонён преподавателем
}

/// <summary>
/// Статус лекции для студента.
/// В БД хранятся только InProgress, PendingReview и Completed.
/// Locked, Available и NotStudied вычисляются сервисом доступа.
/// </summary>
public enum LectureStatus
{
    Locked = 0,         // Заблокирована
    Available = 1,      // Доступна
    NotStudied = 2,     // Не изучена
    InProgress = 3,     // В процессе
    PendingReview = 4,  // Ожидает проверки
    Completed = 5       // Завершена
}