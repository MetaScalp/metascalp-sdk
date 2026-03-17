using MetaScalp.Sdk;

// ---- REST Example ----
Console.WriteLine("=== MetaScalp SDK Example ===\n");

var client = await MetaScalpClient.DiscoverAsync();
Console.WriteLine($"Connected to MetaScalp on port {client.Port}");

var connectionsResponse = await client.GetConnectionsAsync();
Console.WriteLine($"Found {connectionsResponse.Connections.Count} connection(s)");

foreach (var conn in connectionsResponse.Connections)
{
    Console.WriteLine($"  [{conn.Id}] {conn.Name} — {conn.Exchange} {conn.Market}");

    var balanceResponse = await client.GetBalanceAsync(conn.Id);
    foreach (var b in balanceResponse.Balances)
    {
        if (b.Total > 0)
            Console.WriteLine($"    {b.Coin}: total={b.Total}, free={b.Free}, locked={b.Locked}");
    }
}

// ---- WebSocket Example ----
Console.WriteLine("\n=== WebSocket Streaming ===\n");

var firstConn = connectionsResponse.Connections.First();
var ticker = "BTCUSDT";

var socket = await MetaScalpSocket.DiscoverAsync();
Console.WriteLine($"WebSocket connected on port {socket.Port}");

// These events fire from Subscribe() — connection-level, all tickers
socket.OnOrderUpdate += data =>
    Console.WriteLine($"  Order: {data.Ticker} {data.Side} {data.Price} x {data.Size} [{data.Status}]");

socket.OnPositionUpdate += data =>
    Console.WriteLine($"  Position: {data.Ticker} {data.Side} {data.Size} @ {data.AvgPrice}");

socket.OnBalanceUpdate += data =>
    Console.WriteLine($"  Balance: {string.Join(", ", data.Balances.Select(b => $"{b.Coin}={b.Total}"))}");

// These events fire from SubscribeTrades() — specific to a (connectionId, ticker) pair
socket.OnTradeUpdate += data =>
{
    foreach (var trade in data.Trades)
    {
        var color = trade.Side == "Buy" ? ConsoleColor.Green : ConsoleColor.Red;
        Console.ForegroundColor = color;
        Console.Write($"  {trade.Side,-4}");
        Console.ResetColor();
        Console.WriteLine($" {trade.Price} x {trade.Size} @ {trade.Time:HH:mm:ss.fff}");
    }
};

// These events fire from SubscribeOrderBook() — specific to a (connectionId, ticker) pair
socket.OnOrderBookSnapshot += data =>
    Console.WriteLine($"  Order book snapshot: {data.Asks.Count} asks, {data.Bids.Count} bids");

socket.OnOrderBookUpdate += data =>
    Console.WriteLine($"  Order book update: {data.Updates.Count} levels changed");

socket.OnError += error =>
    Console.WriteLine($"  Error: {error}");

// Connection-level: orders, positions, balances for ALL tickers on this connection
socket.Subscribe(firstConn.Id);

// Market data: trades and order book for a SPECIFIC ticker (independent from Subscribe)
socket.SubscribeTrades(firstConn.Id, ticker);
socket.SubscribeOrderBook(firstConn.Id, ticker);

Console.WriteLine($"Subscribed to connection {firstConn.Id}, trades and order book for {ticker}");
Console.WriteLine("Listening... (press Enter to stop)\n");

Console.ReadLine();

socket.UnsubscribeTrades(firstConn.Id, ticker);
socket.UnsubscribeOrderBook(firstConn.Id, ticker);
socket.Unsubscribe(firstConn.Id);
await socket.DisconnectAsync();
client.Dispose();

Console.WriteLine("Done.");
