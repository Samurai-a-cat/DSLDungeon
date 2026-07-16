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
    private readonly GameService _gameService;
    private readonly IJSRuntime _js;
    private const string LocalStorageKey = "dsldungeon_code";

    public string CurrentCode { get; set; } = DefaultCode;
    public List<CompilationError> Errors { get; set; } = new();
    public CompilationStatus Status { get; private set; } = CompilationStatus.Idle;
    public string? LastValidScript { get; private set; }

    public event Action? OnStateChanged;

    private System.Timers.Timer? _debounceTimer;
    private readonly object _timerLock = new();

    public CodeEditorService(DslCompilerService compiler, GameService gameService, IJSRuntime js)
    {
        _compiler = compiler;
        _gameService = gameService;
        _js = js;
    }

    public static string DefaultCode => @"// DSLDungeon Script
// Пиши код на C# — он будет управлять поведением героев

public void Tick(DslContext context)
{
    var enemy = context.FindNearestEnemyInfo();
    if (enemy.HasValue)
    {
        int dist = context.DistanceTo(enemy.Value.Q, enemy.Value.R);
        if (dist <= 1)
        {
            context.Attack(enemy.Value.Name);
        }
        else
        {
            context.MoveTowards(enemy.Value.Q, enemy.Value.R);
        }
    }
    else
    {
        context.Wait(0.5f);
    }
}

// Вы можете создавать любые вспомогательные функции!
// Все они должны возвращать значение, завершаться return или вызывать команду.
private int GetTargetDistance(DslContext context, int targetQ, int targetR)
{
    return context.DistanceTo(targetQ, targetR);
}
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
            _debounceTimer = new System.Timers.Timer(500);
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
        var result = await _compiler.CompileAsync(CurrentCode);
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
        await Task.Delay(50);

        var result = await _compiler.CompileAsync(CurrentCode);
        Errors = result.Errors;
        await UpdateMonacoMarkersAsync();

        if (result.Success && result.CompiledAssembly != null)
        {
            LastValidScript = result.Code;
            _gameService.SetHeroScript(result.CompiledAssembly);
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

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            var output = await _compiler.RunSandboxAsync(CurrentCode);
            // Явное приведение аргумента к object для устранения неоднозначности вызова
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
            endColumn = 1000
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