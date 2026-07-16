using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DSLDungeon.Game.DSL;

public class DslSafetyInjector : CSharpSyntaxRewriter
{
    private static readonly StatementSyntax OpsCheck = SyntaxFactory.ParseStatement(
        @"if (++__dslOps > 10000) throw new System.TimeoutException(""DSL operation limit exceeded"");");

    private static readonly StatementSyntax DepthCheck = SyntaxFactory.ParseStatement(
        @"if (++__dslCallDepth > 100) throw new System.TimeoutException(""DSL stack depth limit exceeded"");");

    private static readonly StatementSyntax DepthDecrement = SyntaxFactory.ParseStatement(
        @"__dslCallDepth--;");

    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);

        // Конвертируем выражение-тело (=>) в обычный блок перед инжекцией
        if (visited.ExpressionBody != null)
        {
            StatementSyntax stmt = visited.ReturnType.ToString() == "void"
                ? SyntaxFactory.ExpressionStatement(visited.ExpressionBody.Expression)
                : SyntaxFactory.ReturnStatement(visited.ExpressionBody.Expression);

            var block = SyntaxFactory.Block(stmt);
            visited = visited
                .WithExpressionBody(null)
                .WithSemicolonToken(default)
                .WithBody(block);
        }

        if (visited.Body == null) return visited;

        // Оборачиваем оригинальное тело метода в try-finally для безопасного декремента глубины стека
        var tryFinally = SyntaxFactory.TryStatement(
            visited.Body,
            SyntaxFactory.List<CatchClauseSyntax>(),
            SyntaxFactory.FinallyClause(SyntaxFactory.Block(DepthDecrement))
        );

        var newStatements = new SyntaxList<StatementSyntax>()
            .Add(OpsCheck)
            .Add(DepthCheck)
            .Add(tryFinally);

        return visited.WithBody(SyntaxFactory.Block(newStatements));
    }

    public override SyntaxNode VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        var visited = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node);

        if (visited.ExpressionBody != null)
        {
            StatementSyntax stmt = visited.ReturnType.ToString() == "void"
                ? SyntaxFactory.ExpressionStatement(visited.ExpressionBody.Expression)
                : SyntaxFactory.ReturnStatement(visited.ExpressionBody.Expression);

            var block = SyntaxFactory.Block(stmt);
            visited = visited
                .WithExpressionBody(null)
                .WithSemicolonToken(default)
                .WithBody(block);
        }

        if (visited.Body == null) return visited;

        var tryFinally = SyntaxFactory.TryStatement(
            visited.Body,
            SyntaxFactory.List<CatchClauseSyntax>(),
            SyntaxFactory.FinallyClause(SyntaxFactory.Block(DepthDecrement))
        );

        var newStatements = new SyntaxList<StatementSyntax>()
            .Add(OpsCheck)
            .Add(DepthCheck)
            .Add(tryFinally);

        return visited.WithBody(SyntaxFactory.Block(newStatements));
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
            var newBlock = SyntaxFactory.Block(OpsCheck, body);
            return withStatement(newBlock);
        }
    }
}