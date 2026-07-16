using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DSLDungeon.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DSLDungeon.Game.DSL;

public static class DslGraphValidator
{
    private const int WrapperLinesBeforeCode = 12;

    public static List<CompilationError> Validate(string code)
    {
        var errors = new List<CompilationError>();

        var wrappedCode = @"using System;
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
" + IndentCode(code) + @"
}";

        var tree = CSharpSyntaxTree.ParseText(wrappedCode);

        // Если есть синтаксические ошибки, отдаем их на обработку компилятору
        var syntaxDiagnostics = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (syntaxDiagnostics.Any())
        {
            return errors; 
        }

        var root = tree.GetRoot();
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
        var localFunctions = root.DescendantNodes().OfType<LocalFunctionStatementSyntax>().ToList();

        // Проверяем исключительно наличие точки входа Tick
        var tickMethod = methods.FirstOrDefault(m => m.Identifier.Text == "Tick");
        if (tickMethod == null)
        {
            var tickLocal = localFunctions.FirstOrDefault(f => f.Identifier.Text == "Tick");
            if (tickLocal == null)
            {
                errors.Add(new CompilationError
                {
                    Line = 1,
                    Column = 1,
                    Message = "Скрипт должен обязательно содержать точку входа: метод 'public void Tick(DslContext context)'.",
                    Severity = ErrorSeverity.Error
                });
            }
        }

        return errors;
    }

    private static string IndentCode(string code)
    {
        var normalized = code.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n');
        var indented = lines.Select(l => "    " + l);
        return string.Join("\n", indented);
    }
}