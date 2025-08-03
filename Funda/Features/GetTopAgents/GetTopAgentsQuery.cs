namespace Funda.Features.GetTopAgents;

public record GetTopAgentsQuery(string SearchPath, string Title, int TopCount = 10);