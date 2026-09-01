using Architect.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Architect.RoslynParser.Rules;

public interface ICSharpRule
{
    string RuleId { get; }
    string RuleName { get; }
    RuleCategory Category { get; }
    AnalysisSeverity Severity { get; }
    
    IEnumerable<CodeViolation> Analyze(SyntaxTree tree, SemanticModel? semanticModel);
}
