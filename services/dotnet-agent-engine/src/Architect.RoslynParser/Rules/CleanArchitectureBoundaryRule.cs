using Architect.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.RoslynParser.Rules;

public class CleanArchitectureBoundaryRule : ICSharpRule
{
    public string RuleId => "ARCH-CA-001";
    public string RuleName => "DomainLayerDependencyViolation";
    public RuleCategory Category => RuleCategory.CleanArchitecture;
    public AnalysisSeverity Severity => AnalysisSeverity.Error;

    private static readonly string[] ForbiddenDomainImports =
    [
        "Infrastructure",
        "EntityFrameworkCore",
        "Controllers",
        "AspNetCore",
        "SqlClient",
        "Npgsql",
        "MongoDB"
    ];

    public IEnumerable<CodeViolation> Analyze(SyntaxTree tree, SemanticModel? semanticModel)
    {
        var root = tree.GetRoot();
        
        // Sınıfın namespace'ini kontrol et (Domain veya Core mu?)
        var namespaceDeclarations = root.DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>();

        var isDomainNamespace = namespaceDeclarations.Any(ns => 
            ns.Name.ToString().Contains("Domain", StringComparison.OrdinalIgnoreCase) || 
            ns.Name.ToString().Contains("Core", StringComparison.OrdinalIgnoreCase));

        if (!isDomainNamespace)
        {
            yield break;
        }

        // Domain katmanındaki using direktiflerini tara
        var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>();

        foreach (var usingDirective in usingDirectives)
        {
            var importName = usingDirective.Name?.ToString() ?? string.Empty;

            foreach (var forbidden in ForbiddenDomainImports)
            {
                if (importName.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                {
                    var lineSpan = usingDirective.GetLocation().GetLineSpan();
                    yield return new CodeViolation(
                        RuleId,
                        RuleName,
                        $"Clean Architecture İhlali: Domain/Core katmanı doğrudan '{importName}' katmanına/kütüphanesine bağımlı olamaz.",
                        Severity,
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.EndLinePosition.Line + 1,
                        "Bağımlılık Tersine Çevirme (Dependency Inversion) uygulayın. Domain içinde bir Interface (arayüz) tanımlayın ve Infrastructure katmanında uygulayın.",
                        Category
                    );
                }
            }
        }
    }
}
