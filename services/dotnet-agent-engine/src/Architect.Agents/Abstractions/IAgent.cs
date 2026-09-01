using Architect.Agents.Models;

namespace Architect.Agents.Abstractions;

public interface IAgent
{
    string AgentName { get; }
    string RoleTitle { get; }
    Task<AgentOpinion> EvaluateAsync(DebateContext context, CancellationToken cancellationToken = default);
}
