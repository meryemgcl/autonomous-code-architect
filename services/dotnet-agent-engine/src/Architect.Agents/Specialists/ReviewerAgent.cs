using System.Text;
using Architect.Agents.Abstractions;
using Architect.Agents.Models;
using Architect.Core.Models;

namespace Architect.Agents.Specialists;

public class ReviewerAgent : IAgent
{
    public string AgentName => "ReviewerAgent";
    public string RoleTitle => "Senior Software & Architecture Reviewer";

    public Task<AgentOpinion> EvaluateAsync(DebateContext context, CancellationToken cancellationToken = default)
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

        var opinion = new AgentOpinion(AgentName, RoleTitle, stance, sb.ToString(), patches);
        return Task.FromResult(opinion);
    }
}
