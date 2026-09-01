using System.Text.RegularExpressions;
using Architect.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.RoslynParser.Rules;

public partial class HardcodedSecretRule : ICSharpRule
{
    public string RuleId => "ARCH-SEC-001";
    public string RuleName => "HardcodedSecretDetected";
    public RuleCategory Category => RuleCategory.Security;
    public AnalysisSeverity Severity => AnalysisSeverity.Critical;

    private static readonly Regex SecretVariablePattern = new(
        @"(?i)(password|secret|api_?key|bearer|token|private_?key|conn_?str)",
        RegexOptions.Compiled
    );

    public IEnumerable<CodeViolation> Analyze(SyntaxTree tree, SemanticModel? semanticModel)
    {
        var root = tree.GetRoot();
        var variableDeclarators = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();

        foreach (var declarator in variableDeclarators)
        {
            var varName = declarator.Identifier.Text;
            if (SecretVariablePattern.IsMatch(varName) && declarator.Initializer?.Value is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var stringVal = literal.Token.ValueText;
                if (!string.IsNullOrWhiteSpace(stringVal) && stringVal.Length > 4)
                {
                    var lineSpan = declarator.GetLocation().GetLineSpan();
                    yield return new CodeViolation(
                        RuleId,
                        RuleName,
                        $"'{varName}' değişkeninde sabit kodlanmış gizli anahtar (hardcoded secret/password) tespit edildi.",
                        Severity,
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.EndLinePosition.Line + 1,
                        "Gizli değerleri kaynak kodda tutmayın. 'IConfiguration', 'Environment.GetEnvironmentVariable' veya 'Azure Key Vault / AWS Secrets Manager' kullanın.",
                        Category
                    );
                }
            }
        }
    }
}
