namespace ElectronicStudyCabinet.Data;

// Виды действий, которые записываются в журнал
public static class AuditActions
{
    public const string ManualUnlock = "ManualUnlock";
    public const string ManualUnlockRevoked = "ManualUnlockRevoked";
    public const string ReviewAccepted = "ReviewAccepted";
    public const string ReviewRejected = "ReviewRejected";
    public const string DeleteDiscipline = "DeleteDiscipline";
    public const string DeleteLecture = "DeleteLecture";
    public const string DeleteTest = "DeleteTest";
    public const string TestPublished = "TestPublished";
    public const string TestUnpublished = "TestUnpublished";
    public const string GroupCreated = "GroupCreated";
    public const string GroupDeleted = "GroupDeleted";
    public const string GroupMembersAdded = "GroupMembersAdded";
    public const string GroupMemberRemoved = "GroupMemberRemoved";
    public const string GroupChanged = "GroupChanged";

    // Понятные названия для страницы журнала
    private static readonly Dictionary<string, string> Labels = new()
    {
        [ManualUnlock] = "Ручное открытие лекции",
        [ManualUnlockRevoked] = "Отмена ручного открытия",
        [ReviewAccepted] = "Ответ принят",
        [ReviewRejected] = "Ответ отклонён",
        [DeleteDiscipline] = "Удаление дисциплины",
        [DeleteLecture] = "Удаление лекции",
        [DeleteTest] = "Удаление теста",
        [TestPublished] = "Публикация теста",
        [TestUnpublished] = "Снятие теста с публикации",
        [GroupCreated] = "Создание группы",
        [GroupDeleted] = "Удаление группы",
        [GroupMembersAdded] = "Добавление студентов в группу",
        [GroupMemberRemoved] = "Исключение студента из группы",
        [GroupChanged] = "Изменение группы студента",
    };

    public static string GetLabel(string action)
    {
        return Labels.TryGetValue(action, out var label) ? label : action;
    }
}