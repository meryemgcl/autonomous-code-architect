using Architect.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.RoslynParser.Rules;

/// <summary>
/// ARCH-CS-005: Public metodların null dönmemesi gerekir.
/// NullReferenceException riskini minimize etmek için Result<T> veya boş koleksiyon önerir.
/// </summary>
public class NullReturnFromPublicMethodRule : ICSharpRule
{
    public string RuleId => "ARCH-CS-005";
    public string RuleName => "AvoidNullReturnFromPublicMethods";
    public RuleCategory Category => RuleCategory.CodeSmell;
    public AnalysisSeverity Severity => AnalysisSeverity.Warning;

    public IEnumerable<CodeViolation> Analyze(SyntaxTree tree, SemanticModel? semanticModel)
    {
        var root = tree.GetRoot();
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)));

        foreach (var method in methods)
        {
            var returnType = method.ReturnType.ToString();
            if (returnType is "void" or "Task" or "ValueTask") continue;

            var nullReturns = method.DescendantNodes()
                .OfType<ReturnStatementSyntax>()
                .Where(r => r.Expression is LiteralExpressionSyntax lit &&
                            lit.IsKind(SyntaxKind.NullLiteralExpression));

            foreach (var ret in nullReturns)
            {
                var lineSpan = ret.GetLocation().GetLineSpan();
                yield return new CodeViolation(
                    RuleId, RuleName,
                    $"Public metod '{method.Identifier.Text}' null dönüyor. NullReferenceException riski yüksektir.",
                    Severity,
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.EndLinePosition.Line + 1,
                    "null yerine 'Result<T>', boş koleksiyon veya özel exception kullanın.",
                    Category
                );
            }
        }
    }
}
