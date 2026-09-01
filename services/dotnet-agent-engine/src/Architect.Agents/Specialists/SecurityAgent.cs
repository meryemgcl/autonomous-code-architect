using System.Text;
using Architect.Agents.Abstractions;
using Architect.Agents.Models;
using Architect.Core.Models;
using Architect.Infrastructure.Memory;

namespace Architect.Agents.Specialists;

public class SecurityAgent : IAgent
{
    private readonly IVectorMemoryService? _vectorMemory;

    public string AgentName => "SecurityAgent";
    public string RoleTitle => "Application Security & OWASP Auditor";

    public SecurityAgent(IVectorMemoryService? vectorMemory = null)
    {
        _vectorMemory = vectorMemory;
    }

    public async Task<AgentOpinion> EvaluateAsync(DebateContext context, CancellationToken cancellationToken = default)
    {
        var violations = context.DeterministicFindings.Violations;
        var securityViolations = violations.Where(v => v.Category == RuleCategory.Security || 
                                                       v.Severity == AnalysisSeverity.Critical).ToList();

        var sb = new StringBuilder();
        var patches = new List<string>();

        sb.AppendLine($"### 🛡️ {RoleTitle} Güvenlik Raporu");

        // RAG Vektör Hafızasından Güvenlik Politikalarını Çek
        if (_vectorMemory != null)
        {
            var query = "Security Secret Password Token OWASP Vault";
            var relevantRules = await _vectorMemory.FindRelevantRulesAsync(query, topK: 1, cancellationToken);
            
            if (relevantRules.Count > 0)
            {
                var r = relevantRules[0];
                sb.AppendLine($"\n**🔒 Kurumsal Güvenlik Politikası:** [{r.Rule.RuleCode}] {r.Rule.RuleName}");
                sb.AppendLine($"> *{r.Rule.Description}*");
            }
        }

        string stance;
        if (securityViolations.Count > 0)
        {
            stance = "NEEDS_SECURITY_FIX";
            sb.AppendLine("\n**🚨 KRİTİK GÜVENLİK TEHLİKELERİ TESPİT EDİLDİ:**");
            foreach (var v in securityViolations)
            {
                sb.AppendLine($"- **[{v.RuleId}] {v.RuleName} ({v.Severity}):** {v.Description} (Satır: {v.StartLine}-{v.EndLine})");
                sb.AppendLine($"  *🔒 Güvenli Düzeltme:* {v.SuggestedFix}");
                patches.Add($"// Güvenlik Düzeltmesi ({v.RuleId}):\n// {v.SuggestedFix}");
            }
        }
        else
        {
            stance = "APPROVE";
            sb.AppendLine("\n**✅ Güvenlik Açığı Tespit Edilmedi:** Hardcoded secret veya bilinen OWASP açığına rastlanmadı.");
        }

        return new AgentOpinion(AgentName, RoleTitle, stance, sb.ToString(), patches);
    }
}
