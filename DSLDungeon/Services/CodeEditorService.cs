using Microsoft.JSInterop;

namespace DSLDungeon.Services;

public enum CompilationStatus
{
    Idle,
    Applying,
    Applied,
    Error
}

public class CodeEditorService
{
    private readonly DslCompilerService _compiler;
    private readonly IJSRuntime _js;
    private const string LocalStorageKey = "dsldungeon_code";

    public string CurrentCode { get; set; } = DefaultCode;
    public List<CompilationError> Errors { get; set; } = new();
    public CompilationStatus Status { get; private set; } = CompilationStatus.Idle;
    public string? LastValidScript { get; private set; }

    public event Action? OnStateChanged;

    public CodeEditorService(DslCompilerService compiler, IJSRuntime js)
    {
        _compiler = compiler;
        _js = js;
    }

    public static string DefaultCode => @"// DSLDungeon Script
// Пиши код на C# — он будет управлять поведением героев

Console.WriteLine(""Hello, DSLDungeon!"");

// Пример: var hero = GetHero();
// hero.Queue.Add(new MoveEvent(...));
";

    public void UpdateCode(string code)
    {
        CurrentCode = code;
        _ = SaveToLocalStorageAsync();
    }

    public async Task LoadFromLocalStorageAsync()
    {
        var code = await _js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
        if (!string.IsNullOrEmpty(code))
            CurrentCode = code;
        NotifyStateChanged();
    }

    public async Task SaveToLocalStorageAsync()
    {
        await _js.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, CurrentCode);
    }

    public async Task ApplyAsync()
    {
        SetStatus(CompilationStatus.Applying);
        await Task.Delay(50); // дать UI отрисовать спиннер

        var result = _compiler.Compile(CurrentCode);
        Errors = result.Errors;

        if (result.Success)
        {
            LastValidScript = result.Code;
            SetStatus(CompilationStatus.Applied);
            _ = AutoResetStatusAsync();
        }
        else
        {
            SetStatus(CompilationStatus.Error);
        }

        NotifyStateChanged();
    }

    public async Task TestSandboxAsync()
    {
        SetStatus(CompilationStatus.Applying);
        var output = await _compiler.RunSandboxAsync(CurrentCode);
        await _js.InvokeVoidAsync("console.log", output);
        SetStatus(CompilationStatus.Idle);
        NotifyStateChanged();
    }

    public async Task SaveToFileAsync()
    {
        var success = await _js.InvokeAsync<bool>("saveToFile", "script.cs", CurrentCode);
        if (!success)
        {
            await _js.InvokeVoidAsync("downloadFile", "script.cs", CurrentCode);
        }
    }

    private void SetStatus(CompilationStatus status)
    {
        Status = status;
        NotifyStateChanged();
    }

    private async Task AutoResetStatusAsync()
    {
        await Task.Delay(2000);
        if (Status == CompilationStatus.Applied)
        {
            Status = CompilationStatus.Idle;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
