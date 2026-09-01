using System.Text;
using Architect.Agents.Abstractions;
using Architect.Agents.Models;
using Architect.Agents.Specialists;

namespace Architect.Agents.Orchestrator;

public interface IArbiterAgent
{
    Task<DebateConsensus> ConductDebateAsync(DebateContext context, CancellationToken cancellationToken = default);
}

public class ArbiterAgent : IArbiterAgent
{
    private readonly List<IAgent> _specialists;

    public ArbiterAgent(IEnumerable<IAgent>? specialists = null)
    {
        var list = specialists?.ToList();
        if (list == null || list.Count == 0)
        {
            _specialists = new List<IAgent>
            {
                new ReviewerAgent(),
                new SecurityAgent(),
                new QaTestWriterAgent()
            };
        }
        else
        {
            _specialists = list;
        }
    }

    public async Task<DebateConsensus> ConductDebateAsync(DebateContext context, CancellationToken cancellationToken = default)
    {
        // FAZ D: Task.WhenAll ile tüm ajanlar paralel çalışır (~3x hızlanma)
        var agentTasks = _specialists
            .Select(specialist => specialist.EvaluateAsync(context, cancellationToken))
            .ToList();

        var opinionArray = await Task.WhenAll(agentTasks);
        var opinions = opinionArray.ToList();

        // 2. Fikir Çatışması ve Uzlaşı Analizi (Debate Resolution)
        var securityOpinion = opinions.FirstOrDefault(o => o.AgentName == "SecurityAgent");
        var reviewerOpinion = opinions.FirstOrDefault(o => o.AgentName == "ReviewerAgent");
        var qaOpinion = opinions.FirstOrDefault(o => o.AgentName == "QaTestWriterAgent");

        var hasSecurityRisk = securityOpinion?.Stance == "NEEDS_SECURITY_FIX";
        var hasReviewerBlocker = reviewerOpinion?.Stance == "REQUEST_CHANGES";

        var sb = new StringBuilder();
        sb.AppendLine("## 🏛️ Autonomous Code Architect - Ajan Konseyi Ortak Kararı\n");

        bool requiresHumanApproval;
        if (hasSecurityRisk)
        {
            requiresHumanApproval = true;
            sb.AppendLine("🔴 **DURUM: DEĞİŞİKLİK REDDEDİLDİ (GÜVENLİK ENGELİ)**");
            sb.AppendLine("> *Ajanlar Arası Çatışma Çözümü:* SecurityAgent tarafından kritik güvenlik riski tespit edildiği için Reviewer onayı olsa dahi PR birleştirilemez.");
        }
        else if (hasReviewerBlocker)
        {
            requiresHumanApproval = false;
            sb.AppendLine("🟡 **DURUM: İYİLEŞTİRME TALEP EDİLDİ (MİMARİ DÜZELTME)**");
            sb.AppendLine("> *Ajanlar Arası Çatışma Çözümü:* Kod güvenlik testlerinden geçti ancak mimari/performans standartlarını sağlaması için önerilen refactoring adımları uygulanmalıdır.");
        }
        else
        {
            requiresHumanApproval = false;
            sb.AppendLine("🟢 **DURUM: ONAYLANDI (CLEAN CODE & SECURE)**");
            sb.AppendLine("> *Ajanlar Arası Çatışma Çözümü:* Tüm uzman ajanlar tam mutabakat ile kodun birleştirilmesini onayladı.");
        }

        sb.AppendLine("\n---\n### 🗣️ Ajan Bireysel Savunmaları");
        foreach (var op in opinions)
        {
            sb.AppendLine($"\n{op.ArgumentsMarkdown}");
        }

        var generatedTests = qaOpinion?.ProposedCodePatches.FirstOrDefault() ?? string.Empty;
        var suggestedPatch = string.Join("\n\n", opinions.SelectMany(o => o.ProposedCodePatches).Where(p => !p.Contains("class ")));

        return new DebateConsensus(
            RequestId: context.RequestId,
            Opinions: opinions,
            FinalConsensusSummary: sb.ToString(),
            GeneratedUnitTestCode: generatedTests,
            SuggestedRefactoredCode: suggestedPatch,
            RequiresHumanApproval: requiresHumanApproval
        );
    }
}
