using System.Text;
using Architect.Agents.Abstractions;
using Architect.Agents.Models;
using Architect.Core.Models;

namespace Architect.Agents.Specialists;

public class SecurityAgent : IAgent
{
    public string AgentName => "SecurityAgent";
    public string RoleTitle => "Application Security & OWASP Auditor";

    public Task<AgentOpinion> EvaluateAsync(DebateContext context, CancellationToken cancellationToken = default)
    {
        var violations = context.DeterministicFindings.Violations;
        var securityViolations = violations.Where(v => v.Category == RuleCategory.Security || 
                                                       v.Severity == AnalysisSeverity.Critical).ToList();

        var sb = new StringBuilder();
        var patches = new List<string>();

        sb.AppendLine($"### 🛡️ {RoleTitle} Güvenlik Raporu");

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

        var opinion = new AgentOpinion(AgentName, RoleTitle, stance, sb.ToString(), patches);
        return Task.FromResult(opinion);
    }
}
