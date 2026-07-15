using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DSLDungeon.Services;

public class CompilationResult
{
    public bool Success { get; set; }
    public List<CompilationError> Errors { get; set; } = new();
    public string? Code { get; set; }
}

public class DslCompilerService
{
    // ScriptOptions.Default уже содержит базовые сборки (mscorlib, System, System.Core).
    // В WASM дополнительные WithReferences могут не найти файлы на диске — не добавляем.
    private static readonly ScriptOptions _scriptOptions = ScriptOptions.Default
        .WithImports("System", "System.Collections.Generic", "System.Linq");

    public CompilationResult Compile(string code)
    {
        var result = new CompilationResult();
        try
        {
            var script = CSharpScript.Create(code, _scriptOptions);
            script.Compile(); // throws CompilationErrorException on errors
            result.Success = true;
            result.Code = code;
        }
        catch (CompilationErrorException ex)
        {
            result.Success = false;
            foreach (var diagnostic in ex.Diagnostics)
            {
                if (MapDiagnostic(diagnostic) is { } err)
                    result.Errors.Add(err);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(new CompilationError
            {
                Line = 0,
                Column = 0,
                Message = $"Compiler exception: {ex.Message}",
                Severity = ErrorSeverity.Error
            });
        }
        return result;
    }

    public async Task<string> RunSandboxAsync(string code)
    {
        try
        {
            var script = CSharpScript.Create(code, _scriptOptions);
            var state = await script.RunAsync();
            var returnValue = state.ReturnValue;
            return returnValue?.ToString() ?? "🧪 Sandbox test passed (null returned)";
        }
        catch (CompilationErrorException ex)
        {
            return $"❌ Compilation error: {string.Join("; ", ex.Diagnostics.Select(d => d.GetMessage()))}";
        }
        catch (Exception ex)
        {
            return $"❌ Runtime error: {ex.Message}";
        }
    }

    private static CompilationError? MapDiagnostic(Diagnostic diagnostic)
    {
        if (diagnostic.Severity == DiagnosticSeverity.Hidden) return null;

        var severity = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => ErrorSeverity.Error,
            DiagnosticSeverity.Warning => ErrorSeverity.Warning,
            DiagnosticSeverity.Info => ErrorSeverity.Info,
            _ => ErrorSeverity.Info
        };

        var lineSpan = diagnostic.Location.GetLineSpan();
        return new CompilationError
        {
            Line = lineSpan.StartLinePosition.Line + 1,
            Column = lineSpan.StartLinePosition.Character + 1,
            Message = diagnostic.GetMessage(),
            Code = diagnostic.Id,
            Severity = severity
        };
    }
}
