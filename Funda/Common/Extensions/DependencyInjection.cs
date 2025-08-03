using Funda.Common.OptionModels;
using Funda.Features.GetTopAgents;
using Funda.Infrastructure.ApiClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Refit;

namespace Funda.Common.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<FundaService>();
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FundaApiOptions>(configuration.GetSection("FundaApi"));

        services.AddRefitClient<IFundaApiClient>()
            .ConfigureHttpClient((sp, c) =>
            {
                var options = sp.GetRequiredService<IOptions<FundaApiOptions>>();
                c.BaseAddress = new Uri(options.Value.ApiEndpoint);
            });

        services.AddSingleton<IAsyncPolicy>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<FundaService>>();
            return CreateDefaultRetryPolicy(logger);
        });

        return services;
    }

    private static AsyncRetryPolicy CreateDefaultRetryPolicy(ILogger logger)
    {
        return Policy
            .Handle<ApiException>(ex =>
                (int)ex.StatusCode == 401 || (int)ex.StatusCode == 429 || (int)ex.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(10 * attempt),  // 10s, 20s, 30s
                onRetry: (ex, ts, attempt, _) =>
                {
                    logger.LogWarning("Retry {Attempt} after {Delay}s due to: {Message}",
                        attempt, ts.TotalSeconds, ex.Message);
                });
    }
}
