namespace ElectronicStudyCabinet.ViewModels;

public class AuditRowViewModel
{
    public DateTime CreatedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AuditActionOptionViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class AuditPageViewModel
{
    public string? Type { get; set; }
    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }

    public List<AuditRowViewModel> Items { get; set; } = new();
    public List<AuditActionOptionViewModel> Types { get; set; } = new();
}