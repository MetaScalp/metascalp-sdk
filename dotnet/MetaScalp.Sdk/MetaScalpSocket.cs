using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MetaScalp.Sdk;

/// <summary>
/// WebSocket client for MetaScalp real-time updates.
/// Supports connection-level subscriptions (orders, positions, balances)
/// and market data subscriptions (trades, order book) scoped by connectionId + ticker.
/// </summary>
public class MetaScalpSocket : IDisposable
{
    private const int PortStart = 17845;
    private const int PortEnd = 17855;

    private ClientWebSocket? _ws;
    private CancellationTokenSource _cts = new();
    private Task? _receiveLoop;

    public int Port { get; }
    public bool Connected { get; private set; }

    // ---- Events ----

    // Connection-level events — fired after calling Subscribe(connectionId).
    // These cover all tickers on the subscribed connection.

    /// <summary>Fired when an order is created, modified, filled, or cancelled. Requires Subscribe().</summary>
    public event Action<OrderUpdateData>? OnOrderUpdate;
    /// <summary>Fired when a position is opened, changed, or closed. Requires Subscribe().</summary>
    public event Action<PositionUpdateData>? OnPositionUpdate;
    /// <summary>Fired when account balances change. Requires Subscribe().</summary>
    public event Action<BalanceUpdateData>? OnBalanceUpdate;
    /// <summary>Fired when financial results are recalculated. Requires Subscribe().</summary>
    public event Action<FinresUpdateData>? OnFinresUpdate;

    // Market data events — fired after calling SubscribeTrades() or SubscribeOrderBook().
    // These are scoped to a specific (connectionId, ticker) pair.

    /// <summary>Fired when trades occur for a subscribed ticker. Requires SubscribeTrades().</summary>
    public event Action<TradeUpdateData>? OnTradeUpdate;
    /// <summary>Fired once with the full order book state after SubscribeOrderBook().</summary>
    public event Action<OrderBookSnapshotData>? OnOrderBookSnapshot;
    /// <summary>Fired with incremental order book changes after the initial snapshot. Requires SubscribeOrderBook().</summary>
    public event Action<OrderBookUpdateData>? OnOrderBookUpdate;
    /// <summary>Fired when the mark price changes for a subscribed ticker (futures only). Requires SubscribeMarkPrice().</summary>
    public event Action<MarkPriceUpdateData>? OnMarkPriceUpdate;
    /// <summary>Fired when the funding rate or funding time changes for a subscribed ticker (perpetual futures only). Requires SubscribeFunding().</summary>
    public event Action<FundingUpdateData>? OnFundingUpdate;

    // Notification events — fired after calling SubscribeNotifications().
    // These are app-wide (not scoped to a connection).

    /// <summary>Fired once with recent notifications after SubscribeNotifications().</summary>
    public event Action<NotificationSnapshotData>? OnNotificationSnapshot;
    /// <summary>Fired when new notifications arrive (~1 second batches). Requires SubscribeNotifications().</summary>
    public event Action<NotificationUpdateData>? OnNotificationUpdate;

    // Signal level events — fired after calling SubscribeSignalLevels().
    // App-wide (not scoped to a connection).

    public event Action<SignalLevelsSnapshotData>? OnSignalLevelsSnapshot;
    public event Action<SignalLevelPlacedData>? OnSignalLevelPlaced;
    public event Action<SignalLevelTriggeredData>? OnSignalLevelTriggered;
    public event Action<SignalLevelRemovedData>? OnSignalLevelRemoved;
    public event Action? OnSignalLevelsRemovedAll;
    public event Action? OnSignalLevelsRemovedTriggered;

    // MetaBroker analytics events — fired after calling SubscribeDensityMap(),
    // SubscribeLargeTrades() or SubscribeLiquidations(). App-wide (not scoped to a connection).

    /// <summary>Fired once after SubscribeDensityMap() with the first-seen walls of the initial upstream
    /// snapshot (may be empty — it resolves the loading state).</summary>
    public event Action<DensityMapSnapshotData>? OnDensityMapSnapshot;
    /// <summary>Fired with newly seen density walls (each wall reported exactly once, on first sight of
    /// its id). Requires SubscribeDensityMap().</summary>
    public event Action<DensityMapUpdateData>? OnDensityMapUpdate;
    /// <summary>Fired with finished large-trade aggregates — final, append-only rows (the feed has no
    /// snapshot). Requires SubscribeLargeTrades().</summary>
    public event Action<LargeTradesUpdateData>? OnLargeTradesUpdate;
    /// <summary>Fired after every SubscribeLiquidations() (including a config replace) with the backfill
    /// rows + aggregates; may be empty. Requires SubscribeLiquidations().</summary>
    public event Action<LiquidationsSnapshotData>? OnLiquidationsSnapshot;
    /// <summary>Fired with live liquidation rows, batched newest first. Requires SubscribeLiquidations().</summary>
    public event Action<LiquidationsUpdateData>? OnLiquidationsUpdate;
    /// <summary>Fired ~every 2 s with the totals + top tokens for the configured window. Requires
    /// SubscribeLiquidations().</summary>
    public event Action<LiquidationsMetadataData>? OnLiquidationsMetadata;

    // Connection lifecycle events
    public event Action<string>? OnError;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public MetaScalpSocket(int port)
    {
        Port = port;
    }

    /// <summary>
    /// Scans ports 17845-17855 to find the MetaScalp WebSocket server.
    /// </summary>
    public static async Task<MetaScalpSocket> DiscoverAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        for (var port = PortStart; port <= PortEnd; port++)
        {
            try
            {
                var socket = new MetaScalpSocket(port);
                await socket.ConnectAsync(timeoutMs, ct);
                return socket;
            }
            catch
            {
                // try next port
            }
        }

        throw new InvalidOperationException($"MetaScalp WebSocket not found on ports {PortStart}-{PortEnd}");
    }

    /// <summary>
    /// Connect to the WebSocket server and start the receive loop.
    /// </summary>
    public async Task ConnectAsync(int timeoutMs = 5000, CancellationToken ct = default)
    {
        _ws = new ClientWebSocket();
        using var timeoutCts = new CancellationTokenSource(timeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        await _ws.ConnectAsync(new Uri($"ws://127.0.0.1:{Port}/"), linkedCts.Token);
        Connected = true;
        _cts = new CancellationTokenSource();
        _receiveLoop = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        OnConnected?.Invoke();
    }

    /// <summary>
    /// Disconnect from the WebSocket server.
    /// </summary>
    public async Task DisconnectAsync()
    {
        _cts.Cancel();
        if (_ws?.State == WebSocketState.Open)
        {
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch { /* ignore */ }
        }

        if (_receiveLoop != null)
        {
            try { await _receiveLoop; } catch { /* ignore */ }
        }

        Connected = false;
    }

    // ---- Connection-level subscriptions ----
    // Use these to receive order, position, balance, and finres updates
    // for ALL tickers on a connection. Events: OnOrderUpdate, OnPositionUpdate,
    // OnBalanceUpdate, OnFinresUpdate.

    /// <summary>
    /// Subscribe to order, position, balance, and finres updates for a connection.
    /// Events: OnOrderUpdate, OnPositionUpdate, OnBalanceUpdate, OnFinresUpdate.
    /// </summary>
    public void Subscribe(long connectionId)
        => Send("subscribe", new { ConnectionId = connectionId });

    /// <summary>
    /// Unsubscribe from connection-level updates.
    /// </summary>
    public void Unsubscribe(long connectionId)
        => Send("unsubscribe", new { ConnectionId = connectionId });

    // ---- Market data subscriptions ----
    // Use these to receive real-time market data for a SPECIFIC ticker on a connection.
    // These are independent from Subscribe() — you can use one without the other.
    // Events: OnTradeUpdate, OnOrderBookSnapshot, OnOrderBookUpdate.

    /// <summary>
    /// Subscribe to real-time trade updates for a specific ticker.
    /// Event: OnTradeUpdate.
    ///
    /// Trades are aggregated server-side using the order book's AddingTicksForAPeriod
    /// setting (per-(connection, ticker), default 200 ms). Consecutive same-side trades
    /// inside the window are merged into one entry — Size is summed, Price and Time track
    /// the latest merged trade. Set AddingTicksForAPeriod = 0 in the order book settings
    /// to receive the raw stream.
    /// </summary>
    public void SubscribeTrades(long connectionId, string ticker)
        => Send("trade_subscribe", new { ConnectionId = connectionId, Ticker = ticker });

    /// <summary>
    /// Unsubscribe from trade updates for a specific ticker.
    /// </summary>
    public void UnsubscribeTrades(long connectionId, string ticker)
        => Send("trade_unsubscribe", new { ConnectionId = connectionId, Ticker = ticker });

    /// <summary>
    /// Subscribe to order book updates for a specific ticker.
    /// When zoomIndex is 0 (default), you receive the full order book snapshot + incremental updates.
    /// When zoomIndex &gt; 1, price levels are aggregated into zoomed buckets (same as trades).
    /// <para>
    /// Optional depth filters:
    /// <list type="bullet">
    ///   <item><c>depthLevels</c> (must be ≥ 1): trims the snapshot to the top N price levels per side
    ///     (asks ascending, bids descending), applied AFTER zoom and <c>depthPercent</c>.
    ///     Filters the snapshot ONLY — incremental updates are unaffected.</item>
    ///   <item><c>depthPercent</c> (must be &gt; 0): per-side band as a percentage, anchored on best ask /
    ///     best bid (NOT mid). Asks kept where <c>price &lt;= bestAsk * (1 + depthPercent / 100)</c>;
    ///     bids where <c>price &gt;= bestBid * (1 - depthPercent / 100)</c>. Applies to both snapshot
    ///     and updates; the band refreshes from the latest known best ask / best bid on each event.
    ///     If a side's anchor is unknown, that side is not filtered (degrades open).</item>
    ///   <item><c>fetchSnapshot</c> (default <c>true</c>): when <c>false</c> AND this subscriber is the
    ///     first to ask for this ticker, the exchange REST snapshot fetch is skipped — only the WS feed is
    ///     subscribed and you receive only delta updates. Useful for mass-subscribing to many tickers
    ///     without hitting the exchange's REST rate limit. Seed state separately via
    ///     <see cref="MetaScalpClient.GetOrderBookSnapshotAsync"/> when needed. If a later subscriber
    ///     (UI or another API client with <c>fetchSnapshot=true</c>) joins, a snapshot is fetched lazily
    ///     and delivered to all subscribers.</item>
    /// </list>
    /// <c>bestAsk</c> / <c>bestBid</c> payload fields are never filtered.
    /// </para>
    /// </summary>
    public void SubscribeOrderBook(long connectionId, string ticker, int zoomIndex = 0,
        int? depthLevels = null, decimal? depthPercent = null, bool fetchSnapshot = true)
    {
        // Build the payload dynamically so we only include fields that differ from defaults.
        // FetchSnapshot is only emitted when false to keep the on-wire payload compatible
        // with older servers that don't know the field.
        var payload = new Dictionary<string, object>
        {
            ["ConnectionId"] = connectionId,
            ["Ticker"] = ticker,
            ["ZoomIndex"] = zoomIndex
        };
        if (depthLevels is not null) payload["DepthLevels"] = depthLevels;
        if (depthPercent is not null) payload["DepthPercent"] = depthPercent;
        if (!fetchSnapshot) payload["FetchSnapshot"] = false;
        Send("orderbook_subscribe", payload);
    }

    /// <summary>
    /// Unsubscribe from order book updates for a specific ticker.
    /// </summary>
    public void UnsubscribeOrderBook(long connectionId, string ticker)
        => Send("orderbook_unsubscribe", new { ConnectionId = connectionId, Ticker = ticker });

    /// <summary>
    /// Subscribe to mark price updates for a specific ticker (futures only).
    /// No initial snapshot — only live updates. Event: OnMarkPriceUpdate.
    /// </summary>
    public void SubscribeMarkPrice(long connectionId, string ticker)
        => Send("mark_price_subscribe", new { ConnectionId = connectionId, Ticker = ticker });

    /// <summary>
    /// Unsubscribe from mark price updates for a specific ticker.
    /// </summary>
    public void UnsubscribeMarkPrice(long connectionId, string ticker)
        => Send("mark_price_unsubscribe", new { ConnectionId = connectionId, Ticker = ticker });

    /// <summary>
    /// Subscribe to funding rate updates for a specific ticker (perpetual futures only).
    /// No initial snapshot — only live updates. Event: OnFundingUpdate.
    /// </summary>
    public void SubscribeFunding(long connectionId, string ticker)
        => Send("funding_subscribe", new { ConnectionId = connectionId, Ticker = ticker });

    /// <summary>
    /// Unsubscribe from funding rate updates for a specific ticker.
    /// </summary>
    public void UnsubscribeFunding(long connectionId, string ticker)
        => Send("funding_unsubscribe", new { ConnectionId = connectionId, Ticker = ticker });

    // ---- Notification subscriptions ----
    // App-wide notifications (trades, signal levels, large amounts, screener).
    // Independent from Subscribe() — no connectionId required.
    // Events: OnNotificationSnapshot, OnNotificationUpdate.

    /// <summary>
    /// Subscribe to app-wide notifications. Receives a snapshot of recent notifications, then live updates.
    /// Events: OnNotificationSnapshot (once), then OnNotificationUpdate (continuous).
    /// </summary>
    public void SubscribeNotifications()
        => Send("notification_subscribe", new { });

    /// <summary>
    /// Unsubscribe from notification updates.
    /// </summary>
    public void UnsubscribeNotifications()
        => Send("notification_unsubscribe", new { });

    // ---- Signal level subscriptions ----

    public void SubscribeSignalLevels()
        => Send("signal_level_subscribe", new { });

    public void UnsubscribeSignalLevels()
        => Send("signal_level_unsubscribe", new { });

    // ---- MetaBroker analytics streams ----
    // Density map / large trades / liquidations — app-wide feeds relayed from the MetaBroker
    // backend (the same data the terminal's analytics windows show). No connectionId required.
    // One subscription per feed per socket: re-subscribing REPLACES the config.

    /// <summary>
    /// Subscribe to the MetaBroker density map notifications stream — order book walls, each
    /// reported exactly once on first sight of its id. <c>ExchangeMarkets</c> is required and must
    /// be non-empty; everything else defaults to the terminal's Density Map window defaults
    /// (coefficients 3/2/1, lifetimes 5 min, quote assets USDT). Re-subscribing replaces the
    /// config without replaying already-notified walls.
    /// Events: OnDensityMapSnapshot (after the ack), then OnDensityMapUpdate (continuous).
    /// </summary>
    public void SubscribeDensityMap(DensityMapSubscribeOptions options)
        => Send("density_map_subscribe", options);

    /// <summary>
    /// Stop the density map stream (tears down the upstream feed).
    /// </summary>
    public void UnsubscribeDensityMap()
        => Send("density_map_unsubscribe", new { });

    /// <summary>
    /// Subscribe to the MetaBroker large trades stream — aggregated trade prints, final and
    /// append-only (no snapshot; history starts at subscribe time). <c>ExchangeMarkets</c> is
    /// required and must be non-empty; <c>AggregationMs</c> defaults to 500 (0 = every raw print).
    /// Re-subscribing replaces the config. Event: OnLargeTradesUpdate.
    /// </summary>
    public void SubscribeLargeTrades(LargeTradesSubscribeOptions options)
        => Send("large_trades_subscribe", options);

    /// <summary>
    /// Stop the large trades stream.
    /// </summary>
    public void UnsubscribeLargeTrades()
        => Send("large_trades_unsubscribe", new { });

    /// <summary>
    /// Subscribe to the MetaBroker cross-exchange liquidations stream (futures only).
    /// No MetaBroker login is needed: the upstream is the screener-v2 hub signed with the shared
    /// service key. <c>Exchanges</c> is required and must be non-empty. Re-subscribing replaces
    /// the filters and the server re-sends a snapshot.
    /// Events: OnLiquidationsSnapshot (after every subscribe/replace), OnLiquidationsUpdate
    /// (live rows, newest first), OnLiquidationsMetadata (totals + top tokens, sub-second cadence).
    /// </summary>
    public void SubscribeLiquidations(LiquidationsSubscribeOptions options)
        => Send("liquidations_subscribe", options);

    /// <summary>
    /// Stop the liquidations stream.
    /// </summary>
    public void UnsubscribeLiquidations()
        => Send("liquidations_unsubscribe", new { });

    // ---- Internals ----

    private void Send(string type, object data)
    {
        if (_ws?.State != WebSocketState.Open)
            throw new InvalidOperationException("Not connected");

        var json = JsonConvert.SerializeObject(new { Type = type, Data = data });
        var buffer = Encoding.UTF8.GetBytes(json);
        _ = _ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        try
        {
            while (_ws?.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                        return;
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                    DispatchMessage(json);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { }
        finally
        {
            Connected = false;
            OnDisconnected?.Invoke();
        }
    }

    private void DispatchMessage(string json)
    {
        try
        {
            var obj = JObject.Parse(json);
            var type = (obj["Type"] ?? obj["type"])?.ToString();
            var data = obj["Data"] ?? obj["data"];
            if (type == null || data == null) return;

            switch (type)
            {
                case "order_update":
                    OnOrderUpdate?.Invoke(data.ToObject<OrderUpdateData>()!);
                    break;
                case "position_update":
                    OnPositionUpdate?.Invoke(data.ToObject<PositionUpdateData>()!);
                    break;
                case "balance_update":
                    OnBalanceUpdate?.Invoke(data.ToObject<BalanceUpdateData>()!);
                    break;
                case "finres_update":
                    OnFinresUpdate?.Invoke(data.ToObject<FinresUpdateData>()!);
                    break;
                case "trade_update":
                    OnTradeUpdate?.Invoke(data.ToObject<TradeUpdateData>()!);
                    break;
                case "orderbook_snapshot":
                    OnOrderBookSnapshot?.Invoke(data.ToObject<OrderBookSnapshotData>()!);
                    break;
                case "orderbook_update":
                    OnOrderBookUpdate?.Invoke(data.ToObject<OrderBookUpdateData>()!);
                    break;
                case "mark_price_update":
                    OnMarkPriceUpdate?.Invoke(data.ToObject<MarkPriceUpdateData>()!);
                    break;
                case "funding_update":
                    OnFundingUpdate?.Invoke(data.ToObject<FundingUpdateData>()!);
                    break;
                case "notification_snapshot":
                    OnNotificationSnapshot?.Invoke(data.ToObject<NotificationSnapshotData>()!);
                    break;
                case "notification_update":
                    OnNotificationUpdate?.Invoke(data.ToObject<NotificationUpdateData>()!);
                    break;
                case "signal_levels_snapshot":
                    OnSignalLevelsSnapshot?.Invoke(data.ToObject<SignalLevelsSnapshotData>()!);
                    break;
                case "signal_level_placed":
                    OnSignalLevelPlaced?.Invoke(data.ToObject<SignalLevelPlacedData>()!);
                    break;
                case "signal_level_triggered":
                    OnSignalLevelTriggered?.Invoke(data.ToObject<SignalLevelTriggeredData>()!);
                    break;
                case "signal_level_removed":
                    OnSignalLevelRemoved?.Invoke(data.ToObject<SignalLevelRemovedData>()!);
                    break;
                case "signal_levels_removed_all":
                    OnSignalLevelsRemovedAll?.Invoke();
                    break;
                case "signal_levels_removed_triggered":
                    OnSignalLevelsRemovedTriggered?.Invoke();
                    break;
                case "density_map_snapshot":
                    OnDensityMapSnapshot?.Invoke(data.ToObject<DensityMapSnapshotData>()!);
                    break;
                case "density_map_update":
                    OnDensityMapUpdate?.Invoke(data.ToObject<DensityMapUpdateData>()!);
                    break;
                case "large_trades_update":
                    OnLargeTradesUpdate?.Invoke(data.ToObject<LargeTradesUpdateData>()!);
                    break;
                case "liquidations_snapshot":
                    OnLiquidationsSnapshot?.Invoke(data.ToObject<LiquidationsSnapshotData>()!);
                    break;
                case "liquidations_update":
                    OnLiquidationsUpdate?.Invoke(data.ToObject<LiquidationsUpdateData>()!);
                    break;
                case "liquidations_metadata":
                    OnLiquidationsMetadata?.Invoke(data.ToObject<LiquidationsMetadataData>()!);
                    break;
                case "error":
                    OnError?.Invoke(data["Error"]?.ToString() ?? json);
                    break;
            }
        }
        catch
        {
            // ignore malformed messages
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _ws?.Dispose();
    }
}
