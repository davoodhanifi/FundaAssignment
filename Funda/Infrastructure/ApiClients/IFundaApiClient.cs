using Refit;
using System.Text.Json.Serialization;

namespace Funda.Infrastructure.ApiClients;

public interface IFundaApiClient
{
    [Get("/{apiKey}/?type=koop&zo={searchPath}&page={page}&pagesize=25")]
    Task<FundaApiResponse> GetListingsAsync(string apiKey, string searchPath, int? page = 1);
}

public class FundaApiResponse
{
    [JsonPropertyName("Objects")]
    public List<FundaObject> Objects { get; set; } = [];

    [JsonPropertyName("Paging")]
    public PagingInfo Paging { get; set; } = new();

    [JsonPropertyName("TotaalAantalObjecten")]
    public int TotalCount { get; set; } = 0;
}

public class PagingInfo
{
    [JsonPropertyName("AantalPaginas")]
    public int? AantalPaginas { get; set; }
}

public class FundaObject
{
    [JsonPropertyName("MakelaarNaam")]
    public string MakelaarNaam { get; set; } = string.Empty;
}