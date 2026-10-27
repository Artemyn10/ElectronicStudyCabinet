namespace ElectronicStudyCabinet.Services;

public interface IFileStorageService
{
    string? Validate(IFormFile file);
    string GetSafeFileName(IFormFile file);
    Task<string> SaveAsync(IFormFile file);
    Stream? OpenRead(string storedFileName);
    string GetContentType(string storedFileName);
    void Delete(string? storedFileName);
}

public class FileStorageService : IFileStorageService
{
    public const long MaxFileSizeBytes = 20L * 1024 * 1024; // 20 МБ

    private static readonly string[] AllowedExtensions = { ".pdf", ".docx" };

    private readonly string _root;

    public FileStorageService(IWebHostEnvironment env, IConfiguration configuration)
    {
        var configured = configuration["Storage:UploadsPath"] ?? "PrivateUploads";

        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(env.ContentRootPath, configured);

        Directory.CreateDirectory(_root);
    }

    // Возвращает текст ошибки или null, если файл подходит
    public string? Validate(IFormFile file)
    {
        if (file.Length == 0)
            return "Файл пустой";

        if (file.Length > MaxFileSizeBytes)
            return "Размер файла не должен превышать 20 МБ";

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return "Допустимы только файлы PDF и DOCX";

        // Проверяем не только расширение, но и начало содержимого файла
        var header = new byte[4];
        using (var stream = file.OpenReadStream())
        {
            var read = stream.Read(header, 0, header.Length);
            if (read < header.Length)
                return "Файл повреждён";
        }

        var isPdf = header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46; // "%PDF"
        var isZip = header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04; // "PK.."

        if (extension == ".pdf" && !isPdf)
            return "Файл не является PDF-документом";

        if (extension == ".docx" && !isZip)
            return "Файл не является DOCX-документом";

        return null;
    }

    // Имя файла для показа пользователю (без пути, не длиннее допустимого)
    public string GetSafeFileName(IFormFile file)
    {
        var name = Path.GetFileName(file.FileName);
        if (name.Length > 200)
            name = name[..150] + Path.GetExtension(name);

        return name;
    }

    // Сохраняет файл под случайным именем и возвращает это имя
    public async Task<string> SaveAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(_root, storedName);

        await using var target = new FileStream(path, FileMode.CreateNew);
        await file.CopyToAsync(target);

        return storedName;
    }

    public Stream? OpenRead(string storedFileName)
    {
        // GetFileName отсекает любые попытки выйти из папки (например, "..\..\")
        var path = Path.Combine(_root, Path.GetFileName(storedFileName));

        return File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
    }

    public string GetContentType(string storedFileName)
    {
        return Path.GetExtension(storedFileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }

    public void Delete(string? storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
            return;

        try
        {
            var path = Path.Combine(_root, Path.GetFileName(storedFileName));
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // Не удалось удалить файл: это не должно ломать работу приложения
        }
    }
}