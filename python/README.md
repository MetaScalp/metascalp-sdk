# MetaScalp SDK

Official SDK for [MetaScalp](https://metascalp.io) API — connect your trading bots and scripts to the MetaScalp terminal via REST and WebSocket.

MetaScalp exposes a local API that lets you query exchange data, execute trades, and stream real-time market data (trades, order book, mark/index price, funding) and account updates (orders, positions, balances) — plus manage signal levels, plain levels, chart annotations, the notification feed and the terminal UI itself, and stream the MetaBroker analytics feeds (density map, large trades, liquidations).

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

Full reference with request/response shapes: [MetaScalp API docs](https://metascalp.github.io/metascalp-sdk/) ([markdown](./docs/MetaScalp-Api.md)).

### REST Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/ping` | Discover running MetaScalp instance |
| `GET` | `/api/connections` | List active exchange connections |
| `GET` | `/api/connections/{id}` | Get single connection details |
| `GET` | `/api/connections/{id}/tickers` | List available tickers (`?Refresh=true` re-fetches from the exchange) |
| `GET` | `/api/connections/{id}/orders?Ticker=X` | Get open orders |
| `GET` | `/api/connections/{id}/positions` | Get open positions |
| `GET` | `/api/connections/{id}/balance` | Get account balances |
| `GET` | `/api/connections/{id}/orderbook-snapshot?Ticker=X` | One-shot fresh order book snapshot from the exchange REST endpoint |
| `GET` | `/api/connections/{id}/cluster-snapshot?Ticker=X&TimeFrame=M5` | Cluster (volume profile) snapshot; `Columns=1..100` fills more history than the default 10 columns |
| `POST` | `/api/connections/{id}/orders` | Place an order |
| `POST` | `/api/connections/{id}/orders/cancel` | Cancel an order |
| `POST` | `/api/connections/{id}/orders/cancel-all` | Cancel all orders for a ticker |
| `GET/POST/PUT/DELETE` | `/api/connections/{id}/signal-levels[/{slId}]` | Full signal-level CRUD (+ `DELETE /api/signal-levels/triggered`) |
| `GET/POST/PUT/DELETE` | `/api/connections/{id}/user-levels[/{ulId}]` | Full user (plain) level CRUD |
| `GET/PUT/POST/DELETE` | `/api/connections/{id}/annotations[/{type}[/{index}]]` | Chart annotations: read all three lists, replace a list, append one, delete by index, clear all |
| `GET/PUT` | `/api/connections/{id}/orderbook-settings?Ticker=X` | Read / partially update order book settings |
| `POST` | `/api/notifications` | Inject a custom row into the notification feed |
| `GET` | `/api/ui/state`, `/api/ui/windows/{windowId}` | Read-only inventory of the open UI (windows, tabs, documents) |
| `POST` | `/api/ui/tabs/{tabId}/close`, `.../{tabId}/activate` | Close one tab by id (refuses a window's only tab), switch to one tab by id — ids from `GET /api/ui/state` |
| `PUT` | `/api/ui/documents/{externalId}/link-number`, `.../ticker` | Set a panel's link group / re-point a panel to another market |
| `POST` | `/api/change-ticker` | Switch ticker in MetaScalp UI |
| `POST` | `/api/combo` | Open combo layout (`activate: false` opens it in the background) |
| `POST` | `/api/close-last-tab` | Close the main window's last tab (test surface; refuses to close the only tab) |

### WebSocket Messages

**Connection-level** — subscribe by `connectionId`:

| Subscribe | Updates received |
|-----------|-----------------|
| `subscribe` | `order_update`, `position_update`, `balance_update`, `finres_update` |

**Market data** — subscribe by `connectionId` + `ticker`:

| Subscribe | Updates received |
|-----------|-----------------|
| `trade_subscribe` | `trade_update` (aggregated ticks carry `highPrice`/`lowPrice`) |
| `orderbook_subscribe` | `orderbook_snapshot`, `orderbook_update` |
| `mark_price_subscribe` | `mark_price_update` (futures only) |
| `index_price_subscribe` | `index_price_update` (futures only) |
| `funding_subscribe` | `funding_update` (perpetual futures only) |
| `annotation_subscribe` | one-shot `annotations_snapshot` of the current chart annotations |

**App-wide** — no `connectionId` required:

| Subscribe | Updates received |
|-----------|-----------------|
| `notification_subscribe` | `notification_update` (including custom rows injected via `POST /api/notifications`) |
| `signal_level_subscribe` | `signal_level_placed/updated/triggered/removed/...` |
| `user_level_subscribe` | `user_level_placed/updated/removed/...` |
| `ui_subscribe` | `ui_snapshot`, then `ui_update` on UI changes |
| `density_map_subscribe` | `density_map_snapshot`, then `density_map_update` — MetaBroker order-book walls, one notification per wall |
| `large_trades_subscribe` | `large_trades_update` — MetaBroker aggregated large trade prints (append-only, no snapshot) |
| `liquidations_subscribe` | `liquidations_snapshot`, `liquidations_update`, `liquidations_metadata` — MetaBroker cross-exchange liquidations (no MetaBroker login needed) |

> **SDK coverage note.** The js / python / dotnet convenience wrappers currently cover the core surface (connections, orders, positions, balances, market data) plus the MetaBroker analytics streams (density map, large trades, liquidations). The newer families (levels, annotations, notifications, UI) are available through the same clients as plain REST calls / raw WS messages until dedicated wrappers ship.

> **Mass-subscribe optimization.** `orderbook_subscribe` accepts an optional `fetchSnapshot` field (default `true`). Pass `false` to skip the exchange REST snapshot fetch when subscribing — useful when subscribing to 100+ tickers at once without hitting exchange REST rate limits. Seed state separately via `GET /api/connections/{id}/orderbook-snapshot` when you need it. A later subscriber that wants a snapshot triggers a lazy fetch that fans out to all listeners.

## Connection Details

- **Host:** `127.0.0.1` (localhost only)
- **Port range:** `17845`–`17855` (first available, shared by HTTP and WebSocket)
- Both REST and WebSocket run on the same port — no separate socket port
- All SDKs include auto-discovery that scans the port range

## License

MIT
