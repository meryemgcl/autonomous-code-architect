using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.RoslynParser.Metrics;

public class CyclomaticComplexityWalker : CSharpSyntaxWalker
{
    public int Complexity { get; private set; } = 1; // Base complexity is 1

    public override void VisitIfStatement(IfStatementSyntax node)
    {
        Complexity++;
        base.VisitIfStatement(node);
    }

    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        Complexity++;
        base.VisitWhileStatement(node);
    }

    public override void VisitForStatement(ForStatementSyntax node)
    {
        Complexity++;
        base.VisitForStatement(node);
    }

    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        Complexity++;
        base.VisitForEachStatement(node);
    }

    public override void VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
    {
        Complexity++;
        base.VisitCaseSwitchLabel(node);
    }

    public override void VisitCatchClause(CatchClauseSyntax node)
    {
        Complexity++;
        base.VisitCatchClause(node);
    }

    public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
    {
        Complexity++; // Ternary ? :
        base.VisitConditionalExpression(node);
    }

    public override void VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        if (node.IsKind(SyntaxKind.LogicalAndExpression) ||
            node.IsKind(SyntaxKind.LogicalOrExpression) ||
            node.IsKind(SyntaxKind.CoalesceExpression))
        {
            Complexity++;
        }
        base.VisitBinaryExpression(node);
    }
}
