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
```

### Typical WebSocket client flow

```
1. Connect
   ws = new WebSocket("ws://127.0.0.1:17845/")

2. Subscribe to a connection (orders, positions, balances, finres)
   → {"type":"subscribe","data":{"connectionId":1}}
   ← {"type":"subscribed","data":{"connectionId":1}}

3. Subscribe to market data for a specific ticker
   → {"type":"trade_subscribe","data":{"connectionId":1,"ticker":"BTCUSDT"}}
   ← {"type":"trade_subscribed","data":{"connectionId":1,"ticker":"BTCUSDT"}}
   → {"type":"orderbook_subscribe","data":{"connectionId":1,"ticker":"BTCUSDT"}}
   ← {"type":"orderbook_subscribed","data":{"connectionId":1,"ticker":"BTCUSDT"}}

4. Receive real-time updates
   ← {"type":"order_update","data":{"connectionId":1,"orderId":123,...}}
   ← {"type":"position_update","data":{"connectionId":1,...}}
   ← {"type":"balance_update","data":{"connectionId":1,"balances":[...]}}
   ← {"type":"finres_update","data":{"connectionId":1,"finreses":[...]}}
   ← {"type":"trade_update","data":{"connectionId":1,"ticker":"BTCUSDT","trades":[...]}}
   ← {"type":"orderbook_snapshot","data":{"connectionId":1,"ticker":"BTCUSDT","asks":[...],"bids":[...],...}}
   ← {"type":"orderbook_update","data":{"connectionId":1,"ticker":"BTCUSDT","updates":[...]}}

5. Unsubscribe when done
   → {"type":"trade_unsubscribe","data":{"connectionId":1,"ticker":"BTCUSDT"}}
   ← {"type":"trade_unsubscribed","data":{"connectionId":1,"ticker":"BTCUSDT"}}
   → {"type":"orderbook_unsubscribe","data":{"connectionId":1,"ticker":"BTCUSDT"}}
   ← {"type":"orderbook_unsubscribed","data":{"connectionId":1,"ticker":"BTCUSDT"}}
   → {"type":"unsubscribe","data":{"connectionId":1}}
   ← {"type":"unsubscribed","data":{"connectionId":1}}
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
{ "app": "MetaScalp", "version": "0.0.9" }
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

The endpoint accepts two request formats. Include **either** `tickerPattern` **or** the `exchange` + `market` + `ticker` fields.

**Option A — Ticker pattern**

| Field           | Type   | Required | Description                                                                 |
|-----------------|--------|----------|-----------------------------------------------------------------------------|
| `tickerPattern` | string | yes      | Pattern string (see [Ticker pattern format](#ticker-pattern-format) below). |
| `binding`       | string | no       | Named binding (`"001"`–`"500"`). Omit or send empty string to only notify the active window. |

**Option B — Explicit fields**

| Field      | Type    | Required | Description                                       |
|------------|---------|----------|---------------------------------------------------|
| `exchange` | integer | yes      | Exchange identifier (see [Exchange values](#exchange-values))   |
| `market`   | integer | yes      | Market type identifier (see [MarketType values](#markettype-values)) |
| `ticker`   | string  | yes      | Trading pair symbol, e.g. `"BTCUSDT"`             |
| `binding`  | string  | no       | Named binding (`"001"`–`"500"`). Omit or send empty string to only notify the active window. |

**Bindings**

A **binding** is a named group of linked panels inside MetaScalp (e.g. a chart, order book, and trade feed that should all show the same ticker). Bindings are numbered `"001"` through `"500"` and are configured by the user inside the MetaScalp UI. When you send a binding name with a request, all panels assigned to that binding will switch to the new ticker.

| `binding` value        | Active window notified | Named binding notified |
|------------------------|:----------------------:|:----------------------:|
| omitted / empty / null | yes                    | —                      |
| `"001"` … `"500"`      | yes                    | yes                    |

**Response**

**`200 OK`** — ticker changed successfully:
```json
{ "status": "ok" }
```

**`400 Bad Request`** — validation error:
```json
{ "error": "..." }
```

Possible error messages:

| Condition                          | Error message                                                         |
|------------------------------------|-----------------------------------------------------------------------|
| Missing fields                     | `Invalid request body. Provide 'tickerPattern' or 'exchange'+'market'+'ticker'.` |
| Invalid pattern format             | `Invalid ticker pattern: '{pattern}'`                                 |
| Binding name not found             | `Binding '{name}' not found. Available: {list}`                       |
| No connection for exchange + market | `No connection found for exchange {exchange} and market {market}`    |
| Ticker not available on connection | `Ticker '{ticker}' not found on connection {id}`                      |

**Examples**

Using ticker pattern:
```bash
curl -X POST http://127.0.0.1:17845/api/change-ticker \
  -H "Content-Type: application/json" \
  -d '{"tickerPattern": "BINANCE:BTCUSDT.p", "binding": "001"}'
```

Using explicit fields:
```bash
curl -X POST http://127.0.0.1:17845/api/change-ticker \
  -H "Content-Type: application/json" \
  -d '{"exchange": 2, "market": 2, "ticker": "BTCUSDT", "binding": "001"}'
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
| `ticker` | string | yes      | Trading pair symbol (not a pattern), e.g. `"BTCUSDT"`. The combo opens on the currently active exchange and market connection. |

**Response**

**`200 OK`:**
```json
{ "status": "ok" }
```

**`400 Bad Request`:**

| Condition              | Error message                                    |
|------------------------|--------------------------------------------------|
| Missing or empty `ticker` | `Invalid request body. 'ticker' is required.` |

**Example**

```bash
curl -X POST http://127.0.0.1:17845/api/combo \
  -H "Content-Type: application/json" \
  -d '{"ticker": "BTCUSDT"}'
```

---

### Connections

#### List Connections

Returns all currently active exchange connections. Use the `id` from the response to query orders, positions, balances, or to subscribe via WebSocket.

```
GET http://127.0.0.1:{port}/api/connections
```

**Response `200 OK`:**
```json
{
  "connections": [
    {
      "id": 1,
      "name": "Binance Futures",
      "exchange": "Binance",
      "exchangeId": 2,
      "market": "USDT Futures",
      "marketType": 2,
      "state": 2,
      "viewMode": false,
      "demoMode": false
    },
    {
      "id": 3,
      "name": "Bybit Spot",
      "exchange": "Bybit",
      "exchangeId": 6,
      "market": "Spot",
      "marketType": 0,
      "state": 2,
      "viewMode": false,
      "demoMode": false
    }
  ]
}
```

Connection fields:

| Field        | Type    | Description |
|--------------|---------|-------------|
| `id`         | integer | Connection ID — use this for all exchange operations |
| `name`       | string  | User-defined connection name |
| `exchange`   | string  | Exchange name (e.g. `"Binance"`, `"Bybit"`) |
| `exchangeId` | integer | Exchange identifier (see [Exchange values](#exchange-values)) |
| `market`     | string  | Market display name |
| `marketType` | integer | Market type (see [MarketType values](#markettype-values)) |
| `state`      | integer | Connection state: `0` Disconnected, `1` Connecting, `2` Connected, `3` Reconnecting, `4` Resetting |
| `viewMode`   | boolean | `true` = read-only, trading disabled |
| `demoMode`   | boolean | `true` = paper trading mode |

#### Get Connection

Returns details for a single connection.

```
GET http://127.0.0.1:{port}/api/connections/{connectionId}
```

**Response `200 OK`:** Same object as in the list above (single connection, not wrapped in array).

**`404 Not Found`:**
```json
{ "error": "Connection {connectionId} not found" }
```

---

### Trading Operations

All trading endpoints require a valid `{connectionId}` in the URL path. If the connection is not found or not active, the API returns an error before executing the operation.

Common errors for all exchange endpoints:

| Condition             | HTTP Status | Error message |
|-----------------------|-------------|---------------|
| Invalid connection ID | `400`       | `Invalid connection ID` |
| Connection not found  | `404`       | `Connection {id} not found` |
| Connection not active | `400`       | `Connection {id} is not active` |

#### Get Tickers

Returns all available trading pairs on a connection.

```
GET http://127.0.0.1:{port}/api/connections/{connectionId}/tickers
```

**Response `200 OK`:**
```json
{
  "connectionId": 1,
  "count": 354,
  "tickers": [
    {
      "name": "BTCUSDT",
      "baseAsset": "BTC",
      "quoteAsset": "USDT",
      "isTradingAllowed": true,
      "priceIncrement": 0.01,
      "sizeIncrement": 0.001,
      "minSize": 0.001,
      "maxSize": 1000.0
    }
  ]
}
```

Ticker fields:

| Field              | Type    | Description |
|--------------------|---------|-------------|
| `name`             | string  | Trading pair symbol |
| `baseAsset`        | string  | Base asset (e.g. `"BTC"`) |
| `quoteAsset`       | string  | Quote asset (e.g. `"USDT"`) |
| `isTradingAllowed` | boolean | Whether trading is enabled for this pair |
| `priceIncrement`   | decimal | Minimum price step |
| `sizeIncrement`    | decimal | Minimum size step |
| `minSize`          | decimal | Minimum order size |
| `maxSize`          | decimal? | Maximum order size (null if unlimited) |

#### Get Open Orders

Returns open orders for a specific ticker on a connection.

```
GET http://127.0.0.1:{port}/api/connections/{connectionId}/orders?Ticker=BTCUSDT
```

| Query Parameter | Type   | Required | Description |
|-----------------|--------|----------|-------------|
| `Ticker`        | string | yes      | Trading pair symbol |

**Response `200 OK`:**
```json
{
  "connectionId": 1,
  "ticker": "BTCUSDT",
  "count": 2,
  "orders": [
    {
      "id": 123456789,
      "ticker": "BTCUSDT",
      "clientId": "ms_limit_1234",
      "side": 1,
      "price": 65000.00,
      "size": 0.01,
      "filledSize": 0.0,
      "filledPrice": 0.0,
      "remainingSize": 0.01,
      "status": 1,
      "type": 0,
      "triggerPrice": null,
      "createDate": "2026-03-13T10:30:00+00:00"
    }
  ]
}
```

Order fields:

| Field           | Type         | Description |
|-----------------|--------------|-------------|
| `id`            | integer      | Exchange order ID |
| `ticker`        | string       | Trading pair |
| `clientId`      | string?      | Client-generated order ID |
| `side`          | integer      | `0` None, `1` Buy, `2` Sell |
| `price`         | decimal      | Order price |
| `size`          | decimal      | Order size |
| `filledSize`    | decimal      | Filled amount |
| `filledPrice`   | decimal      | Execution price (0 if not yet filled) |
| `remainingSize` | decimal      | Remaining amount |
| `status`        | integer      | `0` New, `1` Open, `2` Closed |
| `type`          | integer      | `0` Limit, `1` Stop, `2` StopLoss, `3` TakeProfit, `4` Market |
| `triggerPrice`  | decimal?     | Trigger price for stop/conditional orders |
| `createDate`    | string (ISO) | Order creation timestamp |

#### Get Open Positions

Returns all open positions on a connection (futures/margin markets).

```
GET http://127.0.0.1:{port}/api/connections/{connectionId}/positions
```

**Response `200 OK`:**
```json
{
  "connectionId": 1,
  "count": 1,
  "positions": [
    {
      "id": 1,
      "ticker": "BTCUSDT",
      "side": 1,
      "size": 0.05,
      "avgPrice": 64500.00,
      "marginMode": 0
    }
  ]
}
```

Position fields:

| Field        | Type    | Description |
|--------------|---------|-------------|
| `id`         | integer | Position ID |
| `ticker`     | string  | Trading pair |
| `side`       | integer | `1` Buy (Long), `2` Sell (Short) |
| `size`       | decimal | Position size |
| `avgPrice`   | decimal | Average entry price |
| `marginMode` | integer | `0` Cross, `1` Isolated |

#### Get Balance

Returns account balances for all assets on a connection.

```
GET http://127.0.0.1:{port}/api/connections/{connectionId}/balance
```

**Response `200 OK`:**
```json
{
  "connectionId": 1,
  "count": 3,
  "balances": [
    {
      "coin": "USDT",
      "total": 10000.00,
      "free": 8500.00,
      "locked": 1500.00
    }
  ]
}
```

Balance fields:

| Field    | Type    | Description |
|----------|---------|-------------|
| `coin`   | string  | Asset symbol |
| `total`  | decimal | Total balance |
| `free`   | decimal | Available balance |
| `locked` | decimal | Locked in open orders/positions |

#### Place Order

Places a new order on the exchange through a connection.

```
POST http://127.0.0.1:{port}/api/connections/{connectionId}/orders
Content-Type: application/json
```

**Request body:**

| Field        | Type    | Required | Default | Description |
|--------------|---------|----------|---------|-------------|
| `ticker`     | string  | yes      |         | Trading pair symbol |
| `side`       | integer | yes      |         | `1` Buy, `2` Sell |
| `price`      | decimal | yes*     |         | Order price (*required for non-market orders) |
| `size`       | decimal | yes      |         | Order size (must be > 0) |
| `type`       | integer | no       | `0`     | `0` Limit, `1` Stop, `2` StopLoss, `3` TakeProfit, `4` Market |
| `reduceOnly` | boolean | no       | `false` | Close position only, do not open new |

**Response `200 OK`:**
```json
{ "status": "ok", "clientId": "ms_limit_1234", "executionTimeMs": 123.45 }
```

The `clientId` is auto-generated by MetaScalp and can be used to track the order. The `executionTimeMs` field indicates how long the exchange request took to execute, in milliseconds.

**`400 Bad Request`:**

| Condition           | Error message |
|---------------------|---------------|
| Missing fields      | `Invalid request body. 'ticker', 'side', 'price', and 'size' are required.` |
| Size <= 0           | `Size must be greater than zero` |
| Price <= 0 (non-market) | `Price must be greater than zero for non-market orders` |
| Exchange rejected   | *(exchange-specific error message)* |

> **Note:** Error responses from exchange rejection also include `executionTimeMs`.

**Example:**
```bash
curl -X POST http://127.0.0.1:17845/api/connections/1/orders \
  -H "Content-Type: application/json" \
  -d '{"ticker": "BTCUSDT", "side": 1, "price": 65000.00, "size": 0.01, "type": 0}'
```

#### Cancel Order

Cancels an existing order on the exchange.

```
POST http://127.0.0.1:{port}/api/connections/{connectionId}/orders/cancel
Content-Type: application/json
```

**Request body:**

| Field     | Type    | Required | Default | Description |
|-----------|---------|----------|---------|-------------|
| `ticker`  | string  | yes      |         | Trading pair symbol |
| `orderId` | integer | yes      |         | Exchange order ID to cancel |
| `type`    | integer | no       | `0`     | Order type: `0` Limit, `1` Stop, etc. |

**Response `200 OK`:**
```json
{ "status": "ok" }
```

**Example:**
```bash
curl -X POST http://127.0.0.1:17845/api/connections/1/orders/cancel \
  -H "Content-Type: application/json" \
  -d '{"ticker": "BTCUSDT", "orderId": 123456789, "type": 0}'
```

#### Cancel All Orders

Cancels all open orders for a given ticker on the exchange.

```
POST http://127.0.0.1:{port}/api/connections/{connectionId}/orders/cancel-all
Content-Type: application/json
```

**Request body:**

| Field    | Type   | Required | Description |
|----------|--------|----------|-------------|
| `ticker` | string | yes      | Trading pair symbol |

**Response `200 OK`:**
```json
{ "status": "ok", "cancelledCount": 5 }
```

Returns `cancelledCount: 0` if there are no open orders for that ticker.

**Example:**
```bash
curl -X POST http://127.0.0.1:17845/api/connections/1/orders/cancel-all \
  -H "Content-Type: application/json" \
  -d '{"ticker": "BTCUSDT"}'
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
{ "type": "message_type", "data": { ... } }
```

#### Messages you send

**Connection-level subscriptions** — subscribe by connection ID to receive order, position, balance, and finres updates:

| Type | Data | Description |
|---|---|---|
| `subscribe` | `{ "connectionId": 123 }` | Subscribe to updates for a connection. Connection must be active in MetaScalp. Idempotent — re-subscribing is a no-op. |
| `unsubscribe` | `{ "connectionId": 123 }` | Stop receiving updates for a connection. Idempotent. |

**Market data subscriptions** — subscribe by connection ID + ticker to receive trade or order book updates for a specific symbol:

| Type | Data | Description |
|---|---|---|
| `trade_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Subscribe to real-time trade updates for a specific ticker on a connection. Connection must be active. Idempotent. |
| `trade_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop receiving trade updates for that ticker. Idempotent. |
| `orderbook_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Subscribe to order book updates for a specific ticker on a connection. You will receive an initial snapshot followed by incremental updates. Connection must be active. Idempotent. |
| `orderbook_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop receiving order book updates for that ticker. Idempotent. |

#### Messages you receive

##### Acknowledgements

| Type | Data | When |
|---|---|---|
| `subscribed` | `{ "connectionId": 123 }` | After successful connection subscribe |
| `unsubscribed` | `{ "connectionId": 123 }` | After successful connection unsubscribe |
| `trade_subscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | After successful trade subscribe |
| `trade_unsubscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | After successful trade unsubscribe |
| `orderbook_subscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | After successful order book subscribe |
| `orderbook_unsubscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | After successful order book unsubscribe |
| `error` | `{ "error": "..." }` | Invalid message, unknown type, bad connection ID, or missing ticker |

##### Real-time updates

These are pushed automatically after subscribing. You only receive updates for connection IDs you are subscribed to.

**Order update** — sent when an order is created, modified, filled, or cancelled:

```json
{
  "type": "order_update",
  "data": {
    "connectionId": 1,
    "orderId": 98765,
    "ticker": "BTCUSDT",
    "side": "Buy",
    "type": "Limit",
    "price": 65000.0,
    "filledPrice": 64980.5,
    "size": 0.01,
    "filledSize": 0.0,
    "fee": 0.0013,
    "feeCurrency": "USDT",
    "status": "New",
    "time": "2025-03-24T14:30:00+00:00"
  }
}
```

| Field | Type | Description |
|---|---|---|
| `connectionId` | integer | Connection this order belongs to |
| `orderId` | integer | Exchange order ID |
| `ticker` | string | Trading pair symbol |
| `side` | string | `"Buy"` or `"Sell"` |
| `type` | string | `"Limit"`, `"Stop"`, `"StopLoss"`, `"TakeProfit"`, `"Market"` |
| `price` | decimal | Order price |
| `filledPrice` | decimal | Average filled price |
| `size` | decimal | Order size |
| `filledSize` | decimal | Filled amount so far |
| `fee` | decimal | Trading fee charged |
| `feeCurrency` | string | Currency the fee is charged in (e.g. `"USDT"`) |
| `status` | string | `"New"`, `"Open"`, `"Closed"` |
| `time` | string | Order creation time (ISO 8601) |

**Position update** — sent when a position is opened, modified, or closed:

```json
{
  "type": "position_update",
  "data": {
    "connectionId": 1,
    "positionId": 4321,
    "ticker": "ETHUSDT",
    "side": "Buy",
    "size": 1.5,
    "avgPrice": 3200.00,
    "avgPriceFix": 3200.00,
    "avgPriceDyn": 3195.50,
    "status": "Open"
  }
}
```

| Field | Type | Description |
|---|---|---|
| `connectionId` | integer | Connection this position belongs to |
| `positionId` | integer | Position ID |
| `ticker` | string | Trading pair symbol |
| `side` | string | `"Buy"` (Long) or `"Sell"` (Short) |
| `size` | decimal | Position size |
| `avgPrice` | decimal | Average entry price (same as `avgPriceFix`, kept for backwards compatibility) |
| `avgPriceFix` | decimal | Fixed average price (weighted average of entry orders only) |
| `avgPriceDyn` | decimal | Dynamic average price (adjusted by realized exit profit) |
| `status` | string | `"New"`, `"Open"`, `"Closed"` |

**Balance update** — sent when account balances change (debounced ~500ms):

```json
{
  "type": "balance_update",
  "data": {
    "connectionId": 1,
    "balances": [
      { "coin": "USDT", "total": 10000.0, "free": 8500.0, "locked": 1500.0 },
      { "coin": "BTC", "total": 0.5, "free": 0.5, "locked": 0.0 }
    ]
  }
}
```

| Field | Type | Description |
|---|---|---|
| `connectionId` | integer | Connection this balance belongs to |
| `balances` | array | Array of asset balances |
| `balances[].coin` | string | Asset symbol |
| `balances[].total` | decimal | Total balance |
| `balances[].free` | decimal | Available balance |
| `balances[].locked` | decimal | Locked in open orders/positions |

**FinRes update** — sent when financial results are recalculated (after balance or order changes):

```json
{
  "type": "finres_update",
  "data": {
    "connectionId": 1,
    "finreses": [
      { "currency": "USDT", "result": 250.50, "fee": 12.30, "funds": 10000.0, "available": 8500.0, "blocked": 1500.0 },
      { "currency": "BTC", "result": 0.005, "fee": 0.0001, "funds": 0.5, "available": 0.5, "blocked": 0.0 }
    ]
  }
}
```

| Field | Type | Description |
|---|---|---|
| `connectionId` | integer | Connection this FinRes belongs to |
| `finreses` | array | Array of per-currency financial results |
| `finreses[].currency` | string | Asset symbol (e.g. `"USDT"`, `"BTC"`) |
| `finreses[].result` | decimal | Profit/loss since connection was initialized |
| `finreses[].fee` | decimal | Accumulated trading fees |
| `finreses[].funds` | decimal | Total balance |
| `finreses[].available` | decimal | Available (free) balance |
| `finreses[].blocked` | decimal | Locked in open orders/positions |

**Trade update** — sent when trades occur for a subscribed ticker:

```json
{
  "type": "trade_update",
  "data": {
    "connectionId": 1,
    "ticker": "BTCUSDT",
    "trades": [
      { "price": 65123.50, "size": 0.15, "side": "Buy", "time": "2026-03-16T12:00:01.234+00:00" },
      { "price": 65123.00, "size": 0.03, "side": "Sell", "time": "2026-03-16T12:00:01.235+00:00" }
    ]
  }
}
```

| Field | Type | Description |
|---|---|---|
| `connectionId` | integer | Connection this trade data belongs to |
| `ticker` | string | Trading pair symbol |
| `trades` | array | Array of trades in this update |
| `trades[].price` | decimal | Trade price |
| `trades[].size` | decimal | Trade size |
| `trades[].side` | string | `"Buy"` or `"Sell"` |
| `trades[].time` | string (ISO) | Trade timestamp |

**Order book snapshot** — sent once after subscribing, contains the full current order book state:

```json
{
  "type": "orderbook_snapshot",
  "data": {
    "connectionId": 1,
    "ticker": "BTCUSDT",
    "asks": [
      { "price": 65124.00, "size": 1.20, "type": "Ask" },
      { "price": 65125.00, "size": 0.85, "type": "Ask" }
    ],
    "bids": [
      { "price": 65123.00, "size": 2.50, "type": "Bid" },
      { "price": 65122.00, "size": 1.10, "type": "Bid" }
    ],
    "bestAsk": { "price": 65124.00, "size": 1.20, "type": "BestAsk" },
    "bestBid": { "price": 65123.00, "size": 2.50, "type": "BestBid" }
  }
}
```

| Field | Type | Description |
|---|---|---|
| `connectionId` | integer | Connection this order book belongs to |
| `ticker` | string | Trading pair symbol |
| `asks` | array | Ask (sell) side of the order book, sorted by price ascending |
| `bids` | array | Bid (buy) side of the order book, sorted by price descending |
| `bestAsk` | object | Best (lowest) ask price level |
| `bestBid` | object | Best (highest) bid price level |
| `asks[]/bids[].price` | decimal | Price level |
| `asks[]/bids[].size` | decimal | Total size at this price level |
| `asks[]/bids[].type` | string | `"Ask"`, `"Bid"`, `"BestAsk"`, or `"BestBid"` |

**Order book update** — sent after the snapshot, contains incremental changes to the order book:

```json
{
  "type": "orderbook_update",
  "data": {
    "connectionId": 1,
    "ticker": "BTCUSDT",
    "updates": [
      { "price": 65124.00, "size": 0.90, "type": "Ask" },
      { "price": 65126.00, "size": 0.50, "type": "Ask" },
      { "price": 65123.00, "size": 2.80, "type": "Bid" }
    ]
  }
}
```

| Field | Type | Description |
|---|---|---|
| `connectionId` | integer | Connection this update belongs to |
| `ticker` | string | Trading pair symbol |
| `updates` | array | Changed price levels. A size of `0` means the level was removed. |
| `updates[].price` | decimal | Price level |
| `updates[].size` | decimal | New total size at this level (0 = removed) |
| `updates[].type` | string | `"Ask"`, `"Bid"`, `"BestAsk"`, or `"BestBid"` |

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

The `tickerPattern` string follows the **TradingView-style** format:

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

**Which market type should I use?** If you are unsure, use the `tickerPattern` approach (Option A) instead — the `.p` suffix automatically resolves to the correct futures type for the given exchange. If you must use explicit fields, the most common choice for perpetual futures is `2` (UsdtFutures).

> **Note:** When the requested market type is not Spot and no exact connection match is found, MetaScalp falls back to any non-Spot connection on the same exchange.

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
        if (data.app === "MetaScalp") return port;
      }
    } catch {}
  }
  return null;
}

async function getConnections(port) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections`);
  return r.json();
}

async function getTickers(port, connectionId) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/tickers`);
  return r.json();
}

async function getBalance(port, connectionId) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/balance`);
  return r.json();
}

async function getOpenOrders(port, connectionId, ticker) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/orders?Ticker=${ticker}`);
  return r.json();
}

async function getPositions(port, connectionId) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/positions`);
  return r.json();
}

async function placeOrder(port, connectionId, order) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/orders`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(order)
  });
  return r.json();
}

async function cancelOrder(port, connectionId, ticker, orderId, type = 0) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/orders/cancel`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ticker, orderId, type })
  });
  return r.json();
}

async function cancelAllOrders(port, connectionId, ticker) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/orders/cancel-all`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ticker })
  });
  return r.json();
}

async function changeTickerByPattern(port, tickerPattern, binding) {
  const body = { tickerPattern };
  if (binding) body.binding = binding;
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
  const { connections } = await getConnections(port);
  const conn = connections[0]; // pick first connection

  // Query data
  const tickers = await getTickers(port, conn.id);
  const balance = await getBalance(port, conn.id);
  const orders = await getOpenOrders(port, conn.id, "BTCUSDT");
  const positions = await getPositions(port, conn.id);

  // Place a limit buy order
  const result = await placeOrder(port, conn.id, {
    ticker: "BTCUSDT",
    side: 1,
    price: 65000.00,
    size: 0.01,
    type: 0
  });

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
            if r.ok and r.json().get("app") == "MetaScalp":
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
        "ticker": ticker,
        "side": side,
        "price": price,
        "size": size,
        "type": order_type,
        "reduceOnly": reduce_only
    }
    r = requests.post(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orders",
                      json=payload)
    return r.json()

def cancel_order(port, connection_id, ticker, order_id, order_type=0):
    payload = {"ticker": ticker, "orderId": order_id, "type": order_type}
    r = requests.post(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orders/cancel",
                      json=payload)
    return r.json()

def cancel_all_orders(port, connection_id, ticker):
    payload = {"ticker": ticker}
    r = requests.post(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orders/cancel-all",
                      json=payload)
    return r.json()

def change_ticker_by_pattern(port, ticker_pattern, binding=None):
    payload = {"tickerPattern": ticker_pattern}
    if binding:
        payload["binding"] = binding
    r = requests.post(f"http://127.0.0.1:{port}/api/change-ticker", json=payload)
    return r.json()

def open_combo(port, ticker):
    r = requests.post(f"http://127.0.0.1:{port}/api/combo", json={"ticker": ticker})
    return r.json()

# Usage
port = discover_metascalp()
if port:
    # Discover connections
    data = get_connections(port)
    conn = data["connections"][0]  # pick first connection

    # Query data
    tickers = get_tickers(port, conn["id"])
    balance = get_balance(port, conn["id"])
    orders = get_open_orders(port, conn["id"], "BTCUSDT")
    positions = get_positions(port, conn["id"])

    # Place a limit buy order
    result = place_order(port, conn["id"], "BTCUSDT", side=1, price=65000.00, size=0.01)

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
    type: "subscribe",
    data: { connectionId: 1 }
  }));

  // Subscribe to trades for BTCUSDT on connection 1
  ws.send(JSON.stringify({
    type: "trade_subscribe",
    data: { connectionId: 1, ticker: "BTCUSDT" }
  }));

  // Subscribe to order book for BTCUSDT on connection 1
  ws.send(JSON.stringify({
    type: "orderbook_subscribe",
    data: { connectionId: 1, ticker: "BTCUSDT" }
  }));
};

ws.onmessage = (event) => {
  const msg = JSON.parse(event.data);

  switch (msg.type) {
    case "subscribed":
      console.log(`Subscribed to connection ${msg.data.connectionId}`);
      break;
    case "trade_subscribed":
      console.log(`Subscribed to trades for ${msg.data.ticker} on connection ${msg.data.connectionId}`);
      break;
    case "orderbook_subscribed":
      console.log(`Subscribed to order book for ${msg.data.ticker} on connection ${msg.data.connectionId}`);
      break;
    case "order_update":
      console.log("Order update:", msg.data);
      // { connectionId, orderId, ticker, side, type, price, filledPrice, size, filledSize, fee, feeCurrency, status, time }
      break;
    case "position_update":
      console.log("Position update:", msg.data);
      // { connectionId, positionId, ticker, side, size, avgPriceFix, avgPriceDyn, status }
      break;
    case "balance_update":
      console.log("Balance update:", msg.data);
      // { connectionId, balances: [{ coin, total, free, locked }] }
      break;
    case "finres_update":
      console.log("FinRes update:", msg.data);
      // { connectionId, finreses: [{ currency, result, fee, funds, available, blocked }] }
      break;
    case "trade_update":
      console.log("Trade update:", msg.data);
      // { connectionId, ticker, trades: [{ price, size, side, time }] }
      break;
    case "orderbook_snapshot":
      console.log("Order book snapshot:", msg.data);
      // { connectionId, ticker, asks: [...], bids: [...], bestAsk, bestBid }
      break;
    case "orderbook_update":
      console.log("Order book update:", msg.data);
      // { connectionId, ticker, updates: [{ price, size, type }] }
      break;
    case "error":
      console.error("Socket error:", msg.data.error);
      break;
  }
};

ws.onclose = () => console.log("Disconnected");

// Later: unsubscribe from market data
ws.send(JSON.stringify({
  type: "trade_unsubscribe",
  data: { connectionId: 1, ticker: "BTCUSDT" }
}));
ws.send(JSON.stringify({
  type: "orderbook_unsubscribe",
  data: { connectionId: 1, ticker: "BTCUSDT" }
}));

// Unsubscribe from connection updates
ws.send(JSON.stringify({
  type: "unsubscribe",
  data: { connectionId: 1 }
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
            "type": "subscribe",
            "data": {"connectionId": connection_id}
        }))

        # Subscribe to trades for a specific ticker
        await ws.send(json.dumps({
            "type": "trade_subscribe",
            "data": {"connectionId": connection_id, "ticker": ticker}
        }))

        # Subscribe to order book for the same ticker
        await ws.send(json.dumps({
            "type": "orderbook_subscribe",
            "data": {"connectionId": connection_id, "ticker": ticker}
        }))

        # Listen for updates
        async for raw in ws:
            msg = json.loads(raw)
            msg_type = msg["type"]

            if msg_type == "subscribed":
                print(f"Subscribed to connection {msg['data']['connectionId']}")
            elif msg_type == "trade_subscribed":
                print(f"Subscribed to trades for {msg['data']['ticker']}")
            elif msg_type == "orderbook_subscribed":
                print(f"Subscribed to order book for {msg['data']['ticker']}")
            elif msg_type == "order_update":
                print(f"Order: {msg['data']}")
            elif msg_type == "position_update":
                print(f"Position: {msg['data']}")
            elif msg_type == "balance_update":
                print(f"Balance: {msg['data']}")
            elif msg_type == "finres_update":
                print(f"FinRes: {msg['data']}")
            elif msg_type == "trade_update":
                print(f"Trades: {msg['data']}")
                # { connectionId, ticker, trades: [{ price, size, side, time }] }
            elif msg_type == "orderbook_snapshot":
                print(f"Order book snapshot: {len(msg['data'].get('asks', []))} asks, {len(msg['data'].get('bids', []))} bids")
                # { connectionId, ticker, asks, bids, bestAsk, bestBid }
            elif msg_type == "orderbook_update":
                print(f"Order book update: {len(msg['data'].get('updates', []))} levels changed")
                # { connectionId, ticker, updates: [{ price, size, type }] }
            elif msg_type == "error":
                print(f"Error: {msg['data']['error']}")

asyncio.run(listen_updates(connection_id=1, ticker="BTCUSDT"))
```
