using Architect.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.RoslynParser.Rules;

/// <summary>
/// ARCH-CS-006: Static mutable field thread-safety kuralı.
/// Static + non-readonly field'lar race condition riski yaratır.
/// </summary>
public class StaticMutableFieldRule : ICSharpRule
{
    public string RuleId => "ARCH-CS-006";
    public string RuleName => "AvoidStaticMutableFields";
    public RuleCategory Category => RuleCategory.CodeSmell;
    public AnalysisSeverity Severity => AnalysisSeverity.Warning;

    public IEnumerable<CodeViolation> Analyze(SyntaxTree tree, SemanticModel? semanticModel)
    {
        var root = tree.GetRoot();
        var fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>();

        foreach (var field in fields)
        {
            var isStatic   = field.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
            var isReadonly = field.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));
            var isConst    = field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword));

            if (!isStatic || isReadonly || isConst) continue;

            var lineSpan = field.GetLocation().GetLineSpan();
            var fieldNames = string.Join(", ", field.Declaration.Variables.Select(v => v.Identifier.Text));

            yield return new CodeViolation(
                RuleId, RuleName,
                $"Static mutable field tespit edildi: '{fieldNames}'. Thread-safety riski mevcuttur.",
                Severity,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.EndLinePosition.Line + 1,
                "'readonly' ekleyin veya 'Interlocked', 'ConcurrentDictionary' gibi thread-safe yapılar kullanın.",
                Category
            );
        }
    }
}
