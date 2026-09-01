using Architect.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.RoslynParser.Rules;

public class EmptyCatchBlockRule : ICSharpRule
{
    public string RuleId => "ARCH-CS-002";
    public string RuleName => "AvoidEmptyCatchBlocks";
    public RuleCategory Category => RuleCategory.CodeSmell;
    public AnalysisSeverity Severity => AnalysisSeverity.Warning;

    public IEnumerable<CodeViolation> Analyze(SyntaxTree tree, SemanticModel? semanticModel)
    {
        var root = tree.GetRoot();
        var catchClauses = root.DescendantNodes().OfType<CatchClauseSyntax>();

        foreach (var catchClause in catchClauses)
        {
            if (catchClause.Block.Statements.Count == 0)
            {
                var lineSpan = catchClause.GetLocation().GetLineSpan();
                var startLine = lineSpan.StartLinePosition.Line + 1;
                var endLine = lineSpan.EndLinePosition.Line + 1;

                yield return new CodeViolation(
                    RuleId,
                    RuleName,
                    "Boş 'catch' bloğu tespit edildi. Hataların yutulması sistemde sessiz çökmelere yol açar.",
                    Severity,
                    startLine,
                    endLine,
                    "Hatayı loglayın (ILogger) veya 'throw;' ile üst katmana iletin.",
                    Category
                );
            }
        }
    }
}
