using Architect.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.RoslynParser.Rules;

public class LargeMethodRule : ICSharpRule
{
    public const int MaxAllowedLines = 30;

    public string RuleId => "ARCH-CS-003";
    public string RuleName => "AvoidLargeMethods";
    public RuleCategory Category => RuleCategory.SolidPrinciples;
    public AnalysisSeverity Severity => AnalysisSeverity.Warning;

    public IEnumerable<CodeViolation> Analyze(SyntaxTree tree, SemanticModel? semanticModel)
    {
        var root = tree.GetRoot();
        var methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methodDeclarations)
        {
            var lineSpan = method.GetLocation().GetLineSpan();
            var lines = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;

            if (lines > MaxAllowedLines)
            {
                yield return new CodeViolation(
                    RuleId,
                    RuleName,
                    $"'{method.Identifier.Text}' metodu {lines} satır uzunluğunda (Maksimum önerilen: {MaxAllowedLines}). Tek Sorumluluk Prensibi (SRP) ihlali olabilir.",
                    Severity,
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.EndLinePosition.Line + 1,
                    "Metodu daha küçük ve odaklı alt metodlara (Extract Method) bölün.",
                    Category
                );
            }
        }
    }
}
