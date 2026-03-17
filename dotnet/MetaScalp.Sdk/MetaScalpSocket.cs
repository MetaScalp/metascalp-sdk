using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace MetaScalp.Sdk;

/// <summary>
/// WebSocket client for MetaScalp real-time updates.
/// Supports connection-level subscriptions (orders, positions, balances)
/// and market data subscriptions (trades, order book) scoped by connectionId + ticker.
/// </summary>
public class MetaScalpSocket : IDisposable
{
    private const int PortStart = 17856;
    private const int PortEnd = 17866;

    private static readonly JsonSerializerSettings CamelCaseSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private ClientWebSocket? _ws;
    private CancellationTokenSource _cts = new();
    private Task? _receiveLoop;

    public int Port { get; }
    public bool Connected { get; private set; }

    // ---- Events ----

    public event Action<OrderUpdateData>? OnOrderUpdate;
    public event Action<PositionUpdateData>? OnPositionUpdate;
    public event Action<BalanceUpdateData>? OnBalanceUpdate;
    public event Action<FinresUpdateData>? OnFinresUpdate;
    public event Action<TradeUpdateData>? OnTradeUpdate;
    public event Action<OrderBookSnapshotData>? OnOrderBookSnapshot;
    public event Action<OrderBookUpdateData>? OnOrderBookUpdate;
    public event Action<string>? OnError;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public MetaScalpSocket(int port)
    {
        Port = port;
    }

    /// <summary>
    /// Scans ports 17856-17866 to find the MetaScalp WebSocket server.
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

    public void Subscribe(long connectionId)
        => Send("subscribe", new { connectionId });

    public void Unsubscribe(long connectionId)
        => Send("unsubscribe", new { connectionId });

    // ---- Market data subscriptions ----

    public void SubscribeTrades(long connectionId, string ticker)
        => Send("trade_subscribe", new { connectionId, ticker });

    public void UnsubscribeTrades(long connectionId, string ticker)
        => Send("trade_unsubscribe", new { connectionId, ticker });

    public void SubscribeOrderBook(long connectionId, string ticker)
        => Send("orderbook_subscribe", new { connectionId, ticker });

    public void UnsubscribeOrderBook(long connectionId, string ticker)
        => Send("orderbook_unsubscribe", new { connectionId, ticker });

    // ---- Internals ----

    private void Send(string type, object data)
    {
        if (_ws?.State != WebSocketState.Open)
            throw new InvalidOperationException("Not connected");

        var json = JsonConvert.SerializeObject(new { type, data });
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
                case "error":
                    OnError?.Invoke(data["error"]?.ToString() ?? json);
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
