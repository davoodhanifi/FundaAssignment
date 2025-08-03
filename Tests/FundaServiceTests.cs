using Funda.Common.OptionModels;
using Funda.Features.GetTopAgents;
using Funda.Infrastructure.ApiClients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Retry;
using Refit;
using System.Net;

namespace Tests;

public class FundaServiceTests
{
    private readonly Mock<IFundaApiClient> _apiMock = new();
    private readonly ILogger<FundaService> _logger = Mock.Of<ILogger<FundaService>>();
    private readonly IOptions<FundaApiOptions> _options = Options.Create(new FundaApiOptions
    {
        ApiKey = "dummy",
        ApiEndpoint = "https://dummy/"
    });

    [Fact]
    public async Task GetTopAgents_ReturnsCorrectTopAgent()
    {
        // Arrange
        var page1 = new FundaApiResponse
        {
            Objects =
            [
                new() { MakelaarNaam = "Agent A" },
                new() { MakelaarNaam = "Agent A" },
                new() { MakelaarNaam = "Agent B" },
            ],
            Paging = new PagingInfo { AantalPaginas = 1 }
        };

        _apiMock.Setup(api => api.GetListingsAsync("dummy", "/amsterdam/", 1))
            .ReturnsAsync(page1);

        var service = new FundaService(_apiMock.Object, _options, _logger, NoRetryPolicy);

        // Act
        var result = await service.GetTopAgents(new GetTopAgentsQuery("/amsterdam/", "Test", 1));

        // Assert
        Assert.Equal("Agent A", result.First().Agent);
        Assert.Equal(2, result.First().Count);
    }

    [Fact]
    public async Task GetTopAgents_ReturnsEmpty_WhenNoResults()
    {
        // Arrange
        var empty = new FundaApiResponse
        {
            Objects = [],
            Paging = new PagingInfo { AantalPaginas = 1 }
        };

        _apiMock.Setup(api => api.GetListingsAsync("dummy", "/amsterdam/", 1))
            .ReturnsAsync(empty);

        var service = new FundaService(_apiMock.Object, _options, _logger, NoRetryPolicy);

        // Act
        var result = await service.GetTopAgents(new GetTopAgentsQuery("/amsterdam/", "Test", 5));

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTopAgents_HandlesMultiplePages()
    {
        // Arrange
        var page1 = new FundaApiResponse
        {
            Objects =
            [
                new() { MakelaarNaam = "Agent A" },
                new() { MakelaarNaam = "Agent B" },
            ],
            Paging = new PagingInfo { AantalPaginas = 2 }
        };
        var page2 = new FundaApiResponse
        {
            Objects =
            [
                new() { MakelaarNaam = "Agent A" },
                new() { MakelaarNaam = "Agent C" },
            ],
            Paging = new PagingInfo { AantalPaginas = 2 }
        };
        _apiMock.Setup(api => api.GetListingsAsync("dummy", "/amsterdam/", 1))
            .ReturnsAsync(page1);
        _apiMock.Setup(api => api.GetListingsAsync("dummy", "/amsterdam/", 2))
            .ReturnsAsync(page2);
        var service = new FundaService(_apiMock.Object, _options, _logger, NoRetryPolicy);

        // Act
        var result = await service.GetTopAgents(new GetTopAgentsQuery("/amsterdam/", "Test", 3));

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Agent A", result[0].Agent);
        Assert.Equal(2, result[0].Count);
        Assert.Equal("Agent B", result[1].Agent);
        Assert.Equal(1, result[1].Count);
        Assert.Equal("Agent C", result[2].Agent);
        Assert.Equal(1, result[2].Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]  // 401
    [InlineData(HttpStatusCode.InternalServerError)]  // 500
    [InlineData(HttpStatusCode.BadRequest)]  // 400
    public async Task GetTopAgents_HandlesHttpErrors(HttpStatusCode statusCode)
    {
        // Arrange
        var apiException = CreateFakeApiException(statusCode);
        _apiMock.Setup(api => api.GetListingsAsync("dummy", "/amsterdam/", 1))
                .ThrowsAsync(apiException);

        var service = new FundaService(_apiMock.Object, _options, _logger, NoRetryPolicy);

        // Act + Assert
        await Assert.ThrowsAsync<ApiException>(() =>
            service.GetTopAgents(new GetTopAgentsQuery("/amsterdam/", "Test", 5)));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]         // 401
    [InlineData(HttpStatusCode.TooManyRequests)]      // 429
    [InlineData(HttpStatusCode.InternalServerError)]  // 500
    public async Task GetTopAgents_RetriesThreeTimes_OnRetryableErrors(HttpStatusCode statusCode)
    {
        // Arrange
        var callCount = 0;

        var apiMock = new Mock<IFundaApiClient>();
        apiMock.Setup(api => api.GetListingsAsync("dummy", "/amsterdam/", 1))
            .Callback(() => callCount++)
            .ThrowsAsync(CreateFakeApiException(statusCode));

        var options = Options.Create(new FundaApiOptions
        {
            ApiKey = "dummy",
            ApiEndpoint = "https://fake/"
        });

        var logger = Mock.Of<ILogger<FundaService>>();

        var retryPolicy = Policy
            .Handle<ApiException>(ex =>
                (int)ex.StatusCode == 401 || (int)ex.StatusCode == 429 || (int)ex.StatusCode >= 500)
            .RetryAsync(3);

        var service = new FundaService(apiMock.Object, options, logger, retryPolicy);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() =>
            service.GetTopAgents(new GetTopAgentsQuery("/amsterdam/", "Test", 5)));

        Assert.Equal(4, callCount); // 1 initial call + 3 retries
    }

    private ApiException CreateFakeApiException(HttpStatusCode statusCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://fake");
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("{\"error\":\"mocked error\"}")
        };

        return ApiException.Create(request, HttpMethod.Get, response, null).Result;
    }

    private static AsyncRetryPolicy NoRetryPolicy => Policy
        .Handle<Exception>()
        .RetryAsync(0); // no retries
}
