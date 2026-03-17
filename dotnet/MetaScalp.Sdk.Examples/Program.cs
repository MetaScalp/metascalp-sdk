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

socket.OnOrderBookSnapshot += data =>
    Console.WriteLine($"  Order book snapshot: {data.Asks.Count} asks, {data.Bids.Count} bids");

socket.OnOrderBookUpdate += data =>
    Console.WriteLine($"  Order book update: {data.Updates.Count} levels changed");

socket.OnError += error =>
    Console.WriteLine($"  Error: {error}");

// Subscribe
socket.Subscribe(firstConn.Id);
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
