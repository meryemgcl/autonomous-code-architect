using System.Diagnostics;
using Architect.Core.Models;
using Architect.RoslynParser.Metrics;
using Architect.RoslynParser.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.RoslynParser.Services;

public interface ICSharpAstAnalyzer
{
    AnalysisResult AnalyzeCode(string requestId, string filePath, string sourceCode);
}

public class CSharpAstAnalyzer : ICSharpAstAnalyzer
{
    private readonly List<ICSharpRule> _rules;

    public CSharpAstAnalyzer(IEnumerable<ICSharpRule>? customRules = null)
    {
        var ruleList = customRules?.ToList();
        if (ruleList == null || ruleList.Count == 0)
        {
            _rules = new List<ICSharpRule>
            {
                new AsyncVoidRule(),
                new EmptyCatchBlockRule(),
                new LargeMethodRule(),
                new HardcodedSecretRule(),
                new CleanArchitectureBoundaryRule(),
                new MagicNumberRule(),
                new NullReturnFromPublicMethodRule(),
                new StaticMutableFieldRule()
            };
        }
        else
        {
            _rules = ruleList;
        }
    }

    public AnalysisResult AnalyzeCode(string requestId, string filePath, string sourceCode)
    {
        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            return new AnalysisResult(
                requestId,
                filePath,
                false,
                "Source code is empty",
                new CodeMetrics(0, 0, 0, 0, 0),
                Array.Empty<CodeViolation>(),
                0
            );
        }

        try
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();

            // Calculate Metrics
            var linesOfCode = sourceCode.Split('\n').Length;
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();

            var complexityWalker = new CyclomaticComplexityWalker();
            complexityWalker.Visit(root);
            var complexity = complexityWalker.Complexity;

            var maintainabilityIndex = Math.Max(0, Math.Min(100, 100 - (complexity * 3) - (linesOfCode / 10)));

            var metrics = new CodeMetrics(
                LinesOfCode: linesOfCode,
                CyclomaticComplexity: complexity,
                MaintainabilityIndex: maintainabilityIndex,
                MethodCount: methods,
                ClassCount: classes
            );

            // Run Rules
            var violations = new List<CodeViolation>();
            foreach (var rule in _rules)
            {
                violations.AddRange(rule.Analyze(tree, semanticModel: null));
            }

            stopwatch.Stop();

            return new AnalysisResult(
                RequestId: requestId,
                FilePath: filePath,
                Success: true,
                ErrorMessage: null,
                Metrics: metrics,
                Violations: violations,
                ExecutionTimeMs: stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new AnalysisResult(
                RequestId: requestId,
                FilePath: filePath,
                Success: false,
                ErrorMessage: ex.Message,
                Metrics: new CodeMetrics(0, 0, 0, 0, 0),
                Violations: Array.Empty<CodeViolation>(),
                ExecutionTimeMs: stopwatch.ElapsedMilliseconds
            );
        }
    }
}
