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
    private const int WrapperLinesBeforeCode = 12; // Изменилось из-за нового поля __dslCallDepth
    private const int WrapperIndentColumns = 4;

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
    private int __dslCallDepth;
";

    private const string ScriptWrapperSuffix = @"
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

            bool alreadyAdded = _references.Any(r =>
                r.Display != null &&
                (r.Display.EndsWith(assemblyName + ".dll") || r.Display.Contains(assemblyName)));
            if (alreadyAdded) continue;

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

    private static List<CompilationError> ValidateSecurity(SyntaxNode root)
    {
        var errors = new List<CompilationError>();

        var forbiddenIdentifiers = new HashSet<string>(StringComparer.Ordinal)
        {
            "File", "Directory", "Path", "Process", "Thread", "Task",
            "Assembly", "Type", "Activator", "Environment",
            "Console", "HttpClient", "WebRequest", "Socket",
            "Marshal", "Unsafe", "GC", "Debugger",
            "AppDomain", "Reflection", "WebClient", "DllImport", "Win32",
            "DSLDungeon",
            
            // ---- ЗАЩИТА ОТ JS-ИНЪЕКЦИЙ И СИСТЕМНОГО ИНТЕРОПА ----
            "JSImport",               // Блокирует [JSImport] интероп .NET 7+
            "JSExport",               // Блокирует [JSExport] интероп .NET 7+
            "JSRuntime",              // Блокирует доступ к JSRuntime Blazor
            "IJSObjectReference",     // Блокирует ссылки на объекты JS
            "IJSStreamReference",      // Блокирует стримы данных в JS
            "IJSUnmarshalledRuntime", // Блокирует старый немаршалированный интероп
            "JSHost",                 // Блокирует хост-команды WebAssembly
            "LibraryImport"           // Блокирует современный аналог DllImport в .NET 7+
        };

        foreach (var id in root.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        {
            var name = id.Identifier.ValueText;
            if (forbiddenIdentifiers.Contains(name))
            {
                var span = id.GetLocation().GetLineSpan();
                errors.Add(new CompilationError
                {
                    Line = span.StartLinePosition.Line + 1,
                    Column = span.StartLinePosition.Character + 1,
                    Message = $"Использование '{name}' запрещено в целях безопасности DSL. Попытки выполнения JavaScript-кода пресекаются.",
                    Severity = ErrorSeverity.Error
                });
            }
        }

        return errors;
    }

    public async Task<CompilationResult> CompileAsync(string code)
    {
        code = code.Replace("\r\n", "\n").Replace("\r", "\n");

        var rawTree = CSharpSyntaxTree.ParseText(code);

        // 1. Проверка безопасности сырого AST
        var securityErrors = ValidateSecurity(rawTree.GetRoot());
        if (securityErrors.Count > 0)
        {
            return new CompilationResult
            {
                Success = false,
                Errors = securityErrors
            };
        }

        // 2. Валидация наличия точки входа
        var graphErrors = DslGraphValidator.Validate(code);
        if (graphErrors.Count > 0)
        {
            return new CompilationResult
            {
                Success = false,
                Errors = graphErrors
            };
        }

        var result = new CompilationResult();
        try
        {
            await EnsureReferencesAsync();

            var injector = new DslSafetyInjector();
            var safeCode = injector.Visit(rawTree.GetRoot()).ToFullString();

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

                bool hasClassLevelStatementErrors = emitResult.Diagnostics.Any(d => 
                    d.Id == "CS1519" || d.Id == "CS0825" || d.Id == "CS8803" || d.Id == "CS8805" || 
                    d.Id == "CS0236" || d.Id == "CS8641" || d.Id == "CS0535");

                if (hasClassLevelStatementErrors)
                {
                    result.Errors.Add(new CompilationError
                    {
                        Line = 1,
                        Column = 1,
                        Message = "⚠️ Ошибка архитектуры скрипта: в новой версии весь исполняемый код должен находиться внутри методов. Пожалуйста, оберните вашу логику в метод: 'public void Tick(DslContext context) { ... }'. (Если вы видите эту ошибку после обновления, очистите редактор или нажмите 'Сбросить', чтобы применить новый шаблон).",
                        Severity = ErrorSeverity.Error
                    });
                }
                else
                {
                    foreach (var diagnostic in emitResult.Diagnostics)
                    {
                        if (MapDiagnostic(diagnostic, isWrapped: true) is { } err)
                            result.Errors.Add(err);
                    }
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
        var indented = lines.Select(l => "    " + l);
        return string.Join("\n", indented);
    }

    private static CompilationError? MapDiagnostic(Diagnostic diagnostic, bool isWrapped)
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

        int adjustedLine = line;
        int adjustedColumn = column;

        if (isWrapped && line > WrapperLinesBeforeCode)
        {
            adjustedLine = line - WrapperLinesBeforeCode;
            adjustedColumn = Math.Max(1, column - WrapperIndentColumns);
        }

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