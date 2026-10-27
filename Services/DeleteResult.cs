namespace ElectronicStudyCabinet.Services;

public enum DeleteResult
{
    Deleted,      // удалено
    NotFound,     // запись не найдена
    HasChildren   // есть вложенные данные, удалять нельзя
}