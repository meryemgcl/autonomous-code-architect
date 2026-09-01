using System.Text;
using Architect.Agents.Abstractions;
using Architect.Agents.Models;
using Architect.Core.Models;
using Architect.Infrastructure.Memory;

namespace Architect.Agents.Specialists;

public class ReviewerAgent : IAgent
{
    private readonly IVectorMemoryService? _vectorMemory;

    public string AgentName => "ReviewerAgent";
    public string RoleTitle => "Senior Software & Architecture Reviewer";

    public ReviewerAgent(IVectorMemoryService? vectorMemory = null)
    {
        _vectorMemory = vectorMemory;
    }

    public async Task<AgentOpinion> EvaluateAsync(DebateContext context, CancellationToken cancellationToken = default)
    {
        var violations = context.DeterministicFindings.Violations;
        var metrics = context.DeterministicFindings.Metrics;

        var solidViolations = violations.Where(v => v.Category == RuleCategory.SolidPrinciples || 
                                                    v.Category == RuleCategory.CleanArchitecture ||
                                                    v.Category == RuleCategory.CodeSmell).ToList();

        var sb = new StringBuilder();
        var patches = new List<string>();

        sb.AppendLine($"### 🧐 {RoleTitle} Değerlendirmesi");
        sb.AppendLine($"- **İncelenen Dosya:** `{context.FilePath}` ({context.Language})");
        sb.AppendLine($"- **Karmaşıklık Skoru (CC):** {metrics.CyclomaticComplexity} | **Bakım Edilebilirlik:** {metrics.MaintainabilityIndex}/100");

        // RAG Vektör Hafızasından İlgili Kurumsal Kuralları Getir
        if (_vectorMemory != null)
        {
            var query = $"Clean Architecture SOLID Complexity {string.Join(" ", solidViolations.Select(v => v.RuleName))}";
            var relevantRules = await _vectorMemory.FindRelevantRulesAsync(query, topK: 2, cancellationToken);
            
            if (relevantRules.Count > 0)
            {
                sb.AppendLine("\n**📚 Eşleşen Kurumsal Mimari Standartları (RAG Hafızası):**");
                foreach (var r in relevantRules)
                {
                    sb.AppendLine($"- 🔹 **[{r.Rule.RuleCode}] {r.Rule.RuleName}** *(Benzerlik Skoru: %{r.SimilarityScore * 100:F1})*");
                    sb.AppendLine($"  {r.Rule.Description}");
                }
            }
        }

        string stance;
        if (solidViolations.Count > 0 || metrics.CyclomaticComplexity > 8)
        {
            stance = "REQUEST_CHANGES";
            sb.AppendLine("\n**⚠️ Mimari & Temiz Kod Bulguları:**");
            foreach (var v in solidViolations)
            {
                sb.AppendLine($"- **[{v.RuleId}] {v.RuleName}:** {v.Description} (Satır: {v.StartLine}-{v.EndLine})");
                sb.AppendLine($"  *👉 Öneri:* {v.SuggestedFix}");
                patches.Add($"// Refactoring Önerisi ({v.RuleId}):\n// {v.SuggestedFix}");
            }
        }
        else
        {
            stance = "APPROVE";
            sb.AppendLine("\n**✅ Mimari ve Temiz Kod Standartları Karşılandı:** Sınıf tasarımı ve metod sınırları kabul edilebilir seviyede.");
        }

        return new AgentOpinion(AgentName, RoleTitle, stance, sb.ToString(), patches);
    }
}
