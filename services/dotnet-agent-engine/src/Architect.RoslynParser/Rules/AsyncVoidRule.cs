using Architect.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.RoslynParser.Rules;

public class AsyncVoidRule : ICSharpRule
{
    public string RuleId => "ARCH-CS-001";
    public string RuleName => "AvoidAsyncVoidMethods";
    public RuleCategory Category => RuleCategory.CodeSmell;
    public AnalysisSeverity Severity => AnalysisSeverity.Critical;

    public IEnumerable<CodeViolation> Analyze(SyntaxTree tree, SemanticModel? semanticModel)
    {
        var root = tree.GetRoot();
        var methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methodDeclarations)
        {
            var isAsync = method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
            var isVoid = method.ReturnType is PredefinedTypeSyntax predefinedType &&
                         predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword);

            if (isAsync && isVoid)
            {
                var lineSpan = method.GetLocation().GetLineSpan();
                var startLine = lineSpan.StartLinePosition.Line + 1;
                var endLine = lineSpan.EndLinePosition.Line + 1;

                yield return new CodeViolation(
                    RuleId,
                    RuleName,
                    $"Metod '{method.Identifier.Text}' 'async void' olarak tanımlanmış. Yakalanmayan istisnalar uygulamayı çökertebilir.",
                    Severity,
                    startLine,
                    endLine,
                    $"'async void {method.Identifier.Text}' yerine 'async Task {method.Identifier.Text}' kullanın.",
                    Category
                );
            }
        }
    }
}
