# MetaScalp SDK

Official SDK for [MetaScalp](https://metascalp.io) API — connect your trading bots and scripts to the MetaScalp terminal via REST and WebSocket.

MetaScalp exposes a local API that lets you query exchange data, execute trades, and stream real-time market data (trades, order book) and account updates (orders, positions, balances).

## Available SDKs

| Language | Directory | Install |
|----------|-----------|---------|
| **JavaScript / TypeScript** | [`js/`](./js) | `npm install metascalp` |
| **Python** | [`python/`](./python) | `pip install metascalp` |
| **C# / .NET** | [`dotnet/`](./dotnet) | `dotnet add package MetaScalp.Sdk` |

## Quick Start

### JavaScript / TypeScript

```typescript
import { MetaScalpClient, MetaScalpSocket } from 'metascalp';

// REST — discover MetaScalp and query data
const client = await MetaScalpClient.discover();
const { connections } = await client.getConnections();
const conn = connections[0];

// Place a limit buy order
await client.placeOrder(conn.id, {
  ticker: 'BTCUSDT',
  side: 1,
  price: 65000,
  size: 0.01,
  type: 0
});

// WebSocket — stream real-time updates
const socket = await MetaScalpSocket.discover();

// Connection-level: orders, positions, balances for ALL tickers on this connection
socket.subscribe(conn.id);
socket.on('order_update', (data) => console.log('Order:', data));
socket.on('position_update', (data) => console.log('Position:', data));
socket.on('balance_update', (data) => console.log('Balance:', data));

// Market data: trades and order book for a SPECIFIC ticker (independent from subscribe)
socket.subscribeTrades(conn.id, 'BTCUSDT');
socket.on('trade_update', (data) => console.log('Trade:', data));

socket.subscribeOrderBook(conn.id, 'BTCUSDT');
socket.on('orderbook_snapshot', (data) => console.log('OB Snapshot:', data));
socket.on('orderbook_update', (data) => console.log('OB Update:', data));
```

### Python

```python
import asyncio
from metascalp import MetaScalpClient, MetaScalpSocket

async def main():
    # REST
    client = await MetaScalpClient.discover()
    connections = await client.get_connections()
    conn = connections['connections'][0]

    # Place order
    await client.place_order(conn['id'], ticker='BTCUSDT', side=1, price=65000, size=0.01)

    # WebSocket
    socket = await MetaScalpSocket.discover()

    # Connection-level events (from subscribe) — all tickers
    @socket.on('order_update')
    def on_order(data):
        print(f"Order: {data['ticker']} {data['side']} {data['status']}")

    @socket.on('balance_update')
    def on_balance(data):
        print(f"Balance: {data}")

    # Market data events (from subscribe_trades / subscribe_order_book) — specific ticker
    @socket.on('trade_update')
    def on_trade(data):
        print(f"Trade: {data}")

    @socket.on('orderbook_snapshot')
    def on_snapshot(data):
        print(f"Snapshot: {len(data['asks'])} asks, {len(data['bids'])} bids")

    # Connection-level: orders, positions, balances for ALL tickers
    socket.subscribe(conn['id'])

    # Market data: trades and order book for a SPECIFIC ticker (independent from subscribe)
    socket.subscribe_trades(conn['id'], 'BTCUSDT')
    socket.subscribe_order_book(conn['id'], 'BTCUSDT')

    await socket.listen_forever()

asyncio.run(main())
```

### C# / .NET

```csharp
using MetaScalp.Sdk;

// REST
var client = await MetaScalpClient.DiscoverAsync();
var connections = await client.GetConnectionsAsync();
var conn = connections.First();

await client.PlaceOrderAsync(conn.Id, new PlaceOrderRequest
{
    Ticker = "BTCUSDT",
    Side = 1,
    Price = 65000m,
    Size = 0.01m,
    Type = 0
});

// WebSocket
var socket = await MetaScalpSocket.DiscoverAsync();

// Connection-level events (from Subscribe) — all tickers
socket.OnOrderUpdate += (data) => Console.WriteLine($"Order: {data.Ticker} {data.Side} {data.Status}");
socket.OnBalanceUpdate += (data) => Console.WriteLine($"Balance updated");

// Market data events (from SubscribeTrades / SubscribeOrderBook) — specific ticker
socket.OnTradeUpdate += (data) => Console.WriteLine($"Trade: {data.Ticker} {data.Trades.Count} trades");
socket.OnOrderBookSnapshot += (data) => Console.WriteLine($"OB: {data.Asks.Count} asks, {data.Bids.Count} bids");

// Connection-level: orders, positions, balances for ALL tickers
socket.Subscribe(conn.Id);

// Market data: trades and order book for a SPECIFIC ticker (independent from Subscribe)
socket.SubscribeTrades(conn.Id, "BTCUSDT");
socket.SubscribeOrderBook(conn.Id, "BTCUSDT");
```

## API Overview

### REST Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/ping` | Discover running MetaScalp instance |
| `GET` | `/api/connections` | List active exchange connections |
| `GET` | `/api/connections/{id}` | Get single connection details |
| `GET` | `/api/connections/{id}/tickers` | List available tickers |
| `GET` | `/api/connections/{id}/orders?Ticker=X` | Get open orders |
| `GET` | `/api/connections/{id}/positions` | Get open positions |
| `GET` | `/api/connections/{id}/balance` | Get account balances |
| `POST` | `/api/connections/{id}/orders` | Place an order |
| `POST` | `/api/connections/{id}/orders/cancel` | Cancel an order |
| `POST` | `/api/change-ticker` | Switch ticker in MetaScalp UI |
| `POST` | `/api/combo` | Open combo layout |

### WebSocket Messages

**Connection-level** — subscribe by `connectionId`:

| Subscribe | Updates received |
|-----------|-----------------|
| `subscribe` | `order_update`, `position_update`, `balance_update`, `finres_update` |

**Market data** — subscribe by `connectionId` + `ticker`:

| Subscribe | Updates received |
|-----------|-----------------|
| `trade_subscribe` | `trade_update` |
| `orderbook_subscribe` | `orderbook_snapshot`, `orderbook_update` |
| `mark_price_subscribe` | `mark_price_update` (futures only) |
| `funding_subscribe` | `funding_update` (perpetual futures only) |

## Connection Details

- **Host:** `127.0.0.1` (localhost only)
- **Port range:** `17845`–`17855` (first available, shared by HTTP and WebSocket)
- Both REST and WebSocket run on the same port — no separate socket port
- All SDKs include auto-discovery that scans the port range

## License

MIT
