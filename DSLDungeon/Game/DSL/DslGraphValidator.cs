using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DSLDungeon.Services;

namespace DSLDungeon.Game.DSL;

/// <summary>
/// Статический анализатор: каждая ветвь графа потока управления
/// должна завершиться вызовом context.XXX команды (или return/throw).
/// </summary>
public static class DslGraphValidator
{
    private static readonly string[] CommandPrefixes = new[]
    {
        "context.Move",
        "context.Attack",
        "context.Wait",
    };

    public static List<CompilationError> Validate(string code)
    {
        var errors = new List<CompilationError>();
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        // Находим все методы (включая локальные функции)
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            ValidateMethod(method.Body, method.Identifier.Text, errors);
        }

        // Локальные функции
        foreach (var localFunc in root.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
        {
            if (localFunc.Body != null)
                ValidateMethod(localFunc.Body, localFunc.Identifier.Text, errors);
        }

        // Если нет явных методов — проверяем как тело скрипта (код игрока без методов)
        if (!root.DescendantNodes().OfType<MethodDeclarationSyntax>().Any()
            && !root.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Any())
        {
            var statements = root.DescendantNodes().OfType<GlobalStatementSyntax>()
                .Select(g => g.Statement)
                .ToList();

            if (statements.Count > 0)
            {
                var result = AnalyzeStatements(statements);
                if (!result.HasCommand && !result.AlwaysReturns)
                {
                    errors.Add(new CompilationError
                    {
                        Line = statements.Last().GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Column = 1,
                        Message = "Скрипт должен завершаться вызовом команды (context.Move, context.Attack, context.Wait) или return.",
                        Severity = ErrorSeverity.Error
                    });
                }
            }
        }

        return errors;
    }

    private static void ValidateMethod(BlockSyntax? body, string methodName, List<CompilationError> errors)
    {
        if (body == null) return;

        var result = AnalyzeStatements(body.Statements);
        if (!result.HasCommand && !result.AlwaysReturns)
        {
            errors.Add(new CompilationError
            {
                Line = body.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Column = 1,
                Message = $"Метод '{methodName}' должен содержать вызов команды (context.Move, context.Attack, context.Wait) или return на каждом пути.",
                Severity = ErrorSeverity.Error
            });
        }
    }

    /// <summary>
    /// Анализирует список statements. Возвращает:
    /// - HasCommand: есть ли хотя бы одна команда на ВСЕХ путях
    /// - AlwaysReturns: гарантированно ли выполняется return/throw
    /// </summary>
    private static FlowResult AnalyzeStatements(IEnumerable<StatementSyntax> statements)
    {
        bool hasCommand = false;
        bool alwaysReturns = false;

        foreach (var stmt in statements)
        {
            var result = AnalyzeStatement(stmt);

            if (result.AlwaysReturns)
            {
                alwaysReturns = true;
                break; // Дальнейший код недостижим
            }

            if (result.HasCommand)
                hasCommand = true;
        }

        return new FlowResult(hasCommand, alwaysReturns);
    }

    private static FlowResult AnalyzeStatement(StatementSyntax stmt)
    {
        switch (stmt)
        {
            case ExpressionStatementSyntax expr:
                return new FlowResult(IsCommandCall(expr.Expression), false);

            case ReturnStatementSyntax:
            case ThrowStatementSyntax:
                return new FlowResult(false, true);

            case BlockSyntax block:
                return AnalyzeStatements(block.Statements);

            case IfStatementSyntax ifStmt:
                return AnalyzeIf(ifStmt);

            case WhileStatementSyntax whileStmt:
                return AnalyzeLoop(whileStmt.Statement);

            case ForStatementSyntax forStmt:
                return AnalyzeLoop(forStmt.Statement);

            case ForEachStatementSyntax foreachStmt:
                return AnalyzeLoop(foreachStmt.Statement);

            case DoStatementSyntax doStmt:
                return AnalyzeLoop(doStmt.Statement);

            case LocalDeclarationStatementSyntax:
                return new FlowResult(false, false);

            default:
                // Для неизвестных конструкций — консервативно считаем, что команды нет
                return new FlowResult(false, false);
        }
    }

    private static FlowResult AnalyzeIf(IfStatementSyntax ifStmt)
    {
        // Then-ветвь
        var thenResult = AnalyzeStatement(ifStmt.Statement);

        // Else-ветвь (если нет else — значит есть путь "ничего не делать")
        var elseResult = ifStmt.Else != null
            ? AnalyzeStatement(ifStmt.Else.Statement)
            : new FlowResult(false, false);

        // Если нет else — значит один из путей (пропуск if) не содержит команды
        if (ifStmt.Else == null)
        {
            // Путь "if не сработал" — пустой, значит нет гарантии команды
            return new FlowResult(
                HasCommand: thenResult.HasCommand && elseResult.HasCommand,
                AlwaysReturns: thenResult.AlwaysReturns && elseResult.AlwaysReturns
            );
        }

        // Обе ветви должны иметь команду ИЛИ return
        bool allPathsHaveCommand = thenResult.HasCommand && elseResult.HasCommand;
        bool allPathsReturn = thenResult.AlwaysReturns && elseResult.AlwaysReturns;

        return new FlowResult(allPathsHaveCommand, allPathsReturn);
    }

    private static FlowResult AnalyzeLoop(StatementSyntax body)
    {
        var bodyResult = AnalyzeStatement(body);

        // Цикл сам по себе не гарантирует return (может быть 0 итераций)
        // Но если тело всегда возвращает — то цикл либо возвращает, либо не выполняется
        bool alwaysReturns = bodyResult.AlwaysReturns;

        return new FlowResult(bodyResult.HasCommand, alwaysReturns);
    }

    private static bool IsCommandCall(ExpressionSyntax expr)
    {
        var text = expr.ToString();
        return CommandPrefixes.Any(prefix => text.StartsWith(prefix));
    }

    private readonly record struct FlowResult(bool HasCommand, bool AlwaysReturns);
}
