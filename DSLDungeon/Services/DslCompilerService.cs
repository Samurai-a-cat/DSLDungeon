using System.IO;
using System.Reflection;
using DSLDungeon.Game.DSL;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    private const int WrapperLinesBeforeCode = 13;
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
    private int __dslOps;
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


    /// <summary>
    /// Проверяет код на запрещённые вызовы (File, Process, Thread и т.д.).
    /// Работает ДО компиляции — на уровне синтаксического дерева.
    /// </summary>
    private static List<CompilationError> ValidateSecurity(SyntaxNode root)
    {
        var errors = new List<CompilationError>();

        var forbiddenCalls = new[]
        {
            "File.", "Directory.", "Path.", "Process.", "Thread.", "Task.",
            "Assembly.", "Type.Get", "Activator.", "Environment.",
            "Console.", "HttpClient.", "WebRequest.", "Socket.",
            "Marshal.", "Unsafe.", "GC.", "Debugger.",
            "AppDomain.", "Reflection."
        };

        // Запрещённые вызовы методов: File.ReadAllText, Process.Start и т.д.
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var target = invocation.Expression.ToString();
            foreach (var forbidden in forbiddenCalls)
            {
                if (target.Contains(forbidden))
                {
                    var span = invocation.GetLocation().GetLineSpan();
                    errors.Add(new CompilationError
                    {
                        Line = span.StartLinePosition.Line + 1,
                        Column = span.StartLinePosition.Character + 1,
                        Message = $"Вызов '{target}' запрещён в DSL. Используйте только context.*, Math.* и базовые операции.",
                        Severity = ErrorSeverity.Error
                    });
                    break;
                }
            }
        }

        // Запрещённые создания объектов: new Thread(), new Task(), new FileStream()
        var forbiddenTypes = new[] { "Thread", "Task", "FileStream", "HttpClient", "Socket", "Process" };
        foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var typeName = creation.Type.ToString();
            foreach (var forbidden in forbiddenTypes)
            {
                if (typeName.Contains(forbidden))
                {
                    var span = creation.GetLocation().GetLineSpan();
                    errors.Add(new CompilationError
                    {
                        Line = span.StartLinePosition.Line + 1,
                        Column = span.StartLinePosition.Character + 1,
                        Message = $"Создание объектов типа '{typeName}' запрещено в DSL.",
                        Severity = ErrorSeverity.Error
                    });
                    break;
                }
            }
        }

        return errors;
    }

    public async Task<CompilationResult> CompileAsync(string code)
    {
        // Нормализуем переводы строк — Monaco может прислать \r\n
        code = code.Replace("\r\n", "\n").Replace("\r", "\n");

        // Проверка структуры графа: каждая ветвь должна завершаться командой
        var graphErrors = DslGraphValidator.Validate(code);
        if (graphErrors.Count > 0)
        {
            return new CompilationResult
            {
                Success = false,
                Errors = graphErrors
            };
        }

        // Проверка безопасности ДО компиляции
        var securityTree = CSharpSyntaxTree.ParseText(code);
        var securityErrors = ValidateSecurity(securityTree.GetRoot());
        if (securityErrors.Count > 0)
        {
            return new CompilationResult
            {
                Success = false,
                Errors = securityErrors
            };
        }

        var result = new CompilationResult();
        try
        {
            await EnsureReferencesAsync();

            // Инжектируем защиту от бесконечных циклов
            var playerTree = CSharpSyntaxTree.ParseText(code);
            var injector = new DslSafetyInjector();
            var safeCode = injector.Visit(playerTree.GetRoot()).ToFullString();

            var wrappedCode = ScriptWrapperPrefix + IndentCode(safeCode) + ScriptWrapperSuffix;
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
