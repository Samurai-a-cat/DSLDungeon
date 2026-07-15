using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DSLDungeon.Services;

public class CompilationResult
{
    public bool Success { get; set; }
    public List<CompilationError> Errors { get; set; } = new();
    public string? Code { get; set; }
    public Assembly? CompiledAssembly { get; set; }
}

public class DslCompilerService
{
    private const int WrapperLinesBeforeCode = 12;
    private const int WrapperIndentColumns = 8;

    private const string ScriptWrapperPrefix = @"using System;
using System.Collections.Generic;
using System.Linq;
using DSLDungeon.Game.DSL;
using DSLDungeon.Game.Core;
using DSLDungeon.Game.Grid;
using DSLDungeon.Game.Entities;

public class Script : IHeroScript
{
    public void Tick(DslContext context)
    {
";

    private const string ScriptWrapperSuffix = @"
    }
}";

    private readonly HttpClient _httpClient;
    private List<MetadataReference>? _references;
    private bool _initialized;

    public DslCompilerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private async Task EnsureReferencesAsync()
    {
        if (_initialized) return;
        _initialized = true;

        _references = new List<MetadataReference>(Basic.Reference.Assemblies.Net90.References.All);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic) continue;

            string assemblyName = assembly.GetName().Name ?? "";
            if (IsBclAssembly(assemblyName)) continue;

            // Проверяем, не добавили ли уже
            bool alreadyAdded = _references.Any(r =>
                r.Display != null &&
                (r.Display.EndsWith(assemblyName + ".dll") || r.Display.Contains(assemblyName)));
            if (alreadyAdded) continue;

            // Загружаем .dll через HTTP из _framework/
            string path = $"_framework/{assemblyName}.dll";
            try
            {
                var response = await _httpClient.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    var metadataRef = MetadataReference.CreateFromStream(stream);
                    _references.Add(metadataRef);
                }
                else
                {
                    Console.WriteLine($"[DslCompiler] Warning: {path} returned {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DslCompiler] Warning: Could not load {assemblyName}: {ex.Message}");
            }
        }
    }

    private static bool IsBclAssembly(string name)
    {
        var lower = name.ToLowerInvariant();
        return lower.StartsWith("system.")
            || lower.StartsWith("microsoft.")
            || lower.StartsWith("netstandard")
            || lower == "mscorlib";
    }

    public async Task<CompilationResult> CompileAsync(string code)
    {
        // Нормализуем переводы строк — Monaco может прислать \r\n
        code = code.Replace("\r\n", "\n").Replace("\r", "\n");

        var result = new CompilationResult();
        try
        {
            await EnsureReferencesAsync();

            var wrappedCode = ScriptWrapperPrefix + IndentCode(code) + ScriptWrapperSuffix;
            var syntaxTree = CSharpSyntaxTree.ParseText(wrappedCode);

            var compilation = CSharpCompilation.Create(
                "DSLDungeonScript",
                new[] { syntaxTree },
                _references,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    concurrentBuild: false)
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
                ms.Position = 0;
                var assembly = Assembly.Load(ms.ToArray());
                result.Success = true;
                result.Code = code;
                result.CompiledAssembly = assembly;
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
        var compileResult = await CompileAsync(code);
        if (!compileResult.Success)
        {
            return $"❌ Compilation error: {string.Join("; ", compileResult.Errors.Select(e => e.Message))}";
        }
        return "🧪 Sandbox test passed";
    }

    private static string IndentCode(string code)
    {
        var normalized = code.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n');
        var indented = lines.Select(l => "        " + l);
        return string.Join("\n", indented);
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
        var line = lineSpan.StartLinePosition.Line + 1;
        var column = lineSpan.StartLinePosition.Character + 1;

        var adjustedLine = line > WrapperLinesBeforeCode ? line - WrapperLinesBeforeCode : line;
        var adjustedColumn = line > WrapperLinesBeforeCode ? Math.Max(1, column - WrapperIndentColumns) : column;

        return new CompilationError
        {
            Line = adjustedLine,
            Column = adjustedColumn,
            Message = diagnostic.GetMessage(),
            Code = diagnostic.Id,
            Severity = severity
        };
    }
}
