using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwardsFerm.Core.Interfaces;
using AwardsFerm.Core.Models;

namespace AwardsFerm.Worker.Services;

public sealed class HttpSessionEventReporter : ISessionEventReporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public HttpSessionEventReporter(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task ReportAsync(SessionEvent sessionEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiUrl = _configuration["Api:BaseUrl"] ?? "http://localhost:8080";
            var client = _httpClientFactory.CreateClient("api");
            using var content = JsonContent.Create(sessionEvent, options: JsonOptions);
            await client.PostAsync($"{apiUrl}/api/internal/events", content, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to report session event: {ex.Message}");
        }
    }
}
