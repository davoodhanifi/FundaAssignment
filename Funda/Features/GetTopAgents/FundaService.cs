using Funda.Common.OptionModels;
using Funda.Infrastructure.ApiClients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Funda.Features.GetTopAgents;

public class FundaService(IFundaApiClient fundaApi,
    IOptions<FundaApiOptions> fundaApiOptions,
    ILogger<FundaService> logger,
    IAsyncPolicy retryPolicy)
{
    private readonly IFundaApiClient _fundaApi = fundaApi ?? throw new ArgumentNullException(nameof(fundaApi));
    private readonly FundaApiOptions _fundaApiOptions = fundaApiOptions.Value ?? throw new ArgumentNullException(nameof(fundaApiOptions));
    private readonly ILogger<FundaService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IAsyncPolicy _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));

    public async Task<List<TopAgentsResult>> GetTopAgents(GetTopAgentsQuery query)
    {
        try
        {
            _logger.LogInformation($"Starting agent query: {query.Title}, Path: {query.SearchPath}");

            var allListings = await GetAllListingsAsync(query.SearchPath);

            var topAgents = allListings
                .GroupBy(x => x.MakelaarNaam)
                .Select(g => new TopAgentsResult(Agent: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .Take(query.TopCount)
                .ToList();

            _logger.LogInformation($"Top {topAgents.Count} agents for query: {query.SearchPath} Done!");

            return topAgents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to get top {query.TopCount} agents for query: {query.SearchPath} with title: {query.Title}");
            throw;
        }
    }

    private async Task<List<FundaObject>> GetAllListingsAsync(string searchPath)
    {
        var allListings = new List<FundaObject>();
        int page = 1;

        while (true)
        {
            try
            {
                var response = await _retryPolicy.ExecuteAsync(() =>
                    _fundaApi.GetListingsAsync(_fundaApiOptions.ApiKey, searchPath, page));

                _logger.LogInformation($"Fetched page {page}: {response?.Objects?.Count ?? 0} listings");

                if (response?.Objects is { Count: > 0 })
                {
                    allListings.AddRange(response.Objects);
                }

                if (response?.Paging?.AantalPaginas is null ||
                    page >= response.Paging.AantalPaginas)
                {
                    break;
                }

                page++;

                // Delay every 10 requests to avoid triggering the API rate limit
                if (page % 10 == 0)
                {
                    _logger.LogInformation($"Throttling: delaying after 5 seconds to fetch page {page}");
                    await Task.Delay(5000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled exception on page {page}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation($"Fetched all pages: {page}, Total listings: {allListings.Count}");

        return allListings;
    }
}

public record TopAgentsResult(string Agent, int Count);
