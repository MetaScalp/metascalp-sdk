using System.Net.Http.Json;
using Newtonsoft.Json;

namespace MetaScalp.Sdk;

/// <summary>
/// HTTP REST client for MetaScalp API.
/// </summary>
public class MetaScalpClient : IDisposable
{
    private const int PortStart = 17845;
    private const int PortEnd = 17855;

    private readonly HttpClient _http;

    public int Port { get; }

    public MetaScalpClient(int port)
    {
        Port = port;
        _http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
    }

    /// <summary>
    /// Scans ports 17845-17855 to find a running MetaScalp instance.
    /// </summary>
    public static async Task<MetaScalpClient> DiscoverAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
        for (var port = PortStart; port <= PortEnd; port++)
        {
            try
            {
                var res = await http.GetStringAsync($"http://127.0.0.1:{port}/ping", ct);
                var ping = JsonConvert.DeserializeObject<PingResponse>(res);
                if (ping?.App == "MetaScalp")
                    return new MetaScalpClient(port);
            }
            catch
            {
                // try next port
            }
        }

        throw new InvalidOperationException($"MetaScalp not found on ports {PortStart}-{PortEnd}");
    }

    // ---- Discovery ----

    public async Task<PingResponse> PingAsync(CancellationToken ct = default)
        => await GetAsync<PingResponse>("/ping", ct);

    // ---- Connections ----

    public async Task<ConnectionsResponse> GetConnectionsAsync(CancellationToken ct = default)
        => await GetAsync<ConnectionsResponse>("/api/connections", ct);

    public async Task<ConnectionDto> GetConnectionAsync(long connectionId, CancellationToken ct = default)
        => await GetAsync<ConnectionDto>($"/api/connections/{connectionId}", ct);

    // ---- Market Data Queries ----

    public async Task<TickersResponse> GetTickersAsync(long connectionId, CancellationToken ct = default)
        => await GetAsync<TickersResponse>($"/api/connections/{connectionId}/tickers", ct);

    // ---- Trading Data ----

    public async Task<OrdersResponse> GetOrdersAsync(long connectionId, string ticker, CancellationToken ct = default)
        => await GetAsync<OrdersResponse>($"/api/connections/{connectionId}/orders?Ticker={Uri.EscapeDataString(ticker)}", ct);

    public async Task<PositionsResponse> GetPositionsAsync(long connectionId, CancellationToken ct = default)
        => await GetAsync<PositionsResponse>($"/api/connections/{connectionId}/positions", ct);

    public async Task<BalanceResponse> GetBalanceAsync(long connectionId, CancellationToken ct = default)
        => await GetAsync<BalanceResponse>($"/api/connections/{connectionId}/balance", ct);

    // ---- Order Execution ----

    public async Task<PlaceOrderResponse> PlaceOrderAsync(long connectionId, PlaceOrderRequest request, CancellationToken ct = default)
        => await PostAsync<PlaceOrderResponse>($"/api/connections/{connectionId}/orders", request, ct);

    public async Task CancelOrderAsync(long connectionId, CancelOrderRequest request, CancellationToken ct = default)
        => await PostAsync<object>($"/api/connections/{connectionId}/orders/cancel", request, ct);

    // ---- HTTP Helpers ----

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        var res = await _http.GetAsync(path, ct);
        var json = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new MetaScalpApiException((int)res.StatusCode, json, path);
        return JsonConvert.DeserializeObject<T>(json)!;
    }

    private async Task<T> PostAsync<T>(string path, object body, CancellationToken ct)
    {
        var content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
        var res = await _http.PostAsync(path, content, ct);
        var json = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new MetaScalpApiException((int)res.StatusCode, json, path);
        return JsonConvert.DeserializeObject<T>(json)!;
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}

public class MetaScalpApiException : Exception
{
    public int StatusCode { get; }
    public string Path { get; }

    public MetaScalpApiException(int statusCode, string error, string path)
        : base($"MetaScalp API error {statusCode} on {path}: {error}")
    {
        StatusCode = statusCode;
        Path = path;
    }
}
