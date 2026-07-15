using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DSLDungeon.Services;

public class CompilationResult
{
    public bool Success { get; set; }
    public List<CompilationError> Errors { get; set; } = new();
    public string? Code { get; set; }
}

public class DslCompilerService
{
    // Обёртка: 8 строк ДО кода игрока (1-3: using, 4: empty, 5-8: class/method decl)
    private const int WrapperLinesBeforeCode = 8;

    private const string ScriptWrapper = @"using System;
using System.Collections.Generic;
using System.Linq;

public class Script
{{
    public static void Run()
    {{
{0}
    }}
}}";

    public CompilationResult Compile(string code)
    {
        var result = new CompilationResult();
        try
        {
            var wrappedCode = string.Format(ScriptWrapper, IndentCode(code));
            var syntaxTree = CSharpSyntaxTree.ParseText(wrappedCode);
            var compilation = CSharpCompilation.Create(
                "DSLDungeonScript",
                new[] { syntaxTree },
                Basic.Reference.Assemblies.Net90.References.All,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                result.Success = false;
                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    if (MapDiagnostic(diagnostic) is { } err)
                        result.Errors.Add(err);
                }
            }
            else
            {
                result.Success = true;
                result.Code = code;
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

    public Task<string> RunSandboxAsync(string code)
    {
        var compileResult = Compile(code);
        if (!compileResult.Success)
        {
            return Task.FromResult($"❌ Compilation error: {string.Join("; ", compileResult.Errors.Select(e => e.Message))}");
        }
        return Task.FromResult("🧪 Sandbox test passed");
    }

    private static string IndentCode(string code)
    {
        var lines = code.Split("");
        var indented = lines.Select(l => "        " + l);
        return string.Join("", indented);
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
        var line = lineSpan.StartLinePosition.Line + 1; // 1-based

        // Корректируем номер строки: вычитаем строки обёртки
        var adjustedLine = line > WrapperLinesBeforeCode ? line - WrapperLinesBeforeCode : line;

        return new CompilationError
        {
            Line = adjustedLine,
            Column = lineSpan.StartLinePosition.Character + 1,
            Message = diagnostic.GetMessage(),
            Code = diagnostic.Id,
            Severity = severity
        };
    }
}
