using Architect.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.RoslynParser.Rules;

/// <summary>
/// ARCH-CS-004: Magic Number tespiti.
/// Kod içindeki açıklamasız sabit sayısal değerleri tespit eder.
/// </summary>
public class MagicNumberRule : ICSharpRule
{
    public string RuleId => "ARCH-CS-004";
    public string RuleName => "AvoidMagicNumbers";
    public RuleCategory Category => RuleCategory.CodeSmell;
    public AnalysisSeverity Severity => AnalysisSeverity.Warning;

    private static readonly HashSet<string> AllowedValues = new() { "0", "1", "-1", "2" };

    public IEnumerable<CodeViolation> Analyze(SyntaxTree tree, SemanticModel? semanticModel)
    {
        var root = tree.GetRoot();
        var literals = root.DescendantNodes().OfType<LiteralExpressionSyntax>()
            .Where(l => l.IsKind(SyntaxKind.NumericLiteralExpression));

        foreach (var literal in literals)
        {
            var text = literal.Token.Text;
            if (AllowedValues.Contains(text)) continue;

            // const/enum tanımlarını atla
            var inConst = literal.Ancestors().OfType<FieldDeclarationSyntax>()
                .Any(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)));
            if (inConst) continue;

            var inEnumMember = literal.Ancestors().OfType<EnumMemberDeclarationSyntax>().Any();
            if (inEnumMember) continue;

            var lineSpan = literal.GetLocation().GetLineSpan();
            yield return new CodeViolation(
                RuleId, RuleName,
                $"Magic Number '{text}' tespit edildi. Açıklamasız sayısal değerler kodun okunabilirliğini düşürür.",
                Severity,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.EndLinePosition.Line + 1,
                $"'private const int SABIT_ADI = {text};' şeklinde isimlendirilmiş sabit kullanın.",
                Category
            );
        }
    }
}
