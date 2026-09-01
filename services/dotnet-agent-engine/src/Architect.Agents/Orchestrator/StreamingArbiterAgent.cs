using System.Runtime.CompilerServices;
using Architect.Agents.Abstractions;
using Architect.Agents.Models;
using Architect.Agents.Specialists;

namespace Architect.Agents.Orchestrator;

public record AgentStreamEvent(
    string EventType, // "AGENT_STARTED", "AGENT_COMPLETED", "CONSENSUS_REACHED"
    string AgentName,
    string RoleTitle,
    string? Stance,
    string Message,
    int ProgressPercentage
);

public interface IStreamingArbiterAgent
{
    IAsyncEnumerable<AgentStreamEvent> StreamDebateAsync(DebateContext context, CancellationToken cancellationToken = default);
}

public class StreamingArbiterAgent : IStreamingArbiterAgent
{
    private readonly List<IAgent> _specialists;
    private readonly IArbiterAgent _arbiter;

    public StreamingArbiterAgent(IArbiterAgent arbiter, IEnumerable<IAgent>? specialists = null)
    {
        _arbiter = arbiter;
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

    public async IAsyncEnumerable<AgentStreamEvent> StreamDebateAsync(
        DebateContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new AgentStreamEvent(
            EventType: "DEBATE_STARTED",
            AgentName: "SystemOrchestrator",
            RoleTitle: "Debate Master",
            Stance: "INITIALIZING",
            Message: "Otonom Ajan Konseyi toplandı. Kod analizi ve kurumsal mimari hafıza taraması başlatılıyor...",
            ProgressPercentage: 10
        );

        var opinions = new List<AgentOpinion>();
        int progress = 20;

        foreach (var specialist in _specialists)
        {
            yield return new AgentStreamEvent(
                EventType: "AGENT_STARTED",
                AgentName: specialist.AgentName,
                RoleTitle: specialist.RoleTitle,
                Stance: "ANALYZING",
                Message: $"{specialist.RoleTitle} ({specialist.AgentName}) kodu ve AST verilerini inceliyor...",
                ProgressPercentage: progress
            );

            await Task.Delay(150, cancellationToken); // Simüle edilen akıcı ajan düşünme gecikmesi
            var opinion = await specialist.EvaluateAsync(context, cancellationToken);
            opinions.Add(opinion);

            progress += 25;
            yield return new AgentStreamEvent(
                EventType: "AGENT_COMPLETED",
                AgentName: specialist.AgentName,
                RoleTitle: specialist.RoleTitle,
                Stance: opinion.Stance,
                Message: opinion.ArgumentsMarkdown,
                ProgressPercentage: progress
            );
        }

        // Konsensüs Kararı
        var consensus = await _arbiter.ConductDebateAsync(context, cancellationToken);

        yield return new AgentStreamEvent(
            EventType: "CONSENSUS_REACHED",
            AgentName: "ArbiterAgent",
            RoleTitle: "Council Arbiter & Judge",
            Stance: consensus.RequiresHumanApproval ? "REJECTED_NEEDS_SECURITY_FIX" : "CONSENSUS_APPROVED",
            Message: consensus.FinalConsensusSummary,
            ProgressPercentage: 100
        );
    }
}
