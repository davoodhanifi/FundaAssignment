using Funda.Common.Extensions;
using Funda.Features.GetTopAgents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

var app = builder.Build();
using var scope = app.Services.CreateScope();

var fundaService = scope.ServiceProvider.GetRequiredService<FundaService>();

var resultTopAgentAmsterdamSale = await fundaService.GetTopAgents(new GetTopAgentsQuery(
   SearchPath: "/amsterdam/",
   Title: "Top 10 agents listings in Amsterdam",
   TopCount: 10));

Console.WriteLine("Top 10 agents listings in Amsterdam: ");
foreach (var agent in resultTopAgentAmsterdamSale)
{
    Console.WriteLine($"{agent.Agent} - {agent.Count} listings");
}

await Task.Delay(60000);
var resultTopAgentAmsterdamTuinSale = await fundaService.GetTopAgents(new GetTopAgentsQuery(
    SearchPath: "/amsterdam/tuin/",
    Title: "Top 10 agents with garden listings in Amsterdam",
    TopCount: 10));

Console.WriteLine("Top 10 agents with garden listings in Amsterdam: ");
foreach (var agent in resultTopAgentAmsterdamTuinSale)
{
    Console.WriteLine($"{agent.Agent} - {agent.Count} listings");
}

