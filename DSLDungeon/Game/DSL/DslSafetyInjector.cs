using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DSLDungeon.Game.DSL;

/// <summary>
/// Инжектирует счётчик операций в код игрока ДО компиляции.
/// Вставляет проверку в начало каждого метода и в начало каждого цикла.
/// Если скрипт делает больше 10000 шагов — бросает TimeoutException.
/// </summary>
public class DslSafetyInjector : CSharpSyntaxRewriter
{
    private static readonly StatementSyntax OpsCheck = SyntaxFactory.ParseStatement(
        @"if (++__dslOps > 10000) throw new System.TimeoutException(""DSL operation limit exceeded"");");

    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);
        if (visited.Body == null) return visited;

        var newStatements = new SyntaxList<StatementSyntax>()
            .Add(OpsCheck)
            .AddRange(visited.Body.Statements);

        return visited.WithBody(visited.Body.WithStatements(newStatements));
    }

    public override SyntaxNode VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        var visited = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node);
        if (visited.Body == null) return visited;

        var newStatements = new SyntaxList<StatementSyntax>()
            .Add(OpsCheck)
            .AddRange(visited.Body.Statements);

        return visited.WithBody(visited.Body.WithStatements(newStatements));
    }

    public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
    {
        var visited = (WhileStatementSyntax)base.VisitWhileStatement(node);
        return InjectIntoLoopBody(visited, visited.Statement, b => visited.WithStatement(b));
    }

    public override SyntaxNode VisitForStatement(ForStatementSyntax node)
    {
        var visited = (ForStatementSyntax)base.VisitForStatement(node);
        return InjectIntoLoopBody(visited, visited.Statement, b => visited.WithStatement(b));
    }

    public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
    {
        var visited = (ForEachStatementSyntax)base.VisitForEachStatement(node);
        return InjectIntoLoopBody(visited, visited.Statement, b => visited.WithStatement(b));
    }

    public override SyntaxNode VisitDoStatement(DoStatementSyntax node)
    {
        var visited = (DoStatementSyntax)base.VisitDoStatement(node);
        return InjectIntoLoopBody(visited, visited.Statement, b => visited.WithStatement(b));
    }

    private static TNode InjectIntoLoopBody<TNode>(TNode node, StatementSyntax body, Func<StatementSyntax, TNode> withStatement)
        where TNode : SyntaxNode
    {
        if (body is BlockSyntax block)
        {
            var newStatements = new SyntaxList<StatementSyntax>()
                .Add(OpsCheck)
                .AddRange(block.Statements);
            return withStatement(block.WithStatements(newStatements));
        }
        else
        {
            // while (x) Foo(); → while (x) { check; Foo(); }
            var newBlock = SyntaxFactory.Block(OpsCheck, body);
            return withStatement(newBlock);
        }
    }
}
