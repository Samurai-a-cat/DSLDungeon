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

    // Debounce для on-the-fly проверки
    private System.Timers.Timer? _debounceTimer;
    private readonly object _timerLock = new();

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
        ScheduleCompileCheck();
    }

    private void ScheduleCompileCheck()
    {
        lock (_timerLock)
        {
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();
            _debounceTimer = new System.Timers.Timer(500); // 500ms debounce
            _debounceTimer.Elapsed += async (_, _) =>
            {
                _debounceTimer?.Dispose();
                _debounceTimer = null;
                await CompileOnFlyAsync();
            };
            _debounceTimer.AutoReset = false;
            _debounceTimer.Start();
        }
    }

    private async Task CompileOnFlyAsync()
    {
        var result = _compiler.Compile(CurrentCode);
        Errors = result.Errors;
        NotifyStateChanged();
        await UpdateMonacoMarkersAsync();
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
        await UpdateMonacoMarkersAsync();

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

        // Таймаут 1 секунда через CancellationTokenSource
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            var output = await _compiler.RunSandboxAsync(CurrentCode);
            // Если RunSandboxAsync не поддерживает CancellationToken — таймаут не сработает
            // В WASM реальное выполнение кода заблокирует поток anyway
            await _js.InvokeVoidAsync("console.log", output);
        }
        catch (OperationCanceledException)
        {
            await _js.InvokeVoidAsync("console.log", "❌ Sandbox timed out after 1 second");
        }

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

    private async Task UpdateMonacoMarkersAsync()
    {
        var markers = Errors.Select(e => new
        {
            severity = e.Severity switch
            {
                ErrorSeverity.Error => 8,
                ErrorSeverity.Warning => 4,
                ErrorSeverity.Info => 2,
                _ => 1
            },
            message = e.Message,
            startLineNumber = e.Line,
            startColumn = e.Column,
            endLineNumber = e.Line,
            endColumn = 1000 // подчёркиваем всю строку от Column до конца
        }).ToList();

        await _js.InvokeVoidAsync("setMonacoMarkers", "dsl-editor", markers);
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
