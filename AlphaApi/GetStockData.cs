using System.Net;
using BlazorApp.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AlphaApi
{
    public class GetStockData
    {
        private readonly ILogger _logger;

        public HttpTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpTrigger>();
        }

        [Function("WeatherForecast")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var symbol = System.Web.HttpUtility.ParseQueryString(req.Url.Query).Get("symbol") ?? "IBM";
            string apiKey = "HN0EUPQ6JEALB9V6";

            string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={symbol}&interval=5min&apikey={apiKey}";

            using var client = new HttpClient();
            var apiResponse = await client.GetStringAsync(url);

            var jsonElement = JsonSerializer.Deserialize<JsonElement>(apiResponse);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(jsonElement);

            return response;
        }
    }
}
