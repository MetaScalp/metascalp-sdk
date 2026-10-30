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

    public async Task<TickersResponse> GetTickersAsync(long connectionId, bool refresh = false, CancellationToken ct = default)
        => await GetAsync<TickersResponse>($"/api/connections/{connectionId}/tickers{(refresh ? "?Refresh=true" : "")}", ct);

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

    // ---- Combo ----

    /// <summary>
    /// Opens a combo layout: <see cref="ComboRequest.Ticker"/> (one combo) or <see cref="ComboRequest.Tickers"/>
    /// (one per ticker, in order, validated all-or-nothing). <see cref="ComboRequest.Connections"/> opens exactly
    /// these books, in this order. Unset fields are not sent, so older terminals still accept the call.
    /// </summary>
    public async Task<StatusResponse> OpenComboAsync(ComboRequest request, CancellationToken ct = default)
        => await PostAsync<StatusResponse>("/api/combo", request, ct);

    // ---- Order Book Settings ----

    public Task<OrderBookSettingsResponse> GetOrderBookSettingsAsync(long connectionId, string ticker, CancellationToken ct = default)
        => GetAsync<OrderBookSettingsResponse>($"/api/connections/{connectionId}/orderbook-settings?Ticker={Uri.EscapeDataString(ticker)}", ct);

    public Task<OrderBookSettingsResponse> UpdateOrderBookSettingsAsync(long connectionId, string ticker, OrderBookSettingsDto settings, CancellationToken ct = default)
        => PutAsync<OrderBookSettingsResponse>($"/api/connections/{connectionId}/orderbook-settings?Ticker={Uri.EscapeDataString(ticker)}", settings, ct);

    // ---- Order Book Snapshot (one-shot fresh REST fetch) ----

    /// <summary>
    /// Fetches a fresh order book snapshot directly from the exchange REST endpoint.
    /// Each call performs one exchange REST request — no cache, no subscription side effects.
    /// Intended as a one-shot complement to <c>SubscribeOrderBook(..., fetchSnapshot: false)</c>:
    /// subscribe to deltas cheaply, then call this once per ticker when you actually need to seed state.
    /// Throws <see cref="MetaScalpApiException"/> with status <c>501</c> for exchanges that don't
    /// expose a REST snapshot endpoint (e.g. Bybit USDT Perpetual).
    /// </summary>
    /// <param name="zoomIndex">Price aggregation factor. <c>0</c>/<c>1</c> = no aggregation; <c>&gt; 1</c> = bucket and sum sizes.</param>
    /// <param name="depthLevels">Top-N price levels per side after zoom + percent. Must be ≥ 1 when specified.</param>
    /// <param name="depthPercent">Per-side band as a percentage anchored on best ask / best bid. Must be &gt; 0 when specified.</param>
    public Task<OrderBookSnapshotResponse> GetOrderBookSnapshotAsync(long connectionId, string ticker,
        int zoomIndex = 0, int? depthLevels = null, decimal? depthPercent = null, CancellationToken ct = default)
    {
        var qs = new List<string> { $"Ticker={Uri.EscapeDataString(ticker)}" };
        if (zoomIndex > 0) qs.Add($"ZoomIndex={zoomIndex}");
        if (depthLevels is not null) qs.Add($"DepthLevels={depthLevels}");
        if (depthPercent is not null) qs.Add($"DepthPercent={depthPercent.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        return GetAsync<OrderBookSnapshotResponse>($"/api/connections/{connectionId}/orderbook-snapshot?{string.Join("&", qs)}", ct);
    }

    // ---- Clusters ----

    /// <summary>
    /// Fetches the cluster (volume profile / footprint) snapshot for a ticker at a timeframe.
    /// The response always holds 100 time columns, oldest first. By default only the newest 10 carry data
    /// (the cluster backend's default page); pass <paramref name="columns"/> (up to 100) to fill more history.
    /// Each extra 5 columns is one more backend request, so deep fills take longer.
    /// </summary>
    /// <param name="timeFrame">One of <c>S30</c>, <c>M1</c>, <c>M5</c>, <c>M10</c>, <c>M15</c>, <c>M30</c>, <c>H1</c>, <c>H4</c>, <c>D1</c>.</param>
    /// <param name="zoomIndex">Price aggregation factor. <c>1</c> = raw price levels; <c>&gt; 1</c> = bucket into <c>zoomIndex × PriceIncrement</c> and sum sizes.</param>
    /// <param name="columns">History depth counted back from the newest column (1 = newest). <c>null</c>/<c>0</c> = the backend default page (10). Values above 100 are clamped by the server.</param>
    public Task<ClusterSnapshotResponse> GetClusterSnapshotAsync(long connectionId, string ticker, string timeFrame,
        int zoomIndex = 1, int? columns = null, CancellationToken ct = default)
    {
        var qs = new List<string> { $"Ticker={Uri.EscapeDataString(ticker)}", $"TimeFrame={timeFrame}" };
        if (zoomIndex > 1) qs.Add($"ZoomIndex={zoomIndex}");
        if (columns is > 0) qs.Add($"Columns={columns}");
        return GetAsync<ClusterSnapshotResponse>($"/api/connections/{connectionId}/cluster-snapshot?{string.Join("&", qs)}", ct);
    }

    // ---- Signal Levels ----

    public Task<SignalLevelsResponse> GetSignalLevelsAsync(long connectionId, string ticker, CancellationToken ct = default)
        => GetAsync<SignalLevelsResponse>($"/api/connections/{connectionId}/signal-levels?Ticker={Uri.EscapeDataString(ticker)}", ct);

    public Task PlaceSignalLevelAsync(long connectionId, PlaceSignalLevelRequest request, CancellationToken ct = default)
        => PostAsync<object>($"/api/connections/{connectionId}/signal-levels", request, ct);

    public Task RemoveSignalLevelAsync(long connectionId, long signalLevelId, CancellationToken ct = default)
        => DeleteAsync<object>($"/api/connections/{connectionId}/signal-levels/{signalLevelId}", ct);

    public Task RemoveAllSignalLevelsAsync(long connectionId, string ticker, CancellationToken ct = default)
        => DeleteAsync<object>($"/api/connections/{connectionId}/signal-levels?Ticker={Uri.EscapeDataString(ticker)}", ct);

    public Task RemoveTriggeredSignalLevelsAsync(CancellationToken ct = default)
        => DeleteAsync<object>("/api/signal-levels/triggered", ct);

    // ---- Appearance (Settings -> Display) ----

    /// <summary>Active theme id, every theme and the general display options.</summary>
    public Task<AppearanceState> GetAppearanceAsync(CancellationToken ct = default)
        => GetAsync<AppearanceState>("/api/appearance", ct);

    /// <summary>Font families the <c>Font</c> option accepts (custom fonts first, then installed system fonts).</summary>
    public Task<FontFamiliesResponse> GetFontFamiliesAsync(CancellationToken ct = default)
        => GetAsync<FontFamiliesResponse>("/api/appearance/font-families", ct);

    /// <summary>Changes only the non-null options; applied at once. 400 on a bad value or range (nothing saved).</summary>
    public Task<AppearanceGeneral> UpdateAppearanceGeneralAsync(AppearanceGeneralUpdate update, CancellationToken ct = default)
        => PutAsync<AppearanceGeneral>("/api/appearance/general", update, ct);

    /// <summary>Makes the theme active in the running terminal (kept after a restart). 404 for an unknown id.</summary>
    public Task<AppearanceTheme> SetActiveThemeAsync(long themeId, CancellationToken ct = default)
        => PutAsync<AppearanceTheme>("/api/appearance/active-theme", new { themeId }, ct);

    /// <summary>
    /// Creates an editable copy of <see cref="CreateThemeRequest.CopyFromThemeId"/> (default: the active theme);
    /// no name = «&lt;source&gt; (N)». The copy is not activated.
    /// </summary>
    public Task<AppearanceTheme> CreateThemeAsync(CreateThemeRequest? request = null, CancellationToken ct = default)
        => PostAsync<AppearanceTheme>("/api/appearance/themes", request ?? new CreateThemeRequest(), ct);

    /// <summary>Renames an own theme. 409 for a built-in theme (copy it first).</summary>
    public Task<AppearanceTheme> RenameThemeAsync(long themeId, string name, CancellationToken ct = default)
        => PutAsync<AppearanceTheme>($"/api/appearance/themes/{themeId}", new { name }, ct);

    /// <summary>Deletes an own theme; deleting the active one activates its neighbour. 409 built-in, 400 last theme.</summary>
    public Task<DeleteThemeResponse> DeleteThemeAsync(long themeId, CancellationToken ct = default)
        => DeleteAsync<DeleteThemeResponse>($"/api/appearance/themes/{themeId}", ct);

    /// <summary>Every colour row of the four Display colour sections.</summary>
    public Task<ThemeColorsResponse> GetThemeColorsAsync(long themeId, CancellationToken ct = default)
        => GetAsync<ThemeColorsResponse>($"/api/appearance/themes/{themeId}/colors", ct);

    /// <summary>
    /// Writes colour rows: key -> <c>#RRGGBB</c> / <c>#AARRGGBB</c> (<c>Light</c> / <c>Dark</c> for the two choice rows).
    /// All-or-nothing: one bad key or value = 400 and nothing is saved. 409 for a built-in theme.
    /// </summary>
    public Task<ThemeColorsResponse> UpdateThemeColorsAsync(long themeId, IDictionary<string, string> colors, CancellationToken ct = default)
        => PutAsync<ThemeColorsResponse>($"/api/appearance/themes/{themeId}/colors", new { colors }, ct);

    /// <summary>Every «Fonts» row: element, group, label, size, bold, italic, color, hasStyle.</summary>
    public Task<ThemeTextStylesResponse> GetThemeTextStylesAsync(long themeId, CancellationToken ct = default)
        => GetAsync<ThemeTextStylesResponse>($"/api/appearance/themes/{themeId}/text-styles", ct);

    /// <summary>Writes «Fonts» rows: element -> size (8-20) / bold / italic / color; the two chip-background rows take color only.</summary>
    public Task<ThemeTextStylesResponse> UpdateThemeTextStylesAsync(long themeId, IDictionary<string, AppearanceTextStyleUpdate> textStyles,
        CancellationToken ct = default)
        => PutAsync<ThemeTextStylesResponse>($"/api/appearance/themes/{themeId}/text-styles", new { textStyles }, ct);

    // ---- HTTP Helpers ----

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        var res = await _http.GetAsync(path, ct);
        var json = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new MetaScalpApiException((int)res.StatusCode, json, path);
        return JsonConvert.DeserializeObject<T>(json)!;
    }

    private async Task<T> DeleteAsync<T>(string path, CancellationToken ct = default)
    {
        var res = await _http.DeleteAsync(path, ct);
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

    private async Task<T> PutAsync<T>(string path, object body, CancellationToken ct = default)
    {
        var content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
        var res = await _http.PutAsync(path, content, ct);
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
