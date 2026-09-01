using Architect.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.RoslynParser.Remediation;

public record SelfHealingResult(
    string RequestId,
    string FilePath,
    bool Healed,
    string OriginalCode,
    string HealedSourceCode,
    IReadOnlyList<string> AppliedFixes
);

public interface ISelfHealingRemediationEngine
{
    SelfHealingResult RemediateSourceCode(string requestId, string filePath, string sourceCode);
}

public class SelfHealingRewriter : CSharpSyntaxRewriter
{
    public List<string> AppliedFixes { get; } = new();

    // 1. 'async void' metodları 'async Task' yap
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var isAsync = node.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
        var isVoid = node.ReturnType is PredefinedTypeSyntax predefined &&
                     predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);

        if (isAsync && isVoid)
        {
            AppliedFixes.Add($"[Self-Healing]: '{node.Identifier.Text}' metodu 'async void' yerine 'async Task' olarak onarıldı.");
            var taskType = SyntaxFactory.IdentifierName("Task")
                .WithTrailingTrivia(SyntaxFactory.Space);
            
            node = node.WithReturnType(taskType);
        }

        return base.VisitMethodDeclaration(node);
    }

    // 2. Boş 'catch' bloklarına 'throw;' ekle
    public override SyntaxNode? VisitCatchClause(CatchClauseSyntax node)
    {
        if (node.Block.Statements.Count == 0)
        {
            AppliedFixes.Add("[Self-Healing]: Boş catch bloğuna 'throw;' ifadesi eklenerek sessiz çökme riski giderildi.");
            var throwStatement = SyntaxFactory.ThrowStatement()
                .WithLeadingTrivia(SyntaxFactory.Whitespace("            "))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            var newBlock = node.Block.WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(throwStatement));
            node = node.WithBlock(newBlock);
        }

        return base.VisitCatchClause(node);
    }
}

public class SelfHealingRemediationEngine : ISelfHealingRemediationEngine
{
    public SelfHealingResult RemediateSourceCode(string requestId, string filePath, string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            return new SelfHealingResult(requestId, filePath, false, sourceCode, sourceCode, Array.Empty<string>());
        }

        try
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();

            var rewriter = new SelfHealingRewriter();
            var newRoot = rewriter.Visit(root);

            var healedCode = newRoot.ToFullString();
            var healed = rewriter.AppliedFixes.Count > 0;

            return new SelfHealingResult(
                RequestId: requestId,
                FilePath: filePath,
                Healed: healed,
                OriginalCode: sourceCode,
                HealedSourceCode: healedCode,
                AppliedFixes: rewriter.AppliedFixes
            );
        }
        catch
        {
            return new SelfHealingResult(requestId, filePath, false, sourceCode, sourceCode, Array.Empty<string>());
        }
    }
}
