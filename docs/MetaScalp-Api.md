# MetaScalp API

Connect your scripts and bots to MetaScalp to interact with exchange connections. Use **HTTP REST** to query data and execute trades, or **WebSocket** to receive real-time order, position, balance, trade, and order book updates.

- **Host:** `127.0.0.1` (localhost only)
- **Port range:** `17845`–`17855` (first available port is used, shared by HTTP and WebSocket)
- **Content-Type:** `application/json`
- **CORS:** All HTTP responses include `Access-Control-Allow-Origin: *`. OPTIONS preflight is supported on all routes.

## Overview

### HTTP REST endpoints

Use HTTP to discover connections, query data, and execute trades:

| Endpoint | Purpose |
|---|---|
| `GET /ping` | Find the running MetaScalp instance and check its version |
| `POST /api/change-ticker` | Switch the active ticker in the MetaScalp UI |
| `POST /api/combo` | Open a combo layout for a ticker |
| `GET /api/connections` | List all active exchange connections |
| `GET /api/connections/{id}/...` | Query tickers, orders, positions, balances for a connection |
| `POST /api/connections/{id}/orders` | Place an order on a connection |
| `POST /api/connections/{id}/orders/cancel` | Cancel a single order |
| `POST /api/connections/{id}/orders/cancel-all` | Cancel all orders for a ticker |
| `GET /api/connections/{id}/cluster-snapshot` | Get cluster (volume profile) snapshot data |

### WebSocket streaming

Connect via WebSocket to receive **real-time updates** for your exchange connections.

- Connect to `ws://127.0.0.1:{port}/` (same port as HTTP — scan ports `17845`–`17855`)
- **Connection-level subscriptions:** Send a `subscribe` message with a connection ID to receive order, position, balance, and finres updates
- **Market data subscriptions:** Send `trade_subscribe` or `orderbook_subscribe` with a connection ID + ticker to receive trade or order book updates for a specific symbol
- You can subscribe to multiple connections and tickers simultaneously
- All subscriptions are automatically cleaned up when you disconnect

## Getting started

1. **Launch MetaScalp** — both the HTTP and WebSocket servers start automatically.
2. **Discover the HTTP port** — scan `17845`–`17855` with `GET /ping` to find the running instance.
3. **List connections** — call `GET /api/connections` to see available exchange connections.
4. **Execute operations** — use a connection ID for REST queries, trading, or WebSocket subscriptions.

### Typical REST client flow

```
GET /ping                                         → find MetaScalp
GET /api/connections                              → list active connections
GET /api/connections/{id}/tickers                 → get available tickers
GET /api/connections/{id}/balance                 → check balances
GET /api/connections/{id}/orders?Ticker=BTCUSDT   → view open orders
POST /api/connections/{id}/orders                 → place an order
POST /api/connections/{id}/orders/cancel          → cancel an order
POST /api/connections/{id}/orders/cancel-all      → cancel all orders for a ticker
GET  /api/connections/{id}/cluster-snapshot        → get cluster snapshot data
```

### Typical WebSocket client flow

```
1. Connect
   ws = new WebSocket("ws://127.0.0.1:17845/")

2. Subscribe to a connection (orders, positions, balances, finres)
   → {"Type":"subscribe","Data":{"ConnectionId":1}}
   ← {"Type":"subscribed","Data":{"ConnectionId":1}}

3. Subscribe to market data for a specific ticker
   → {"Type":"trade_subscribe","Data":{"ConnectionId":1,"Ticker":"BTCUSDT","ZoomIndex":1}}
   ← {"Type":"trade_subscribed","Data":{"ConnectionId":1,"Ticker":"BTCUSDT","ZoomIndex":1}}
   → {"Type":"orderbook_subscribe","Data":{"ConnectionId":1,"Ticker":"BTCUSDT"}}
   ← {"Type":"orderbook_subscribed","Data":{"ConnectionId":1,"Ticker":"BTCUSDT"}}

4. Receive real-time updates
   ← {"Type":"order_update","Data":{"ConnectionId":1,"OrderId":123,...}}
   ← {"Type":"position_update","Data":{"ConnectionId":1,...}}
   ← {"Type":"balance_update","Data":{"ConnectionId":1,"Balances":[...]}}
   ← {"Type":"finres_update","Data":{"ConnectionId":1,"Finreses":[...]}}
   ← {"Type":"trade_update","Data":{"ConnectionId":1,"Ticker":"BTCUSDT","Trades":[...]}}
   ← {"Type":"orderbook_snapshot","Data":{"ConnectionId":1,"Ticker":"BTCUSDT","Asks":[...],"Bids":[...],...}}
   ← {"Type":"orderbook_update","Data":{"ConnectionId":1,"Ticker":"BTCUSDT","Updates":[...]}}

5. Unsubscribe when done
   → {"Type":"trade_unsubscribe","Data":{"ConnectionId":1,"Ticker":"BTCUSDT"}}
   ← {"Type":"trade_unsubscribed","Data":{"ConnectionId":1,"Ticker":"BTCUSDT"}}
   → {"Type":"orderbook_unsubscribe","Data":{"ConnectionId":1,"Ticker":"BTCUSDT"}}
   ← {"Type":"orderbook_unsubscribed","Data":{"ConnectionId":1,"Ticker":"BTCUSDT"}}
   → {"Type":"unsubscribe","Data":{"ConnectionId":1}}
   ← {"Type":"unsubscribed","Data":{"ConnectionId":1}}
```

---

## Endpoint Reference

### Discovery

#### Ping

```
GET http://127.0.0.1:{port}/ping
```

**Response `200 OK`:**
```json
{ "App": "MetaScalp", "Version": "0.0.9" }
```

---

### Ticker

#### Change Ticker

Switches the active ticker in MetaScalp. The active window is always notified. When a named binding is provided, that binding's linked panels are notified as well.

```
POST http://127.0.0.1:{port}/api/change-ticker
Content-Type: application/json
```

**Request body**

The endpoint accepts two request formats. Include **either** `TickerPattern` **or** the `Exchange` + `Market` + `Ticker` fields.

**Option A — Ticker pattern**

| Field           | Type   | Required | Description                                                                 |
|-----------------|--------|----------|-----------------------------------------------------------------------------|
| `TickerPattern` | string | yes      | Pattern string (see [Ticker pattern format](#ticker-pattern-format) below). |
| `Binding`       | string | no       | Named binding (`"001"`–`"500"`). Omit or send empty string to only notify the active window. |

**Option B — Explicit fields**

| Field      | Type    | Required | Description                                       |
|------------|---------|----------|---------------------------------------------------|
| `Exchange` | integer | yes      | Exchange identifier (see [Exchange values](#exchange-values))   |
| `Market`   | integer | yes      | Market type identifier (see [MarketType values](#markettype-values)) |
| `Ticker`   | string  | yes      | Trading pair symbol, e.g. `"BTCUSDT"`             |
| `Binding`  | string  | no       | Named binding (`"001"`–`"500"`). Omit or send empty string to only notify the active window. |

**Bindings**

A **binding** is a named group of linked panels inside MetaScalp (e.g. a chart, order book, and trade feed that should all show the same ticker). Bindings are numbered `"001"` through `"500"` and are configured by the user inside the MetaScalp UI. When you send a binding name with a request, all panels assigned to that binding will switch to the new ticker.

| `Binding` value        | Active window notified | Named binding notified |
|------------------------|:----------------------:|:----------------------:|
| omitted / empty / null | yes                    | —                      |
| `"001"` … `"500"`      | yes                    | yes                    |

**Response**

**`200 OK`** — ticker changed successfully:
```json
{ "Status": "ok" }
```

**`400 Bad Request`** — validation error:
```json
{ "Error": "..." }
```

Possible error messages:

| Condition                          | Error message                                                         |
|------------------------------------|-----------------------------------------------------------------------|
| Missing fields                     | `Invalid request body. Provide 'TickerPattern' or 'exchange'+'market'+'ticker'.` |
| Invalid pattern format             | `Invalid ticker pattern: '{pattern}'`                                 |
| Binding name not found             | `Binding '{name}' not found. Available: {list}`                       |
| No connection for exchange + market | `No connection found for exchange {exchange} and market {market}`    |
| Ticker not available on connection | `Ticker '{ticker}' not found on connection {id}`                      |

**Examples**

Using ticker pattern:
```bash
curl -X POST http://127.0.0.1:17845/api/change-ticker \
  -H "Content-Type: application/json" \
  -d '{"TickerPattern": "BINANCE:BTCUSDT.p", "Binding": "001"}'
```

Using explicit fields:
```bash
curl -X POST http://127.0.0.1:17845/api/change-ticker \
  -H "Content-Type: application/json" \
  -d '{"Exchange": 2, "Market": 2, "Ticker": "BTCUSDT", "Binding": "001"}'
```

---

### Combo

#### Open Combo

Opens a combo layout for the specified ticker.

```
POST http://127.0.0.1:{port}/api/combo
Content-Type: application/json
```

**Request body**

| Field    | Type   | Required | Description                           |
|----------|--------|----------|---------------------------------------|
| `Ticker` | string | yes      | Trading pair symbol (not a pattern), e.g. `"BTCUSDT"`. The combo opens on the currently active exchange and market connection. |

**Response**

**`200 OK`:**
```json
{ "Status": "ok" }
```

**`400 Bad Request`:**

| Condition              | Error message                                    |
|------------------------|--------------------------------------------------|
| Missing or empty `Ticker` | `Invalid request body. 'ticker' is required.` |

**Example**

```bash
curl -X POST http://127.0.0.1:17845/api/combo \
  -H "Content-Type: application/json" \
  -d '{"Ticker": "BTCUSDT"}'
```

---

### Connections

#### List Connections

Returns all currently active exchange connections. Use the `Id` from the response to query orders, positions, balances, or to subscribe via WebSocket.

```
GET http://127.0.0.1:{port}/api/connections
```

**Response `200 OK`:**
```json
{
  "Connections": [
    {
      "Id": 1,
      "Name": "Binance Futures",
      "Exchange": "Binance",
      "ExchangeId": 2,
      "Market": "USDT Futures",
      "MarketType": 2,
      "State": 2,
      "ViewMode": false,
      "DemoMode": false
    },
    {
      "Id": 3,
      "Name": "Bybit Spot",
      "Exchange": "Bybit",
      "ExchangeId": 6,
      "Market": "Spot",
      "MarketType": 0,
      "State": 2,
      "ViewMode": false,
      "DemoMode": false
    }
  ]
}
```

Connection fields:

| Field        | Type    | Description |
|--------------|---------|-------------|
| `Id`         | integer | Connection ID — use this for all exchange operations |
| `Name`       | string  | User-defined connection name |
| `Exchange`   | string  | Exchange name (e.g. `"Binance"`, `"Bybit"`) |
| `ExchangeId` | integer | Exchange identifier (see [Exchange values](#exchange-values)) |
| `Market`     | string  | Market display name |
| `MarketType` | integer | Market type (see [MarketType values](#markettype-values)) |
| `State`      | integer | Connection state: `0` Disconnected, `1` Connecting, `2` Connected, `3` Reconnecting, `4` Resetting |
| `ViewMode`   | boolean | `true` = read-only, trading disabled |
| `DemoMode`   | boolean | `true` = paper trading mode |

#### Get Connection

Returns details for a single connection.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}
```

**Response `200 OK`:** Same object as in the list above (single connection, not wrapped in array).

**`404 Not Found`:**
```json
{ "Error": "Connection {ConnectionId} not found" }
```

---

### Trading Operations

All trading endpoints require a valid `{ConnectionId}` in the URL path. If the connection is not found or not active, the API returns an error before executing the operation.

Common errors for all exchange endpoints:

| Condition             | HTTP Status | Error message |
|-----------------------|-------------|---------------|
| Invalid connection ID | `400`       | `Invalid connection ID` |
| Connection not found  | `404`       | `Connection {id} not found` |
| Connection not active | `400`       | `Connection {id} is not active` |

#### Get Tickers

Returns all available trading pairs on a connection.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/tickers
```

**Response `200 OK`:**
```json
{
  "ConnectionId": 1,
  "Count": 354,
  "Tickers": [
    {
      "Name": "BTCUSDT",
      "BaseAsset": "BTC",
      "QuoteAsset": "USDT",
      "IsTradingAllowed": true,
      "PriceIncrement": 0.01,
      "SizeIncrement": 0.001,
      "MinSize": 0.001,
      "MaxSize": 1000.0
    }
  ]
}
```

Ticker fields:

| Field              | Type    | Description |
|--------------------|---------|-------------|
| `Name`             | string  | Trading pair symbol |
| `BaseAsset`        | string  | Base asset (e.g. `"BTC"`) |
| `QuoteAsset`       | string  | Quote asset (e.g. `"USDT"`) |
| `IsTradingAllowed` | boolean | Whether trading is enabled for this pair |
| `PriceIncrement`   | decimal | Minimum price step |
| `SizeIncrement`    | decimal | Minimum size step |
| `MinSize`          | decimal | Minimum order size |
| `MaxSize`          | decimal? | Maximum order size (null if unlimited) |

#### Get Open Orders

Returns open orders for a specific ticker on a connection.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/orders?Ticker=BTCUSDT
```

| Query Parameter | Type   | Required | Description |
|-----------------|--------|----------|-------------|
| `Ticker`        | string | yes      | Trading pair symbol |

**Response `200 OK`:**
```json
{
  "ConnectionId": 1,
  "Ticker": "BTCUSDT",
  "Count": 2,
  "Orders": [
    {
      "Id": 123456789,
      "Ticker": "BTCUSDT",
      "ClientId": "ms_limit_1234",
      "Side": 1,
      "Price": 65000.00,
      "Size": 0.01,
      "FilledSize": 0.0,
      "FilledPrice": 0.0,
      "RemainingSize": 0.01,
      "Status": 1,
      "Type": 0,
      "TriggerPrice": null,
      "CreateDate": "2026-03-13T10:30:00+00:00"
    }
  ]
}
```

Order fields:

| Field           | Type         | Description |
|-----------------|--------------|-------------|
| `Id`            | integer      | Exchange order ID |
| `Ticker`        | string       | Trading pair |
| `ClientId`      | string?      | Client-generated order ID |
| `Side`          | integer      | `0` None, `1` Buy, `2` Sell |
| `Price`         | decimal      | Order price |
| `Size`          | decimal      | Order size |
| `FilledSize`    | decimal      | Filled amount |
| `FilledPrice`   | decimal      | Execution price (0 if not yet filled) |
| `RemainingSize` | decimal      | Remaining amount |
| `Status`        | integer      | `0` New, `1` Open, `2` Closed |
| `Type`          | integer      | `0` Limit, `1` Stop, `2` StopLoss, `3` TakeProfit, `4` Market |
| `TriggerPrice`  | decimal?     | Trigger price for stop/conditional orders |
| `CreateDate`    | string (ISO) | Order creation timestamp |

#### Get Open Positions

Returns all open positions on a connection (futures/margin markets).

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/positions
```

**Response `200 OK`:**
```json
{
  "ConnectionId": 1,
  "Count": 1,
  "Positions": [
    {
      "Id": 1,
      "Ticker": "BTCUSDT",
      "Side": 1,
      "Size": 0.05,
      "AvgPrice": 64500.00,
      "MarginMode": 0
    }
  ]
}
```

Position fields:

| Field        | Type    | Description |
|--------------|---------|-------------|
| `Id`         | integer | Position ID |
| `Ticker`     | string  | Trading pair |
| `Side`       | integer | `1` Buy (Long), `2` Sell (Short) |
| `Size`       | decimal | Position size |
| `AvgPrice`   | decimal | Average entry price |
| `MarginMode` | integer | `0` Cross, `1` Isolated |

#### Get Balance

Returns account balances for all assets on a connection.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/balance
```

**Response `200 OK`:**
```json
{
  "ConnectionId": 1,
  "Count": 3,
  "Balances": [
    {
      "Coin": "USDT",
      "Total": 10000.00,
      "Free": 8500.00,
      "Locked": 1500.00
    }
  ]
}
```

Balance fields:

| Field    | Type    | Description |
|----------|---------|-------------|
| `Coin`   | string  | Asset symbol |
| `Total`  | decimal | Total balance |
| `Free`   | decimal | Available balance |
| `Locked` | decimal | Locked in open orders/positions |

#### Place Order

Places a new order on the exchange through a connection.

```
POST http://127.0.0.1:{port}/api/connections/{ConnectionId}/orders
Content-Type: application/json
```

**Request body:**

| Field        | Type    | Required | Default | Description |
|--------------|---------|----------|---------|-------------|
| `Ticker`     | string  | yes      |         | Trading pair symbol |
| `Side`       | integer | yes      |         | `1` Buy, `2` Sell |
| `Price`      | decimal | yes*     |         | Order price (*required for non-market orders) |
| `Size`       | decimal | yes      |         | Order size (must be > 0) |
| `Type`       | integer | no       | `0`     | `0` Limit, `1` Stop, `2` StopLoss, `3` TakeProfit, `4` Market |
| `ReduceOnly` | boolean | no       | `false` | Close position only, do not open new |

**Response `200 OK`:**
```json
{ "Status": "ok", "ClientId": "ms_limit_1234", "ExecutionTimeMs": 123.45 }
```

The `ClientId` is auto-generated by MetaScalp and can be used to track the order. The `ExecutionTimeMs` field indicates how long the exchange request took to execute, in milliseconds.

**`400 Bad Request`:**

| Condition           | Error message |
|---------------------|---------------|
| Missing fields      | `Invalid request body. 'ticker', 'side', 'price', and 'size' are required.` |
| Size <= 0           | `Size must be greater than zero` |
| Price <= 0 (non-market) | `Price must be greater than zero for non-market orders` |
| Exchange rejected   | *(exchange-specific error message)* |

> **Note:** Error responses from exchange rejection also include `ExecutionTimeMs`.

**Example:**
```bash
curl -X POST http://127.0.0.1:17845/api/connections/1/orders \
  -H "Content-Type: application/json" \
  -d '{"Ticker": "BTCUSDT", "Side": 1, "Price": 65000.00, "Size": 0.01, "Type": 0}'
```

#### Cancel Order

Cancels an existing order on the exchange.

```
POST http://127.0.0.1:{port}/api/connections/{ConnectionId}/orders/cancel
Content-Type: application/json
```

**Request body:**

| Field     | Type    | Required | Default | Description |
|-----------|---------|----------|---------|-------------|
| `Ticker`  | string  | yes      |         | Trading pair symbol |
| `OrderId` | integer | yes      |         | Exchange order ID to cancel |
| `Type`    | integer | no       | `0`     | Order type: `0` Limit, `1` Stop, etc. |

**Response `200 OK`:**
```json
{ "Status": "ok" }
```

**Example:**
```bash
curl -X POST http://127.0.0.1:17845/api/connections/1/orders/cancel \
  -H "Content-Type: application/json" \
  -d '{"Ticker": "BTCUSDT", "OrderId": 123456789, "Type": 0}'
```

#### Cancel All Orders

Cancels all open orders for a given ticker on the exchange.

```
POST http://127.0.0.1:{port}/api/connections/{ConnectionId}/orders/cancel-all
Content-Type: application/json
```

**Request body:**

| Field    | Type   | Required | Description |
|----------|--------|----------|-------------|
| `Ticker` | string | yes      | Trading pair symbol |

**Response `200 OK`:**
```json
{ "Status": "ok", "CancelledCount": 5 }
```

Returns `CancelledCount: 0` if there are no open orders for that ticker.

**Example:**
```bash
curl -X POST http://127.0.0.1:17845/api/connections/1/orders/cancel-all \
  -H "Content-Type: application/json" \
  -d '{"Ticker": "BTCUSDT"}'
```

---

### Market data

#### Cluster snapshot

Returns the current cluster (volume profile / footprint) data for a ticker on a connection. The snapshot contains up to 10 time columns, each holding bid/ask volumes at every price level.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/cluster-snapshot?Ticker=BTCUSDT&TimeFrame=M5&ZoomIndex=1
```

**Query parameters:**

| Parameter   | Type   | Required | Default | Description |
|-------------|--------|----------|---------|-------------|
| `Ticker`    | string | yes      |         | Trading pair symbol |
| `TimeFrame` | string | yes      |         | Cluster timeframe — see [ClusterTimeFrame values](#clustertimeframe-values) |
| `ZoomIndex` | int    | no       | `1`     | Price aggregation factor. `1` = no aggregation (raw price levels). Higher values group price levels into buckets of `ZoomIndex * PriceIncrement`. |

**Response `200 OK`:**

```json
{
  "Ticker": "BTCUSDT",
  "TimeFrame": "M5",
  "ZoomIndex": 1,
  "PriceIncrement": 0.01,
  "Columns": [
    {
      "StartTime": "2026-04-13T10:00:00+00:00",
      "AsksSum": 123.45,
      "BidsSum": 678.90,
      "Items": [
        { "Price": 65000.02, "AskSize": 0.8, "BidSize": 1.1 },
        { "Price": 65000.01, "AskSize": 1.5, "BidSize": 2.3 },
        { "Price": 65000.00, "AskSize": 0.3, "BidSize": 0.9 }
      ]
    }
  ]
}
```

- `Columns` — up to 10 time-period columns (rolling window), ordered chronologically
- `Items` — price levels within each column, ordered by price descending (highest first)
- `AsksSum` / `BidsSum` — total ask/bid volume for the column
- `AskSize` / `BidSize` — volume at each price level (ask = seller-initiated, bid = buyer-initiated)
- `PriceIncrement` — the ticker's minimum price step (useful for interpreting ZoomIndex)

When `ZoomIndex > 1`, prices are grouped into buckets of `ZoomIndex * PriceIncrement` and volumes are summed within each bucket.

**Errors:**

| Status | Condition |
|--------|-----------|
| `400`  | Missing `Ticker` or `TimeFrame`, invalid connection ID |
| `404`  | Connection or ticker not found |

**Example:**
```bash
# Get 5-minute clusters for BTCUSDT with default zoom
curl "http://127.0.0.1:17845/api/connections/1/cluster-snapshot?Ticker=BTCUSDT&TimeFrame=M5"

# Get 1-hour clusters with 5x price aggregation
curl "http://127.0.0.1:17845/api/connections/1/cluster-snapshot?Ticker=BTCUSDT&TimeFrame=H1&ZoomIndex=5"
```

---

### Real-time WebSocket

Connect via WebSocket to receive live updates. Subscribe by connection ID for order, position, balance, and finres events. Subscribe by connection ID + ticker for real-time trade and order book market data.

#### Connection

The WebSocket server shares the same port as the HTTP API (`17845`–`17855`). Connect to:

```
ws://127.0.0.1:{port}/
```

Non-WebSocket HTTP requests to this port receive HTTP `400`.

#### Message format

All messages (inbound and outbound) are JSON with this envelope:

```json
{ "Type": "message_type", "Data": { ... } }
```

#### Messages you send

**Connection-level subscriptions** — subscribe by connection ID to receive order, position, balance, and finres updates:

| Type | Data | Description |
|---|---|---|
| `subscribe` | `{ "ConnectionId": 123 }` | Subscribe to updates for a connection. Connection must be active in MetaScalp. Idempotent — re-subscribing is a no-op. |
| `unsubscribe` | `{ "ConnectionId": 123 }` | Stop receiving updates for a connection. Idempotent. |

**Market data subscriptions** — subscribe by connection ID + ticker to receive trade or order book updates for a specific symbol:

| Type | Data | Description |
|---|---|---|
| `trade_subscribe` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT", "ZoomIndex": 1 }` | Subscribe to real-time trade updates. When `ZoomIndex` > 1, trades are aggregated by zoomed price level before sending. Re-subscribing updates ZoomIndex. Connection and ticker must be valid. |
| `trade_unsubscribe` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | Stop receiving trade updates for that ticker. Idempotent. |
| `orderbook_subscribe` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | Subscribe to order book updates for a specific ticker on a connection. You will receive an initial snapshot followed by incremental updates. Connection must be active. Idempotent. |
| `orderbook_unsubscribe` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | Stop receiving order book updates for that ticker. Idempotent. |

#### Messages you receive

##### Acknowledgements

| Type | Data | When |
|---|---|---|
| `subscribed` | `{ "ConnectionId": 123 }` | After successful connection subscribe |
| `unsubscribed` | `{ "ConnectionId": 123 }` | After successful connection unsubscribe |
| `trade_subscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT", "ZoomIndex": 1 }` | After successful trade subscribe |
| `trade_unsubscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful trade unsubscribe |
| `orderbook_subscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful order book subscribe |
| `orderbook_unsubscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful order book unsubscribe |
| `error` | `{ "Error": "..." }` | Invalid message, unknown type, bad connection ID, or missing ticker |

##### Real-time updates

These are pushed automatically after subscribing. You only receive updates for connection IDs you are subscribed to.

**Order update** — sent when an order is created, modified, filled, or cancelled:

```json
{
  "Type": "order_update",
  "Data": {
    "ConnectionId": 1,
    "OrderId": 98765,
    "Ticker": "BTCUSDT",
    "Side": "Buy",
    "Type": "Limit",
    "Price": 65000.0,
    "FilledPrice": 64980.5,
    "Size": 0.01,
    "FilledSize": 0.0,
    "Fee": 0.0013,
    "FeeCurrency": "USDT",
    "Status": "New",
    "Time": "2025-03-24T14:30:00+00:00"
  }
}
```

| Field | Type | Description |
|---|---|---|
| `ConnectionId` | integer | Connection this order belongs to |
| `OrderId` | integer | Exchange order ID |
| `Ticker` | string | Trading pair symbol |
| `Side` | string | `"Buy"` or `"Sell"` |
| `Type` | string | `"Limit"`, `"Stop"`, `"StopLoss"`, `"TakeProfit"`, `"Market"` |
| `Price` | decimal | Order price |
| `FilledPrice` | decimal | Average filled price |
| `Size` | decimal | Order size |
| `FilledSize` | decimal | Filled amount so far |
| `Fee` | decimal | Trading fee charged |
| `FeeCurrency` | string | Currency the fee is charged in (e.g. `"USDT"`) |
| `Status` | string | `"New"`, `"Open"`, `"Closed"` |
| `Time` | string | Order creation time (ISO 8601) |

**Position update** — sent when a position is opened, modified, or closed:

```json
{
  "Type": "position_update",
  "Data": {
    "ConnectionId": 1,
    "PositionId": 4321,
    "Ticker": "ETHUSDT",
    "Side": "Buy",
    "Size": 1.5,
    "AvgPrice": 3200.00,
    "AvgPriceFix": 3200.00,
    "AvgPriceDyn": 3195.50,
    "Status": "Open"
  }
}
```

| Field | Type | Description |
|---|---|---|
| `ConnectionId` | integer | Connection this position belongs to |
| `PositionId` | integer | Position ID |
| `Ticker` | string | Trading pair symbol |
| `Side` | string | `"Buy"` (Long) or `"Sell"` (Short) |
| `Size` | decimal | Position size |
| `AvgPrice` | decimal | Average entry price (same as `AvgPriceFix`, kept for backwards compatibility) |
| `AvgPriceFix` | decimal | Fixed average price (weighted average of entry orders only) |
| `AvgPriceDyn` | decimal | Dynamic average price (adjusted by realized exit profit) |
| `Status` | string | `"New"`, `"Open"`, `"Closed"` |

**Balance update** — sent when account balances change (debounced ~500ms):

```json
{
  "Type": "balance_update",
  "Data": {
    "ConnectionId": 1,
    "Balances": [
      { "Coin": "USDT", "Total": 10000.0, "Free": 8500.0, "Locked": 1500.0 },
      { "Coin": "BTC", "Total": 0.5, "Free": 0.5, "Locked": 0.0 }
    ]
  }
}
```

| Field | Type | Description |
|---|---|---|
| `ConnectionId` | integer | Connection this balance belongs to |
| `Balances` | array | Array of asset balances |
| `Balances[].Coin` | string | Asset symbol |
| `Balances[].Total` | decimal | Total balance |
| `Balances[].Free` | decimal | Available balance |
| `Balances[].Locked` | decimal | Locked in open orders/positions |

**FinRes update** — sent when financial results are recalculated (after balance or order changes):

```json
{
  "Type": "finres_update",
  "Data": {
    "ConnectionId": 1,
    "Finreses": [
      { "Currency": "USDT", "Result": 250.50, "Fee": 12.30, "Funds": 10000.0, "Available": 8500.0, "Blocked": 1500.0 },
      { "Currency": "BTC", "Result": 0.005, "Fee": 0.0001, "Funds": 0.5, "Available": 0.5, "Blocked": 0.0 }
    ]
  }
}
```

| Field | Type | Description |
|---|---|---|
| `ConnectionId` | integer | Connection this FinRes belongs to |
| `Finreses` | array | Array of per-currency financial results |
| `Finreses[].Currency` | string | Asset symbol (e.g. `"USDT"`, `"BTC"`) |
| `Finreses[].Result` | decimal | Profit/loss since connection was initialized |
| `Finreses[].Fee` | decimal | Accumulated trading fees |
| `Finreses[].Funds` | decimal | Total balance |
| `Finreses[].Available` | decimal | Available (free) balance |
| `Finreses[].Blocked` | decimal | Locked in open orders/positions |

**Trade update** — sent when trades occur for a subscribed ticker:

```json
{
  "Type": "trade_update",
  "Data": {
    "ConnectionId": 1,
    "Ticker": "BTCUSDT",
    "Trades": [
      { "Price": 65123.50, "Size": 0.15, "Side": "Buy", "Time": "2026-03-16T12:00:01.234+00:00" },
      { "Price": 65123.00, "Size": 0.03, "Side": "Sell", "Time": "2026-03-16T12:00:01.235+00:00" }
    ]
  }
}
```

| Field | Type | Description |
|---|---|---|
| `ConnectionId` | integer | Connection this trade data belongs to |
| `Ticker` | string | Trading pair symbol |
| `Trades` | array | Array of trades in this update |
| `Trades[].Price` | decimal | Trade price |
| `Trades[].Size` | decimal | Trade size |
| `Trades[].Side` | string | `"Buy"` or `"Sell"` |
| `Trades[].Time` | string (ISO) | Trade timestamp |

**Order book snapshot** — sent once after subscribing, contains the full current order book state:

```json
{
  "Type": "orderbook_snapshot",
  "Data": {
    "ConnectionId": 1,
    "Ticker": "BTCUSDT",
    "Asks": [
      { "Price": 65124.00, "Size": 1.20, "Type": "Ask" },
      { "Price": 65125.00, "Size": 0.85, "Type": "Ask" }
    ],
    "Bids": [
      { "Price": 65123.00, "Size": 2.50, "Type": "Bid" },
      { "Price": 65122.00, "Size": 1.10, "Type": "Bid" }
    ],
    "BestAsk": { "Price": 65124.00, "Size": 1.20, "Type": "BestAsk" },
    "BestBid": { "Price": 65123.00, "Size": 2.50, "Type": "BestBid" }
  }
}
```

| Field | Type | Description |
|---|---|---|
| `ConnectionId` | integer | Connection this order book belongs to |
| `Ticker` | string | Trading pair symbol |
| `Asks` | array | Ask (sell) side of the order book, sorted by price ascending |
| `Bids` | array | Bid (buy) side of the order book, sorted by price descending |
| `BestAsk` | object | Best (lowest) ask price level |
| `BestBid` | object | Best (highest) bid price level |
| `Asks[]/Bids[].Price` | decimal | Price level |
| `Asks[]/Bids[].Size` | decimal | Total size at this price level |
| `Asks[]/Bids[].Type` | string | `"Ask"`, `"Bid"`, `"BestAsk"`, or `"BestBid"` |

**Order book update** — sent after the snapshot, contains incremental changes to the order book:

```json
{
  "Type": "orderbook_update",
  "Data": {
    "ConnectionId": 1,
    "Ticker": "BTCUSDT",
    "Updates": [
      { "Price": 65124.00, "Size": 0.90, "Type": "Ask" },
      { "Price": 65126.00, "Size": 0.50, "Type": "Ask" },
      { "Price": 65123.00, "Size": 2.80, "Type": "Bid" }
    ]
  }
}
```

| Field | Type | Description |
|---|---|---|
| `ConnectionId` | integer | Connection this update belongs to |
| `Ticker` | string | Trading pair symbol |
| `Updates` | array | Changed price levels. A size of `0` means the level was removed. |
| `Updates[].Price` | decimal | Price level |
| `Updates[].Size` | decimal | New total size at this level (0 = removed) |
| `Updates[].Type` | string | `"Ask"`, `"Bid"`, `"BestAsk"`, or `"BestBid"` |

#### Lifecycle

| Event | Behavior |
|---|---|
| Client connects | Session created. No updates flow until subscribe. |
| Subscribe (valid connection ID) | Server responds `subscribed`. Order, position, balance, and finres updates start flowing. |
| Subscribe (invalid connection ID) | Server responds `error`. No subscription created. |
| Subscribe (already subscribed) | Idempotent — responds `subscribed` again, no duplicate events. |
| Unsubscribe | Server responds `unsubscribed`. Updates stop for that connection. |
| Trade/orderbook subscribe (valid) | Server responds `trade_subscribed` / `orderbook_subscribed`. Market data starts flowing for that connection + ticker. |
| Trade/orderbook subscribe (invalid) | Server responds `error`. No subscription created. |
| Trade/orderbook subscribe (duplicate) | Idempotent — responds with confirmation, no duplicate events. |
| Trade/orderbook unsubscribe | Server responds `trade_unsubscribed` / `orderbook_unsubscribed`. Market data stops for that connection + ticker. |
| Client disconnects | All subscriptions (connection-level and market data) are cleaned up automatically. Exchange market data subscriptions are released when no more clients need them. |
| Multiple connections | A single client can subscribe to multiple connection IDs simultaneously. |
| Multiple tickers | A single client can subscribe to trades/order book for multiple tickers on the same or different connections. |
| Multiple clients | Multiple clients can subscribe to the same connection ID or ticker. Each receives its own copy of events. |

#### Error messages

| Condition | Error message |
|---|---|
| Subscribe to non-existent connection | `Connection {id} not found or not active` |
| Trade/orderbook subscribe with missing ticker | `Ticker is required for trade subscription` / `Ticker is required for order book subscription` |
| Trade/orderbook subscribe with invalid connection | `Connection {id} not found or not active` |
| Unknown message type | `Unknown message type: {type}` |
| Invalid JSON | `Invalid message format` |

---

## Ticker pattern format

The `TickerPattern` string follows the **TradingView-style** format:

```
EXCHANGE:SYMBOL.suffix
```

| Part       | Required | Description                                                                 |
|------------|----------|-----------------------------------------------------------------------------|
| `EXCHANGE` | no       | Exchange name (case-insensitive), e.g. `BINANCE`, `BYBIT`, `OKX`. Separated from the symbol by `:`. If omitted, the colon is also omitted. |
| `SYMBOL`   | yes      | Trading pair symbol, e.g. `BTCUSDT`, `ETH-USDT`. May contain letters, digits, `_`, `/`, `-`, `$`, and `:`. |
| `.suffix`  | no       | Single-letter market type suffix preceded by a dot.                         |

### Market type suffixes

| Suffix | Market type |
|--------|-------------|
| `.p`   | Futures     |
| `.m`   | Margin      |
| `.o`   | Options     |
| *(none)* | Spot      |

### Pattern examples

| Pattern                          | Exchange    | Symbol          | Market  |
|----------------------------------|-------------|-----------------|---------|
| `BINANCE:BTCUSDT.p`             | Binance     | BTCUSDT         | Futures |
| `BYBIT:ETHUSDT`                 | Bybit       | ETHUSDT         | Spot    |
| `OKX:BTC-USDT.m`                | OKX         | BTC-USDT        | Margin  |
| `BINANCE:BTCUSDT.o`             | Binance     | BTCUSDT         | Options |
| `HYPERLIQUID:BTCUSDT.p`         | HyperLiquid | BTCUSDT         | Futures |
| `HYPERLIQUID:cash:HOODUSDT0.p`  | HyperLiquid | cash:HOODUSDT0  | Futures |
| `BTCUSDT.p`                     | *(none)*    | BTCUSDT         | Futures |
| `BTCUSDT`                       | *(none)*    | BTCUSDT         | Spot    |

> **Note:** The exchange name in the pattern is matched case-insensitively against the exchange names listed in [Exchange values](#exchange-values) below.

### Symbol resolution

The symbol part of the pattern is flexible — MetaScalp automatically tries several normalizations to find a match:

| Step | Transformation | Example |
|------|---------------|---------|
| 1 | As-is (exact match) | `cash:HOODUSDT0` → `cash:HOODUSDT0` |
| 2 | Uppercase | `btcusdt` → `BTCUSDT` |
| 3 | Strip separators (`-`, `_`, `/`) | `ETH/USDT` → `ETHUSDT`, `SOL_USDT` → `SOLUSDT`, `BTC-USDT` → `BTCUSDT` |
| 4 | Strip separators + uppercase | `eth/usdt` → `ETHUSDT` |
| 5 | Remove `SWAP` suffix | `BTCUSDTSWAP` → `BTCUSDT` |
| 6 | Preserve prefix before `:`, uppercase the rest | `cash:hoodUSDT0` → `cash:HOODUSDT0` |

This means you can pass symbols in **any case** and with or without common separators (`-`, `_`, `/`) — the API will find the correct ticker.

---

## Exchange values

| Value | Exchange     | Notes |
|-------|-------------|-------|
| 2     | Binance     | |
| 3     | Gate        | |
| 5     | KuCoin      | |
| 6     | Bybit       | |
| 7     | Bitget      | |
| 8     | Mexc        | |
| 10    | Okx         | |
| 11    | BingX       | |
| 12    | HTX         | |
| 13    | BitMart     | |
| 14    | LBank       | |
| 15    | HyperLiquid | |
| 16    | UpBit       | |
| 17    | AsterDex    | |
| 18    | Moex        | |
| 19    | Lighter     | |

> **Note:** Values `1` and `9` are unused and reserved. Exchange identifiers are not sequential.

## MarketType values

| Value | Type           | Description |
|-------|----------------|-------------|
| 0     | Spot           | Spot trading |
| 1     | Futures        | Generic futures (use when the exchange does not distinguish subtypes) |
| 2     | UsdtFutures    | USDT-margined futures (e.g. Binance USDT-M) |
| 3     | CoinFutures    | Coin-margined futures (e.g. Binance COIN-M) |
| 4     | InverseFutures | Inverse futures contracts |
| 5     | UsdtPerpetual  | USDT perpetual swaps |
| 6     | UsdcPerpetual  | USDC perpetual swaps |
| 7     | Margin         | Margin (cross/isolated) |
| 8     | Options        | Options contracts |
| 9     | Stock          | Stock / equity markets |

**Which market type should I use?** If you are unsure, use the `TickerPattern` approach (Option A) instead — the `.p` suffix automatically resolves to the correct futures type for the given exchange. If you must use explicit fields, the most common choice for perpetual futures is `2` (UsdtFutures).

> **Note:** When the requested market type is not Spot and no exact connection match is found, MetaScalp falls back to any non-Spot connection on the same exchange.

## ClusterTimeFrame values

| Value | Period     |
|-------|------------|
| `S30` | 30 seconds |
| `M1`  | 1 minute   |
| `M5`  | 5 minutes  |
| `M10` | 10 minutes |
| `M15` | 15 minutes |
| `M30` | 30 minutes |
| `H1`  | 1 hour     |
| `D1`  | 1 day      |

Pass the string value (e.g. `M5`) as the `TimeFrame` query parameter.

---

## Integration examples

### JavaScript (browser)

```javascript
async function discoverMetaScalp() {
  for (let port = 17845; port <= 17855; port++) {
    try {
      const r = await fetch(`http://127.0.0.1:${port}/ping`);
      if (r.ok) {
        const data = await r.json();
        if (data.App === "MetaScalp") return port;
      }
    } catch {}
  }
  return null;
}

async function getConnections(port) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections`);
  return r.json();
}

async function getTickers(port, ConnectionId) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/tickers`);
  return r.json();
}

async function getBalance(port, ConnectionId) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/balance`);
  return r.json();
}

async function getOpenOrders(port, ConnectionId, ticker) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/orders?Ticker=${ticker}`);
  return r.json();
}

async function getPositions(port, ConnectionId) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/positions`);
  return r.json();
}

async function placeOrder(port, ConnectionId, order) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/orders`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(order)
  });
  return r.json();
}

async function cancelOrder(port, ConnectionId, ticker, OrderId, type = 0) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/orders/cancel`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ Ticker: ticker, OrderId, Type: type })
  });
  return r.json();
}

async function cancelAllOrders(port, ConnectionId, ticker) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/orders/cancel-all`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ Ticker: ticker })
  });
  return r.json();
}

async function getClusterSnapshot(port, ConnectionId, ticker, timeFrame, zoomIndex = 1) {
  const params = new URLSearchParams({ Ticker: ticker, TimeFrame: timeFrame, ZoomIndex: zoomIndex });
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/cluster-snapshot?${params}`);
  return r.json();
}

async function changeTickerByPattern(port, TickerPattern, binding) {
  const body = { TickerPattern };
  if (binding) body.Binding = binding;
  const r = await fetch(`http://127.0.0.1:${port}/api/change-ticker`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });
  return r.json();
}

// Usage
const port = await discoverMetaScalp();
if (port) {
  // Discover connections
  const { Connections } = await getConnections(port);
  const conn = Connections[0]; // pick first connection

  // Query data
  const tickers = await getTickers(port, conn.Id);
  const balance = await getBalance(port, conn.Id);
  const orders = await getOpenOrders(port, conn.Id, "BTCUSDT");
  const positions = await getPositions(port, conn.Id);

  // Place a limit buy order
  const result = await placeOrder(port, conn.Id, {
    Ticker: "BTCUSDT",
    Side: 1,
    Price: 65000.00,
    Size: 0.01,
    Type: 0
  });

  // Get cluster snapshot (5-minute timeframe, 2x zoom)
  const clusters = await getClusterSnapshot(port, conn.Id, "BTCUSDT", "M5", 2);

  // Switch ticker in UI
  await changeTickerByPattern(port, "BINANCE:BTCUSDT.p", "001");
}
```

### Python

```python
import requests

def discover_metascalp():
    for port in range(17845, 17856):
        try:
            r = requests.get(f"http://127.0.0.1:{port}/ping", timeout=0.5)
            if r.ok and r.json().get("App") == "MetaScalp":
                return port
        except requests.ConnectionError:
            continue
    return None

def get_connections(port):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections")
    return r.json()

def get_tickers(port, connection_id):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/tickers")
    return r.json()

def get_balance(port, connection_id):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/balance")
    return r.json()

def get_open_orders(port, connection_id, ticker):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orders",
                     params={"Ticker": ticker})
    return r.json()

def get_positions(port, connection_id):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/positions")
    return r.json()

def place_order(port, connection_id, ticker, side, price, size, order_type=0, reduce_only=False):
    payload = {
        "Ticker": ticker,
        "Side": side,
        "Price": price,
        "Size": size,
        "Type": order_type,
        "ReduceOnly": reduce_only
    }
    r = requests.post(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orders",
                      json=payload)
    return r.json()

def cancel_order(port, connection_id, ticker, order_id, order_type=0):
    payload = {"Ticker": ticker, "OrderId": order_id, "Type": order_type}
    r = requests.post(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orders/cancel",
                      json=payload)
    return r.json()

def cancel_all_orders(port, connection_id, ticker):
    payload = {"Ticker": ticker}
    r = requests.post(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orders/cancel-all",
                      json=payload)
    return r.json()

def get_cluster_snapshot(port, connection_id, ticker, time_frame, zoom_index=1):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/cluster-snapshot",
                     params={"Ticker": ticker, "TimeFrame": time_frame, "ZoomIndex": zoom_index})
    return r.json()

def change_ticker_by_pattern(port, ticker_pattern, binding=None):
    payload = {"TickerPattern": ticker_pattern}
    if binding:
        payload["Binding"] = binding
    r = requests.post(f"http://127.0.0.1:{port}/api/change-ticker", json=payload)
    return r.json()

def open_combo(port, ticker):
    r = requests.post(f"http://127.0.0.1:{port}/api/combo", json={"Ticker": ticker})
    return r.json()

# Usage
port = discover_metascalp()
if port:
    # Discover connections
    data = get_connections(port)
    conn = data["Connections"][0]  # pick first connection

    # Query data
    tickers = get_tickers(port, conn["Id"])
    balance = get_balance(port, conn["Id"])
    orders = get_open_orders(port, conn["Id"], "BTCUSDT")
    positions = get_positions(port, conn["Id"])

    # Place a limit buy order
    result = place_order(port, conn["Id"], "BTCUSDT", side=1, price=65000.00, size=0.01)

    # Get cluster snapshot (5-minute timeframe, 2x zoom)
    clusters = get_cluster_snapshot(port, conn["Id"], "BTCUSDT", "M5", zoom_index=2)

    # Switch ticker in UI
    change_ticker_by_pattern(port, "BINANCE:BTCUSDT.p", binding="001")
```

### JavaScript — WebSocket

```javascript
// Discover the WebSocket port
async function discoverMetaScalpSocket() {
  for (let port = 17845; port <= 17855; port++) {
    try {
      const ws = new WebSocket(`ws://127.0.0.1:${port}/`);
      const result = await new Promise((resolve) => {
        ws.onopen = () => { ws.close(); resolve(port); };
        ws.onerror = () => resolve(null);
        setTimeout(() => { try { ws.close(); } catch {} resolve(null); }, 800);
      });
      if (result) return result;
    } catch {}
  }
  return null;
}

// Connect and subscribe to real-time updates
const port = await discoverMetaScalpSocket();
const ws = new WebSocket(`ws://127.0.0.1:${port}/`);

ws.onopen = () => {
  console.log("Connected to MetaScalp socket");

  // Subscribe to connection ID 1 (orders, positions, balances, finres)
  ws.send(JSON.stringify({
    Type: "subscribe",
    Data: { ConnectionId: 1 }
  }));

  // Subscribe to trades for BTCUSDT on connection 1
  ws.send(JSON.stringify({
    Type: "trade_subscribe",
    Data: { ConnectionId: 1, Ticker: "BTCUSDT", ZoomIndex: 1 }
  }));

  // Subscribe to order book for BTCUSDT on connection 1
  ws.send(JSON.stringify({
    Type: "orderbook_subscribe",
    Data: { ConnectionId: 1, Ticker: "BTCUSDT" }
  }));
};

ws.onmessage = (event) => {
  const msg = JSON.parse(event.data);

  switch (msg.Type) {
    case "subscribed":
      console.log(`Subscribed to connection ${msg.Data.ConnectionId}`);
      break;
    case "trade_subscribed":
      console.log(`Subscribed to trades for ${msg.Data.Ticker} on connection ${msg.Data.ConnectionId}`);
      break;
    case "orderbook_subscribed":
      console.log(`Subscribed to order book for ${msg.Data.Ticker} on connection ${msg.Data.ConnectionId}`);
      break;
    case "order_update":
      console.log("Order update:", msg.Data);
      // { ConnectionId, OrderId, Ticker, Side, Type, Price, FilledPrice, Size, FilledSize, Fee, FeeCurrency, Status, Time }
      break;
    case "position_update":
      console.log("Position update:", msg.Data);
      // { ConnectionId, PositionId, Ticker, Side, Size, AvgPriceFix, AvgPriceDyn, Status }
      break;
    case "balance_update":
      console.log("Balance update:", msg.Data);
      // { ConnectionId, Balances: [{ Coin, Total, Free, Locked }] }
      break;
    case "finres_update":
      console.log("FinRes update:", msg.Data);
      // { ConnectionId, Finreses: [{ Currency, Result, Fee, Funds, Available, Blocked }] }
      break;
    case "trade_update":
      console.log("Trade update:", msg.Data);
      // { ConnectionId, Ticker, Trades: [{ Price, Size, Side, Time }] }
      break;
    case "orderbook_snapshot":
      console.log("Order book snapshot:", msg.Data);
      // { ConnectionId, Ticker, Asks: [...], Bids: [...], BestAsk, BestBid }
      break;
    case "orderbook_update":
      console.log("Order book update:", msg.Data);
      // { ConnectionId, Ticker, Updates: [{ Price, Size, Type }] }
      break;
    case "error":
      console.error("Socket error:", msg.Data.Error);
      break;
  }
};

ws.onclose = () => console.log("Disconnected");

// Later: unsubscribe from market data
ws.send(JSON.stringify({
  Type: "trade_unsubscribe",
  Data: { ConnectionId: 1, Ticker: "BTCUSDT" }
}));
ws.send(JSON.stringify({
  Type: "orderbook_unsubscribe",
  Data: { ConnectionId: 1, Ticker: "BTCUSDT" }
}));

// Unsubscribe from connection updates
ws.send(JSON.stringify({
  Type: "unsubscribe",
  Data: { ConnectionId: 1 }
}));
```

### Python — WebSocket

```python
import asyncio
import json
import websockets

async def discover_metascalp_socket():
    for port in range(17845, 17856):
        try:
            async with websockets.connect(f"ws://127.0.0.1:{port}/", open_timeout=1) as ws:
                await ws.close()
                return port
        except (ConnectionRefusedError, OSError, asyncio.TimeoutError):
            continue
    return None

async def listen_updates(connection_id, ticker="BTCUSDT"):
    port = await discover_metascalp_socket()
    if not port:
        print("MetaScalp socket server not found")
        return

    async with websockets.connect(f"ws://127.0.0.1:{port}/") as ws:
        # Subscribe to connection-level updates (orders, positions, balances, finres)
        await ws.send(json.dumps({
            "Type": "subscribe",
            "Data": {"ConnectionId": connection_id}
        }))

        # Subscribe to trades for a specific ticker
        await ws.send(json.dumps({
            "Type": "trade_subscribe",
            "Data": {"ConnectionId": connection_id, "Ticker": ticker, "ZoomIndex": 1}
        }))

        # Subscribe to order book for the same ticker
        await ws.send(json.dumps({
            "Type": "orderbook_subscribe",
            "Data": {"ConnectionId": connection_id, "Ticker": ticker}
        }))

        # Listen for updates
        async for raw in ws:
            msg = json.loads(raw)
            msg_type = msg["Type"]

            if msg_type == "subscribed":
                print(f"Subscribed to connection {msg['Data']['ConnectionId']}")
            elif msg_type == "trade_subscribed":
                print(f"Subscribed to trades for {msg['Data']['Ticker']}")
            elif msg_type == "orderbook_subscribed":
                print(f"Subscribed to order book for {msg['Data']['Ticker']}")
            elif msg_type == "order_update":
                print(f"Order: {msg['Data']}")
            elif msg_type == "position_update":
                print(f"Position: {msg['Data']}")
            elif msg_type == "balance_update":
                print(f"Balance: {msg['Data']}")
            elif msg_type == "finres_update":
                print(f"FinRes: {msg['Data']}")
            elif msg_type == "trade_update":
                print(f"Trades: {msg['Data']}")
                # { ConnectionId, Ticker, Trades: [{ Price, Size, Side, Time }] }
            elif msg_type == "orderbook_snapshot":
                print(f"Order book snapshot: {len(msg['Data'].get('Asks', []))} asks, {len(msg['Data'].get('Bids', []))} bids")
                # { ConnectionId, Ticker, Asks, Bids, BestAsk, BestBid }
            elif msg_type == "orderbook_update":
                print(f"Order book update: {len(msg['Data'].get('Updates', []))} levels changed")
                # { ConnectionId, Ticker, Updates: [{ Price, Size, Type }] }
            elif msg_type == "error":
                print(f"Error: {msg['Data']['Error']}")

asyncio.run(listen_updates(connection_id=1, ticker="BTCUSDT"))
```
