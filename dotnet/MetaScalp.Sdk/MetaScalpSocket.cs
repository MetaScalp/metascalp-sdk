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
    /// You will receive one OnOrderBookSnapshot followed by OnOrderBookUpdate events.
    /// </summary>
    public void SubscribeOrderBook(long connectionId, string ticker)
        => Send("orderbook_subscribe", new { ConnectionId = connectionId, Ticker = ticker });

    /// <summary>
    /// Unsubscribe from order book updates for a specific ticker.
    /// </summary>
    public void UnsubscribeOrderBook(long connectionId, string ticker)
        => Send("orderbook_unsubscribe", new { ConnectionId = connectionId, Ticker = ticker });

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
