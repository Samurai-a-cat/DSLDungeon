namespace DSLDungeon.Services;

public enum ErrorSeverity
{
    Error,
    Warning,
    Info
}

public class CompilationError
{
    public int Line { get; set; }
    public int Column { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ErrorSeverity Severity { get; set; }
}
