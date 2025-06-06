using System.Net;
using BlazorApp.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

public class StockFunction
{
    private readonly ILogger _logger;
    private const string ApiKey = "HN0EUPQ6JEALB9V6";

    public StockFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<StockFunction>();
    }

    [Function("GetStock")]
    // responds to http get
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var response = req.CreateResponse();

        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var symbol = query.Get("symbol");

            // if there is no symbol return bad request
            if (string.IsNullOrWhiteSpace(symbol))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Stock symbol is required");
                return response;
            }

            // call stock api
            using var httpClient = new HttpClient();
            var apiUrl = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={ApiKey}";

            var apiResponse = await httpClient.GetAsync(apiUrl);
            var content = await apiResponse.Content.ReadAsStringAsync();

            // failed to call API
            if (!apiResponse.IsSuccessStatusCode)
            {
                response.StatusCode = HttpStatusCode.BadGateway;
                await response.WriteStringAsync($"Stock API error: {content}");
                return response;
            }

            // convert api data json to custom type 
            var stockData = JsonSerializer.Deserialize<AlphaVantageResponse>(content);

            // when empty price
            if (stockData?.GlobalQuote == null || string.IsNullOrEmpty(stockData.GlobalQuote.Price))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync("No stock data found for symbol");
                return response;
            }

            // when price data conversion to decimal fails
            if (!decimal.TryParse(stockData.GlobalQuote.Price, out var price))
            {
                response.StatusCode = HttpStatusCode.BadGateway;
                await response.WriteStringAsync("Invalid price data received");
                return response;
            }

            await response.WriteAsJsonAsync(new StockData
            {
                Symbol = symbol,
                Price = (double)price,
                LastUpdated = DateTime.UtcNow
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock data");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}


public class AlphaVantageResponse
{
    [JsonPropertyName("Global Quote")]x
    public required GlobalQuote GlobalQuote { get; set; }
}

public class GlobalQuote
{
    [JsonPropertyName("05. price")]
    public required string Price { get; set; }
}