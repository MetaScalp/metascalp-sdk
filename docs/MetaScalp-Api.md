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
| `GET /api/connections/{id}/orderbook-snapshot?Ticker=` | One-shot fresh order book snapshot from the exchange REST endpoint |
| `GET /api/connections/{id}/cluster-snapshot` | Get cluster (volume profile) snapshot data |
| `GET /api/connections/{id}/signal-levels?Ticker=` | List signal levels for a ticker |
| `POST /api/connections/{id}/signal-levels` | Place a signal level |
| `DELETE /api/connections/{id}/signal-levels/{slId}` | Remove a single signal level |
| `DELETE /api/connections/{id}/signal-levels?Ticker=` | Remove all signal levels for a ticker |
| `DELETE /api/signal-levels/triggered` | Remove all triggered signal levels |
| `GET /api/connections/{id}/orderbook-settings?Ticker=` | Get order book settings for a ticker |
| `PUT /api/connections/{id}/orderbook-settings?Ticker=` | Update order book settings (partial) |

### WebSocket streaming

Connect via WebSocket to receive **real-time updates** for your exchange connections.

- Connect to `ws://127.0.0.1:{port}/` (same port as HTTP — scan ports `17845`–`17855`)
- **Connection-level subscriptions:** Send a `subscribe` message with a connection ID to receive order, position, balance, and finres updates
- **Market data subscriptions:** Send `trade_subscribe`, `orderbook_subscribe`, `mark_price_subscribe`, or `funding_subscribe` with a connection ID + ticker to receive trade, order book, mark price, or funding updates for a specific symbol
- **Notification subscriptions:** Send `notification_subscribe` to receive app-wide notification events (no connection ID required)
- **Signal level subscriptions:** Send `signal_level_subscribe` to receive signal level events (no connection ID required)
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
GET  /api/connections/{id}/orderbook-snapshot?Ticker=BTCUSDT  → fresh order book snapshot (single REST call)
GET  /api/connections/{id}/cluster-snapshot        → get cluster snapshot data
GET  /api/connections/{id}/signal-levels?Ticker=   → list signal levels
POST /api/connections/{id}/signal-levels           → place a signal level
DELETE /api/connections/{id}/signal-levels/{slId}  → remove a signal level
GET  /api/connections/{id}/orderbook-settings?Ticker= → get orderbook settings
PUT  /api/connections/{id}/orderbook-settings?Ticker= → update orderbook settings
```

### Typical WebSocket client flow

```
1. Connect
   ws = new WebSocket("ws://127.0.0.1:17845/")

2. Subscribe to a connection (orders, positions, balances, finres)
   → {"Type":"subscribe","Data":{"connectionId":1}}
   ← {"Type":"subscribed","Data":{"connectionId":1}}

3. Subscribe to market data for a specific ticker
   → {"Type":"trade_subscribe","Data":{"connectionId":1,"ticker":"BTCUSDT","zoomIndex":1}}
   ← {"Type":"trade_subscribed","Data":{"connectionId":1,"ticker":"BTCUSDT","zoomIndex":1}}
   → {"Type":"orderbook_subscribe","Data":{"connectionId":1,"ticker":"BTCUSDT","zoomIndex":0,"depthLevels":50,"depthPercent":0.5}}
   ← {"Type":"orderbook_subscribed","Data":{"connectionId":1,"ticker":"BTCUSDT","zoomIndex":0,"depthLevels":50,"depthPercent":0.5}}

4. Subscribe to notifications (app-wide, no connection ID needed)
   → {"Type":"notification_subscribe","Data":{}}
   ← {"Type":"notification_subscribed","Data":{}}
   ← {"Type":"notification_snapshot","Data":{"notifications":[...]}}

5. Subscribe to signal levels
   → {"Type":"signal_level_subscribe","Data":{}}
   ← {"Type":"signal_level_subscribed","Data":{}}
   ← {"Type":"signal_levels_snapshot","Data":{"signalLevels":[...]}}

6. Receive real-time updates
   ← {"Type":"order_update","Data":{"connectionId":1,"orderId":123,...}}
   ← {"Type":"position_update","Data":{"connectionId":1,...}}
   ← {"Type":"balance_update","Data":{"connectionId":1,"balances":[...]}}
   ← {"Type":"finres_update","Data":{"connectionId":1,"finreses":[...]}}
   ← {"Type":"trade_update","Data":{"connectionId":1,"ticker":"BTCUSDT","trades":[...]}}
   ← {"Type":"orderbook_snapshot","Data":{"connectionId":1,"ticker":"BTCUSDT","asks":[...],"bids":[...],...}}
   ← {"Type":"orderbook_update","Data":{"connectionId":1,"ticker":"BTCUSDT","updates":[...]}}
   ← {"Type":"notification_update","Data":{"notifications":[...]}}
   ← {"Type":"signal_level_placed","Data":{"id":1,"connectionId":1,"ticker":"BTCUSDT","price":95000,...}}
   ← {"Type":"signal_level_triggered","Data":{"id":1,"triggerTime":"2026-04-13T..."}}

7. Unsubscribe when done
   → {"Type":"signal_level_unsubscribe","Data":{}}
   ← {"Type":"signal_level_unsubscribed","Data":{}}
   → {"Type":"notification_unsubscribe","Data":{}}
   ← {"Type":"notification_unsubscribed","Data":{}}
   → {"Type":"trade_unsubscribe","Data":{"connectionId":1,"ticker":"BTCUSDT"}}
   ← {"Type":"trade_unsubscribed","Data":{"connectionId":1,"ticker":"BTCUSDT"}}
   → {"Type":"orderbook_unsubscribe","Data":{"connectionId":1,"ticker":"BTCUSDT"}}
   ← {"Type":"orderbook_unsubscribed","Data":{"connectionId":1,"ticker":"BTCUSDT"}}
   → {"Type":"unsubscribe","Data":{"connectionId":1}}
   ← {"Type":"unsubscribed","Data":{"connectionId":1}}
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

Returns all available trading pairs on a connection. By default returns cached data. Set `Refresh=true` to fetch fresh ticker data from the exchange.

```
GET http://127.0.0.1:{port}/api/connections/{connectionId}/tickers
GET http://127.0.0.1:{port}/api/connections/{connectionId}/tickers?Refresh=true
```

| Query Parameter | Type   | Required | Description |
|-----------------|--------|----------|-------------|
| `Refresh`       | boolean | no      | When `true`, fetches fresh data from the exchange instead of cache. Default: `false`. |

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
| `price`      | decimal | yes*     |         | For `Limit`: the limit price. For `Stop`/`StopLoss`/`TakeProfit`: the **trigger price**. Not required for `Market`. |
| `size`       | decimal | yes      |         | Order size (must be > 0) |
| `type`       | integer | no       | `0`     | `0` Limit, `1` Stop, `2` StopLoss, `3` TakeProfit, `4` Market |
| `reduceOnly` | boolean | no       | `false` | Close position only, do not open new |

> **Stop / StopLoss / TakeProfit orders.** Pass `price` as the trigger price (e.g. `64500` to trigger when BTC drops to $64,500).
> - `Stop` is a **stop-limit** order: once the trigger fires, MetaScalp submits a limit order at a price offset automatically by the same logic the UI uses (so the order fills reliably without you having to compute it).
> - `StopLoss` and `TakeProfit` are **stop-market** orders: once the trigger fires, the position is closed at market.
>
> If the connection's *Stop loss / Take profit* setting is `Application`, MetaScalp tracks `StopLoss` and `TakeProfit` triggers locally and only places the order on the exchange once the price is hit.

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

Returns `CancelledCount: 0` if there are no open orders for that ticker.

**Example:**
```bash
curl -X POST http://127.0.0.1:17845/api/connections/1/orders/cancel-all \
  -H "Content-Type: application/json" \
  -d '{"ticker": "BTCUSDT"}'
```

---

### Market data

#### Get Order Book Snapshot

Always fetches a **fresh** order book snapshot from the exchange REST endpoint — no cache lookup, no WebSocket subscription side effects. Intended as a one-shot complement to `orderbook_subscribe` with `fetchSnapshot=false`: subscribe to deltas cheaply, then call this endpoint when (and only when) you actually need to seed the book.

```
GET http://127.0.0.1:{port}/api/connections/{connectionId}/orderbook-snapshot?Ticker=BTCUSDT
```

**Query parameters:**

| Field          | Type    | Required | Default | Description |
|----------------|---------|----------|---------|-------------|
| `Ticker`       | string  | yes      |         | Trading pair symbol |
| `ZoomIndex`    | integer | no       | `0`     | Price aggregation factor. When `> 1`, levels are bucketed and sizes summed (same semantics as `orderbook_subscribe`). |
| `DepthLevels`  | integer | no       |         | Top-N price levels per side after zoom + percent. Must be ≥ 1 when specified. |
| `DepthPercent` | decimal | no       |         | Per-side band as a percentage anchored on best ask / best bid. Must be > 0 when specified. |

**Response `200 OK`:**
```json
{
  "connectionId": 1,
  "ticker": "BTCUSDT",
  "updateId": 12345678,
  "asks": [
    { "price": 65010.0, "size": 0.5, "type": "Ask" }
  ],
  "bids": [
    { "price": 65000.0, "size": 0.3, "type": "Bid" }
  ],
  "bestAsk": { "price": 65010.0, "size": 0.5, "type": "BestAsk" },
  "bestBid": { "price": 65000.0, "size": 0.3, "type": "BestBid" }
}
```

**`400 Bad Request`:**

| Condition                                  | Error message                                                  |
|--------------------------------------------|----------------------------------------------------------------|
| Invalid connection ID                      | `Invalid connection ID`                                        |
| Missing `Ticker`                           | `Query parameter 'Ticker' is required`                         |
| `DepthLevels < 1`                          | `DepthLevels must be >= 1 when specified`                      |
| `DepthPercent <= 0`                        | `DepthPercent must be > 0 when specified`                      |
| Ticker not on connection                   | `Ticker '{ticker}' not found on connection {id}`               |
| Empty snapshot from exchange               | `Exchange returned no snapshot`                                |

**`404 Not Found`** — connection not active:
```json
{ "error": "Connection {id} not found" }
```

**`501 Not Implemented`** — exchange does not expose a REST snapshot endpoint (e.g. Bybit USDT Perpetual, which only delivers snapshots over WebSocket):
```json
{ "error": "REST snapshot is not supported for this exchange" }
```

> **Rate limiting.** Each call performs **one** REST request to the exchange. The caller is responsible for not exceeding the exchange's rate limit when invoking this endpoint for many tickers in quick succession.

**Example:**
```bash
curl "http://127.0.0.1:17845/api/connections/1/orderbook-snapshot?Ticker=BTCUSDT&DepthLevels=50"
```

#### Cluster snapshot

Returns the current cluster (volume profile / footprint) data for a ticker on a connection. The snapshot contains up to 10 time columns, each holding bid/ask volumes at every price level.

```
GET http://127.0.0.1:{port}/api/connections/{connectionId}/cluster-snapshot?Ticker=BTCUSDT&TimeFrame=M5&ZoomIndex=1
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
  "ticker": "BTCUSDT",
  "timeFrame": "M5",
  "zoomIndex": 1,
  "priceIncrement": 0.01,
  "columns": [
    {
      "startTime": "2026-04-13T10:00:00+00:00",
      "asksSum": 123.45,
      "bidsSum": 678.90,
      "items": [
        { "price": 65000.02, "askSize": 0.8, "bidSize": 1.1 },
        { "price": 65000.01, "askSize": 1.5, "bidSize": 2.3 },
        { "price": 65000.00, "askSize": 0.3, "bidSize": 0.9 }
      ]
    }
  ]
}
```

- `columns` — up to 10 time-period columns (rolling window), ordered chronologically
- `items` — price levels within each column, ordered by price descending (highest first)
- `asksSum` / `bidsSum` — total ask/bid volume for the column
- `askSize` / `bidSize` — volume at each price level (ask = seller-initiated, bid = buyer-initiated)
- `priceIncrement` — the ticker's minimum price step (useful for interpreting ZoomIndex)

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

### Signal Levels

Signal levels are price alerts that trigger automatically when the market price crosses the specified threshold. Once triggered, the signal level is marked as triggered (not removed) and a notification is sent. Signal level updates are also pushed via WebSocket to subscribed clients.

All signal level endpoints (except "Remove all triggered") require a valid `{connectionId}` in the URL path, subject to the same [connection validation errors](#trading-operations) as trading endpoints.

#### Get Signal Levels

Returns all signal levels for a specific ticker on a connection.

```
GET http://127.0.0.1:{port}/api/connections/{connectionId}/signal-levels?Ticker=BTCUSDT
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
  "signalLevels": [
    {
      "id": 1,
      "connectionId": 1,
      "ticker": "BTCUSDT",
      "price": 95000.00,
      "isTriggered": false,
      "triggerTime": null,
      "triggerRule": "GreaterThanEqual"
    },
    {
      "id": 2,
      "connectionId": 1,
      "ticker": "BTCUSDT",
      "price": 90000.00,
      "isTriggered": true,
      "triggerTime": "2026-04-13T10:30:00+00:00",
      "triggerRule": "LessThanEqual"
    }
  ]
}
```

Signal level fields:

| Field         | Type         | Description |
|---------------|--------------|-------------|
| `id`          | integer      | Signal level ID |
| `connectionId`| integer      | Connection this signal level belongs to |
| `ticker`      | string       | Trading pair symbol |
| `price`       | decimal      | Price threshold |
| `isTriggered` | boolean      | Whether the signal has been triggered |
| `triggerTime` | string (ISO)?| When the signal was triggered (null if not triggered) |
| `triggerRule` | string       | `"LessThanEqual"` or `"GreaterThanEqual"` |

#### Place Signal Level

Places a new signal level at a specific price. The trigger rule is determined automatically from the current order book best ask. The order book must be active for this ticker — if no market data is available, the request will fail.

```
POST http://127.0.0.1:{port}/api/connections/{connectionId}/signal-levels
Content-Type: application/json
```

**Request body:**

| Field     | Type    | Required | Description |
|-----------|---------|----------|-------------|
| `ticker`  | string  | yes      | Trading pair symbol |
| `price`   | decimal | yes      | Price threshold (must be > 0) |

**Response `200 OK`:**
```json
{ "status": "ok" }
```

**`400 Bad Request`** — if no order book data is available for the ticker:
```json
{ "error": "No market data for 'BTCUSDT'. Subscribe to order book data for this ticker first." }
```

**Example:**
```bash
curl -X POST http://127.0.0.1:17845/api/connections/1/signal-levels \
  -H "Content-Type: application/json" \
  -d '{"ticker": "BTCUSDT", "price": 95000.00}'
```

#### Remove Signal Level

Removes a single signal level by ID.

```
DELETE http://127.0.0.1:{port}/api/connections/{connectionId}/signal-levels/{id}
```

**Response `200 OK`:**
```json
{ "status": "ok" }
```

#### Remove All Signal Levels (by ticker)

Removes all signal levels for a specific ticker on a connection.

```
DELETE http://127.0.0.1:{port}/api/connections/{connectionId}/signal-levels?Ticker=BTCUSDT
```

**Response `200 OK`:**
```json
{ "status": "ok" }
```

#### Remove All Triggered Signal Levels

Removes all triggered signal levels across all connections and tickers.

```
DELETE http://127.0.0.1:{port}/api/signal-levels/triggered
```

**Response `200 OK`:**
```json
{ "status": "ok" }
```

---

### Order Book Settings

Read and update order book display and trading settings for a specific ticker on a connection. The update endpoint uses partial semantics — only send the fields you want to change; omitted fields keep their current values.

All order book settings endpoints require a valid `{connectionId}` in the URL path, subject to the same [connection validation errors](#trading-operations) as trading endpoints.

#### Get Order Book Settings

Returns all order book settings for a specific ticker on a connection.

```
GET http://127.0.0.1:{port}/api/connections/{connectionId}/orderbook-settings?Ticker=BTCUSDT
```

| Query Parameter | Type   | Required | Description |
|-----------------|--------|----------|-------------|
| `Ticker`        | string | yes      | Trading pair symbol |

**Response `200 OK`:**
```json
{
  "connectionId": 1,
  "ticker": "BTCUSDT",
  "settings": {
    "notificationTradeHasBeenMade": true,
    "orderTypeDefault": 0,
    "defaultOrderCoin": 0.001,
    "defaultOrderUsd": 100.0,
    "orderSlippageCoin": 0.0,
    "orderSlippageUsd": 0.0,
    "closeByMarket": false,
    "amountBarFilledAt": 10000.0,
    "largeAmount": 10000.0,
    "largeAmount2": 20000.0,
    "amountBarFilter": 0.0,
    "amountBarFilledAtUsd": 30000.0,
    "largeAmountUsd": 20000.0,
    "largeAmountUsd2": 30000.0,
    "amountBarFilterUsd": 0.0,
    "notificationLargeAmountDetected": false,
    "notificationLargeAmount2Detected": false,
    "useLargeAmountDetectionArea": false,
    "largeAmountDetectionMinValue": 0.0,
    "largeAmountDetectionMaxValue": 0.0,
    "showRuler": "Percent",
    "zoomType": "Absolute",
    "autoZoom": false,
    "zoomPercent": 2.0,
    "rowHeight": 12.0,
    "slimLevelsFactor": 10.0,
    "basicLevelsFactor": 50.0,
    "notificationSignalLevelTriggered": true,
    "autoscroll": false,
    "fullDepth": true,
    "ticksLargeAmount": 0.0,
    "ticksLargeAmountUsd": 0.0,
    "sizeType": "Coin",
    "notificationTradeHasBeenMadeTicks": false,
    "showClusters": false,
    "clusterTimeFrame": "M1",
    "soundNotification": true
  }
}
```

Settings field reference:

**Trading**

| Field | Type | Description |
|-------|------|-------------|
| `notificationTradeHasBeenMade` | boolean | Notify when a trade is executed |
| `orderTypeDefault` | integer | Default order type (`0` Limit, `4` Market) |
| `defaultOrderCoin` | decimal | Default order size in base asset |
| `defaultOrderUsd` | decimal | Default order size in USD |
| `orderSlippageCoin` | decimal | Order slippage allowance in base asset |
| `orderSlippageUsd` | decimal | Order slippage allowance in USD |
| `closeByMarket` | boolean | Close positions using market orders |

**Order Book**

| Field | Type | Description |
|-------|------|-------------|
| `amountBarFilledAt` | decimal | Amount bar fill threshold (base asset) |
| `largeAmount` | decimal | Large amount highlight threshold (base asset) |
| `largeAmount2` | decimal | Second large amount highlight threshold (base asset) |
| `amountBarFilter` | decimal | Minimum amount to display in order book (base asset) |
| `amountBarFilledAtUsd` | decimal | Amount bar fill threshold (USD) |
| `largeAmountUsd` | decimal | Large amount highlight threshold (USD) |
| `largeAmountUsd2` | decimal | Second large amount highlight threshold (USD) |
| `amountBarFilterUsd` | decimal | Minimum amount to display in order book (USD) |
| `notificationLargeAmountDetected` | boolean | Notify when large amount is detected |
| `notificationLargeAmount2Detected` | boolean | Notify when second large amount is detected |
| `useLargeAmountDetectionArea` | boolean | Use price range for large amount detection |
| `largeAmountDetectionMinValue` | decimal | Minimum price for large amount detection area |
| `largeAmountDetectionMaxValue` | decimal | Maximum price for large amount detection area |
| `showRuler` | string | Ruler display mode: `"None"`, `"Points"`, `"Percent"`, `"PercentVolume"` |
| `zoomType` | string | Zoom type: `"Absolute"`, `"Percentage"` |
| `autoZoom` | boolean | Enable automatic zoom |
| `zoomPercent` | decimal | Zoom percentage value |
| `rowHeight` | decimal | Order book row height in pixels |
| `slimLevelsFactor` | decimal | Factor for slim price levels |
| `basicLevelsFactor` | decimal | Factor for basic price levels |
| `notificationSignalLevelTriggered` | boolean | Notify when a signal level is triggered |
| `autoscroll` | boolean | Auto-scroll order book to current price |
| `fullDepth` | boolean | Show full order book depth |
| `sizeType` | string | Size display type: `"Coin"`, `"Usd"` |
| `soundNotification` | boolean | Enable sound notifications |

**Ticks**

| Field | Type | Description |
|-------|------|-------------|
| `ticksLargeAmount` | decimal | Large tick highlight threshold (base asset) |
| `ticksLargeAmountUsd` | decimal | Large tick highlight threshold (USD) |
| `notificationTradeHasBeenMadeTicks` | boolean | Notify on large ticks |

**Clusters**

| Field | Type | Description |
|-------|------|-------------|
| `showClusters` | boolean | Show cluster (volume profile) data |
| `clusterTimeFrame` | string | Cluster timeframe: `"M1"`, `"M5"`, `"M15"`, `"M30"`, `"H1"`, `"H4"`, `"D1"` |

**Enum values:**

| Field | Valid values |
|-------|-------------|
| `showRuler` | `"None"`, `"Points"`, `"Percent"`, `"PercentVolume"` |
| `zoomType` | `"Absolute"`, `"Percentage"` |
| `sizeType` | `"Coin"`, `"Usd"` |
| `clusterTimeFrame` | `"M1"`, `"M5"`, `"M15"`, `"M30"`, `"H1"`, `"H4"`, `"D1"` |

#### Update Order Book Settings

Partial update — only send the fields you want to change. Omitted fields keep their current values.

```
PUT http://127.0.0.1:{port}/api/connections/{connectionId}/orderbook-settings?Ticker=BTCUSDT
Content-Type: application/json
```

| Query Parameter | Type   | Required | Description |
|-----------------|--------|----------|-------------|
| `Ticker`        | string | yes      | Trading pair symbol |

**Request body** (only include fields to update):
```json
{
  "largeAmountUsd": 50000,
  "rowHeight": 14,
  "autoscroll": true
}
```

**Response `200 OK`:** Same shape as GET — returns the full updated settings object.

**`400 Bad Request`:**

| Condition | Error message |
|-----------|---------------|
| Missing ticker | `Query parameter 'Ticker' is required` |
| Invalid enum value | `Invalid value 'X' for field 'ShowRuler'. Valid values: None, Points, Percent, PercentVolume` |

**`404 Not Found`:**

| Condition | Error message |
|-----------|---------------|
| No settings found | `No order book settings found for ticker 'X'` |

**Examples:**

```bash
# Get order book settings
curl "http://127.0.0.1:17845/api/connections/1/orderbook-settings?Ticker=BTCUSDT"

# Update order book settings (partial)
curl -X PUT http://127.0.0.1:17845/api/connections/1/orderbook-settings?Ticker=BTCUSDT \
  -H "Content-Type: application/json" \
  -d '{"largeAmountUsd": 50000, "rowHeight": 14, "autoscroll": true}'
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
| `subscribe` | `{ "connectionId": 123 }` | Subscribe to updates for a connection. Connection must be active in MetaScalp. Idempotent — re-subscribing is a no-op. |
| `unsubscribe` | `{ "connectionId": 123 }` | Stop receiving updates for a connection. Idempotent. |

**Market data subscriptions** — subscribe by connection ID + ticker to receive trade, order book, mark price, or funding updates for a specific symbol:

| Type | Data | Description |
|---|---|---|
| `trade_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT", "zoomIndex": 1 }` | Subscribe to real-time trade updates. When `zoomIndex` > 1, trades are aggregated by zoomed price level before sending. Re-subscribing updates zoomIndex. Connection and ticker must be valid. |
| `trade_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop receiving trade updates for that ticker. Idempotent. |
| `orderbook_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT", "zoomIndex": 0, "depthLevels": 50, "depthPercent": 0.5 }` | Subscribe to order book updates for a specific ticker on a connection. You will receive an initial snapshot followed by incremental updates. When `zoomIndex` > 1, price levels are aggregated into zoomed buckets. Re-subscribing replaces `zoomIndex` / `depthLevels` / `depthPercent` atomically. Connection must be active. Idempotent. Optional `depthLevels` (top-N per side, snapshot only) and `depthPercent` (per-side band on best ask / best bid, snapshot + updates) — see notes below. |
| `orderbook_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop receiving order book updates for that ticker. Idempotent. |
| `mark_price_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Subscribe to mark price updates for a specific ticker. No initial snapshot — only live updates. Connection must be active. Idempotent. |
| `mark_price_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop receiving mark price updates for that ticker. Idempotent. |
| `funding_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Subscribe to funding rate updates for a specific ticker. No initial snapshot — only live updates. Not all exchanges or markets emit funding events. Connection must be active. Idempotent. |
| `funding_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop receiving funding updates for that ticker. Idempotent. |

**`orderbook_subscribe` optional fields:**

- **`zoomIndex`** *(int, default `0`)* — price aggregation factor. When `> 1`, levels are bucketed into zoomed price slots and sizes summed. Affects both snapshot and updates.
- **`depthLevels`** *(int, optional, must be ≥ 1)* — keep at most N price levels per side (asks ascending by price, bids descending), applied **after** zoom and `depthPercent`. **Filters the snapshot only — incremental updates are unaffected**, so the client should maintain its own top-N view as updates arrive.
- **`depthPercent`** *(decimal, optional, must be > 0)* — per-side band as a percentage, anchored on **best ask** / **best bid** (NOT the mid):
  - Asks: keeps `price ≤ bestAsk × (1 + depthPercent / 100)`.
  - Bids: keeps `price ≥ bestBid × (1 − depthPercent / 100)`.
  - Applies to **both the snapshot and subsequent updates**. The band refreshes from the latest known best ask / best bid (snapshots, plus any `BestAsk` / `BestBid` entries on update events).
  - If a side's anchor is unknown (e.g. an empty side at snapshot time), that side is **not filtered** until an anchor arrives (degrades open).
- **`fetchSnapshot`** *(bool, default `true`)* — when `false` AND this subscriber is the first to ask for the ticker, the exchange REST snapshot fetch is skipped — only the WS delta feed is subscribed, and no `orderbook_snapshot` is emitted to this subscriber. Useful for mass-subscribing to 100+ tickers without hitting exchange REST rate limits. Seed state separately via `GET /api/connections/{id}/orderbook-snapshot` when needed. If a later subscriber requests a snapshot (or the UI joins), it is fetched lazily and delivered to all subscribers.
- `BestAsk` / `BestBid` payload fields are **never filtered** — they always represent best of book.

**Notification subscriptions** — subscribe to receive app-wide notification events (trades, signal levels, large amounts, screener):

| Type | Data | Description |
|---|---|---|
| `notification_subscribe` | `{}` | Subscribe to notifications. Receives an initial snapshot of recent notifications, then live updates. Idempotent. |
| `notification_unsubscribe` | `{}` | Stop receiving notification updates. Idempotent. |

**Signal level subscriptions** — subscribe to receive signal level events (placed, triggered, removed):

| Type | Data | Description |
|---|---|---|
| `signal_level_subscribe` | `{}` | Subscribe to signal level updates. Receives an initial snapshot of all signal levels, then live events. Idempotent. |
| `signal_level_unsubscribe` | `{}` | Stop receiving signal level updates. Idempotent. |

#### Messages you receive

##### Acknowledgements

| Type | Data | When |
|---|---|---|
| `subscribed` | `{ "connectionId": 123 }` | After successful connection subscribe |
| `unsubscribed` | `{ "connectionId": 123 }` | After successful connection unsubscribe |
| `trade_subscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT", "zoomIndex": 1 }` | After successful trade subscribe |
| `trade_unsubscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | After successful trade unsubscribe |
| `orderbook_subscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT", "zoomIndex": 0, "depthLevels": 50, "depthPercent": 0.5 }` | After successful order book subscribe. Echoes any non-null `depthLevels` / `depthPercent`. |
| `orderbook_unsubscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | After successful order book unsubscribe |
| `mark_price_subscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | After successful mark price subscribe |
| `mark_price_unsubscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | After successful mark price unsubscribe |
| `funding_subscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | After successful funding subscribe |
| `funding_unsubscribed` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | After successful funding unsubscribe |
| `notification_subscribed` | `{}` | After successful notification subscribe |
| `notification_unsubscribed` | `{}` | After successful notification unsubscribe |
| `signal_level_subscribed` | `{}` | After successful signal level subscribe |
| `signal_level_unsubscribed` | `{}` | After successful signal level unsubscribe |
| `error` | `{ "error": "..." }` | Invalid message, unknown type, bad connection ID, or missing ticker |

##### Real-time updates

These are pushed automatically after subscribing. You only receive updates for connection IDs you are subscribed to.

**Order update** — sent when an order is created, modified, filled, or cancelled:

```json
{
  "Type": "order_update",
  "Data": {
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
  "Type": "position_update",
  "Data": {
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
  "Type": "balance_update",
  "Data": {
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
  "Type": "finres_update",
  "Data": {
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
  "Type": "trade_update",
  "Data": {
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

> **Trade aggregation.** Trades are aggregated server-side using the order book's `AddingTicksForAPeriod` setting (the same value that drives the UI ticks section, default `200` ms; per-(connection, ticker)). Consecutive same-side trades that arrive within the window are merged into one entry — `size` is summed, `price` and `time` track the latest merged trade. A new entry is emitted when the side changes or the window expires. Set `AddingTicksForAPeriod = 0` in the order book settings to disable aggregation and receive the raw exchange stream. Changes to this setting are picked up live by active subscriptions.

**Order book snapshot** — sent once after subscribing, contains the full current order book state:

```json
{
  "Type": "orderbook_snapshot",
  "Data": {
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
| `BestAsk` | object | Best (lowest) ask price level |
| `BestBid` | object | Best (highest) bid price level |
| `asks[]/bids[].price` | decimal | Price level |
| `asks[]/bids[].size` | decimal | Total size at this price level |
| `asks[]/bids[].type` | string | `"Ask"`, `"Bid"`, `"BestAsk"`, or `"BestBid"` |

**Order book update** — sent after the snapshot, contains incremental changes to the order book:

```json
{
  "Type": "orderbook_update",
  "Data": {
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

**Mark price update** — sent when the mark price changes for a subscribed ticker (futures only):

```json
{
  "Type": "mark_price_update",
  "Data": {
    "connectionId": 1,
    "ticker": "BTCUSDT",
    "markPrice": 65123.5
  }
}
```

| Field | Type | Description |
|---|---|---|
| `connectionId` | integer | Connection this update belongs to |
| `ticker` | string | Trading pair symbol |
| `markPrice` | decimal | Current mark price |

> Mark price is only published by exchanges that expose a mark price stream (typically futures markets). On exchanges/markets that don't, no `mark_price_update` events arrive — the subscribe ack still succeeds.

**Funding update** — sent when funding rate or funding time changes for a subscribed ticker (perpetual futures only):

```json
{
  "Type": "funding_update",
  "Data": {
    "connectionId": 1,
    "ticker": "BTCUSDT",
    "fundingRate": 0.0001,
    "fundingTime": "2026-03-16T16:00:00+00:00"
  }
}
```

| Field | Type | Description |
|---|---|---|
| `connectionId` | integer | Connection this update belongs to |
| `ticker` | string | Trading pair symbol |
| `fundingRate` | decimal | Current funding rate (e.g. `0.0001` = 0.01%) |
| `fundingTime` | string (ISO 8601) | Next funding settlement timestamp |

> Funding is only published on perpetual futures connections; spot and dated futures connections will not emit `funding_update` events even after a successful subscribe.

**Notification snapshot** — sent once after `notification_subscribe`, contains recent notifications (up to 200):

```json
{
  "Type": "notification_snapshot",
  "Data": {
    "notifications": [
      {
        "type": "Trade",
        "exchange": "Binance",
        "exchangeId": 2,
        "exchangeLogo": "binance.png",
        "market": "USDT-M Futures",
        "marketType": "UsdtFutures",
        "ticker": "BTCUSDT",
        "price": 65000.0,
        "size": 0.5,
        "tabName": "Tab 1",
        "color": "#FF0000",
        "date": "2026-04-13T10:00:00+00:00"
      }
    ]
  }
}
```

**Notification update** — pushed when new notifications arrive (~1 second batches):

```json
{
  "Type": "notification_update",
  "Data": {
    "notifications": [
      { "type": "Trade", "exchange": "Bybit", "ticker": "ETHUSDT", "price": 3200.0, "size": 1.0, "date": "2026-04-13T10:05:00+00:00", ... }
    ]
  }
}
```

| Notification Type | Description |
|---|---|
| `Trade` | Position executed |
| `SignalLevel` | Signal level triggered |
| `BigOrderBookAmount` | Large order book amount detected |
| `BigOrderBookAmount2` | Large order book amount (2) detected |
| `BigTick` | Large volume tick detected |
| `ScreenerNewCoin` | New coin detected by screener |

| Notification Field | Type | Description |
|---|---|---|
| `type` | string | Notification type (see table above) |
| `exchange` | string | Exchange name |
| `exchangeId` | integer | Exchange ID |
| `exchangeLogo` | string | Exchange logo filename |
| `market` | string | Market name |
| `marketType` | string | Market type |
| `ticker` | string | Trading pair symbol |
| `price` | decimal | Price at the time of the event |
| `size` | decimal | Size/amount |
| `tabName` | string | Tab name where the event originated |
| `color` | string | Connection color |
| `date` | string (ISO) | When the event occurred |

**Signal levels snapshot** — sent once after `signal_level_subscribe`, contains all signal levels:

```json
{
  "Type": "signal_levels_snapshot",
  "Data": {
    "signalLevels": [
      {
        "id": 1,
        "connectionId": 1,
        "ticker": "BTCUSDT",
        "price": 95000.00,
        "isTriggered": false,
        "triggerTime": null,
        "triggerRule": "GreaterThanEqual"
      }
    ]
  }
}
```

**Signal level placed** — pushed when a new signal level is created:

```json
{
  "Type": "signal_level_placed",
  "Data": {
    "id": 1,
    "connectionId": 1,
    "ticker": "BTCUSDT",
    "price": 95000.00,
    "isTriggered": false,
    "triggerTime": null,
    "triggerRule": "GreaterThanEqual"
  }
}
```

**Signal level triggered** — pushed when a signal level is triggered by market price:

```json
{
  "Type": "signal_level_triggered",
  "Data": {
    "id": 1,
    "connectionId": 1,
    "ticker": "BTCUSDT",
    "price": 95000.00,
    "isTriggered": true,
    "triggerTime": "2026-04-13T10:30:00+00:00",
    "triggerRule": "GreaterThanEqual"
  }
}
```

**Signal level removed** — pushed when a single signal level is removed:

```json
{
  "Type": "signal_level_removed",
  "Data": {
    "id": 1,
    "connectionId": 1,
    "ticker": "BTCUSDT"
  }
}
```

**Signal levels removed all** — pushed when all signal levels for a ticker are cleared:

```json
{
  "Type": "signal_levels_removed_all",
  "Data": {
    "connectionId": 1,
    "ticker": "BTCUSDT"
  }
}
```

**Signal levels removed triggered** — pushed when all triggered signal levels are cleared:

```json
{
  "Type": "signal_levels_removed_triggered",
  "Data": {}
}
```

| Signal Level Field | Type | Description |
|---|---|---|
| `id` | integer | Signal level ID |
| `connectionId` | integer | Connection this signal level belongs to |
| `ticker` | string | Trading pair symbol |
| `price` | decimal | Price threshold |
| `isTriggered` | boolean | Whether the signal has been triggered |
| `triggerTime` | string (ISO)? | When triggered (null if not triggered) |
| `triggerRule` | string | `"LessThanEqual"` or `"GreaterThanEqual"` |

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
| Mark price / funding subscribe (valid) | Server responds `mark_price_subscribed` / `funding_subscribed`. Updates start flowing as the exchange publishes them (no initial snapshot). |
| Mark price / funding subscribe (invalid) | Server responds `error`. No subscription created. |
| Mark price / funding subscribe (duplicate) | Idempotent — responds with confirmation, no duplicate events. |
| Mark price / funding unsubscribe | Server responds `mark_price_unsubscribed` / `funding_unsubscribed`. Updates stop for that connection + ticker. |
| Notification subscribe | Server responds `notification_subscribed`. Sends snapshot of recent notifications, then live updates. |
| Notification unsubscribe | Server responds `notification_unsubscribed`. Notification updates stop. |
| Signal level subscribe | Server responds `signal_level_subscribed`. Sends snapshot of all signal levels, then live events (placed, triggered, removed). |
| Signal level unsubscribe | Server responds `signal_level_unsubscribed`. Signal level updates stop. |
| Client disconnects | All subscriptions (connection-level, market data, notifications, signal levels) are cleaned up automatically. Exchange market data subscriptions are released when no more clients need them. |
| Multiple connections | A single client can subscribe to multiple connection IDs simultaneously. |
| Multiple tickers | A single client can subscribe to trades/order book for multiple tickers on the same or different connections. |
| Multiple clients | Multiple clients can subscribe to the same connection ID or ticker. Each receives its own copy of events. |

#### Error messages

| Condition | Error message |
|---|---|
| Subscribe to non-existent connection | `Connection {id} not found or not active` |
| Trade/orderbook subscribe with missing ticker | `Ticker is required for trade subscription` / `Ticker is required for order book subscription` |
| Trade/orderbook subscribe with invalid connection | `Connection {id} not found or not active` |
| Mark price / funding subscribe with missing ticker | `Ticker is required for mark price subscription` / `Ticker is required for funding subscription` |
| Mark price / funding subscribe with invalid connection | `Connection {id} not found or not active` |
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
| 5 | Remove `SWAP` suffix | `bTCUSDTSWAP` → `BTCUSDT` |
| 6 | Preserve prefix before `:`, uppercase the rest | `cash:hoodUSDT0` → `cash:HOODUSDT0` |

This means you can pass symbols in **any case** and with or without common separators (`-`, `_`, `/`) — the API will find the correct ticker.

---

## Exchange values

| Value | Exchange     | Notes |
|-------|-------------|-------|
| 1     | Ftx         | Defunct exchange; retained for back-compat. |
| 2     | Binance     | |
| 3     | GateIo      | |
| 4     | Bybit       | Classic / Standard API. |
| 5     | KuCoin      | |
| 6     | BybitUta    | Bybit Unified Trading Account. |
| 7     | Bitget      | |
| 8     | Mexc        | |
| 9     | CommEx      | |
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
| 20    | XT          | |
| 21    | EdgeX       | |
| 22    | Bitunix     | |
| 23    | Ourbit      | |
| 24    | Whitebit    | |
| 25    | Blofin      | |
| 26    | Weex        | |
| 27    | TBank       | |

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

async function getTickers(port, connectionId, refresh = false) {
  const qs = refresh ? '?refresh=true' : '';
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/tickers${qs}`);
  return r.json();
}

async function getBalance(port, connectionId) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/balance`);
  return r.json();
}

async function getOpenOrders(port, connectionId, ticker) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/orders?ticker=${ticker}`);
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
    body: JSON.stringify({ ticker: ticker, orderId, type: type })
  });
  return r.json();
}

async function cancelAllOrders(port, connectionId, ticker) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/orders/cancel-all`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ticker: ticker })
  });
  return r.json();
}

async function getClusterSnapshot(port, connectionId, ticker, timeFrame, zoomIndex = 1) {
  const params = new URLSearchParams({ ticker: ticker, timeFrame: timeFrame, zoomIndex: zoomIndex });
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/cluster-snapshot?${params}`);
  return r.json();
}

async function getSignalLevels(port, connectionId, ticker) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/signal-levels?ticker=${ticker}`);
  return r.json();
}

async function placeSignalLevel(port, connectionId, ticker, price) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/signal-levels`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ticker: ticker, price: price })
  });
  return r.json();
}

async function removeSignalLevel(port, connectionId, signalLevelId) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/signal-levels/${signalLevelId}`, {
    method: "DELETE"
  });
  return r.json();
}

async function getOrderBookSettings(port, connectionId, ticker) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/orderbook-settings?ticker=${ticker}`);
  return r.json();
}

async function updateOrderBookSettings(port, connectionId, ticker, settings) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${connectionId}/orderbook-settings?ticker=${ticker}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(settings)
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

  // Get cluster snapshot (5-minute timeframe, 2x zoom)
  const clusters = await getClusterSnapshot(port, conn.id, "BTCUSDT", "M5", 2);

  // Signal levels
  const levels = await getSignalLevels(port, conn.id, "BTCUSDT");
  await placeSignalLevel(port, conn.id, "BTCUSDT", 95000.00);
  await removeSignalLevel(port, conn.id, 1);

  // Order book settings
  const obSettings = await getOrderBookSettings(port, conn.id, "BTCUSDT");
  await updateOrderBookSettings(port, conn.id, "BTCUSDT", { largeAmountUsd: 50000, rowHeight: 14 });

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
            if r.ok and r.json().get("App") == "metaScalp":
                return port
        except requests.ConnectionError:
            continue
    return None

def get_connections(port):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections")
    return r.json()

def get_tickers(port, connection_id, refresh=False):
    params = {"refresh": "true"} if refresh else {}
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/tickers", params=params)
    return r.json()

def get_balance(port, connection_id):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/balance")
    return r.json()

def get_open_orders(port, connection_id, ticker):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orders",
                     params={"ticker": ticker})
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

def get_cluster_snapshot(port, connection_id, ticker, time_frame, zoom_index=1):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/cluster-snapshot",
                     params={"ticker": ticker, "timeFrame": time_frame, "zoomIndex": zoom_index})
    return r.json()

def get_signal_levels(port, connection_id, ticker):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/signal-levels",
                     params={"ticker": ticker})
    return r.json()

def place_signal_level(port, connection_id, ticker, price):
    payload = {"ticker": ticker, "price": price}
    r = requests.post(f"http://127.0.0.1:{port}/api/connections/{connection_id}/signal-levels",
                      json=payload)
    return r.json()

def remove_signal_level(port, connection_id, signal_level_id):
    r = requests.delete(f"http://127.0.0.1:{port}/api/connections/{connection_id}/signal-levels/{signal_level_id}")
    return r.json()

def get_orderbook_settings(port, connection_id, ticker):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orderbook-settings",
                     params={"ticker": ticker})
    return r.json()

def update_orderbook_settings(port, connection_id, ticker, **settings):
    r = requests.put(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orderbook-settings",
                     params={"ticker": ticker}, json=settings)
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

    # Get cluster snapshot (5-minute timeframe, 2x zoom)
    clusters = get_cluster_snapshot(port, conn["id"], "BTCUSDT", "M5", zoom_index=2)

    # Signal levels
    levels = get_signal_levels(port, conn["id"], "BTCUSDT")
    place_signal_level(port, conn["id"], "BTCUSDT", price=95000.00)
    remove_signal_level(port, conn["id"], signal_level_id=1)

    # Order book settings
    ob_settings = get_orderbook_settings(port, conn["id"], "BTCUSDT")
    update_orderbook_settings(port, conn["id"], "BTCUSDT", largeAmountUsd=50000, rowHeight=14)

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
    Data: { connectionId: 1 }
  }));

  // Subscribe to trades for BTCUSDT on connection 1
  ws.send(JSON.stringify({
    Type: "trade_subscribe",
    Data: { connectionId: 1, ticker: "BTCUSDT", zoomIndex: 1 }
  }));

  // Subscribe to order book for BTCUSDT on connection 1.
  // zoomIndex aggregates prices; depthLevels caps the snapshot; depthPercent narrows
  // both snapshot and updates to a band around best ask / best bid.
  ws.send(JSON.stringify({
    Type: "orderbook_subscribe",
    Data: { connectionId: 1, ticker: "BTCUSDT", zoomIndex: 0, depthLevels: 50, depthPercent: 0.5 }
  }));

  // Subscribe to mark price + funding rate for BTCUSDT on connection 1 (futures only)
  ws.send(JSON.stringify({
    Type: "mark_price_subscribe",
    Data: { connectionId: 1, ticker: "BTCUSDT" }
  }));
  ws.send(JSON.stringify({
    Type: "funding_subscribe",
    Data: { connectionId: 1, ticker: "BTCUSDT" }
  }));
};

ws.onmessage = (event) => {
  const msg = JSON.parse(event.data);

  switch (msg.Type) {
    case "subscribed":
      console.log(`Subscribed to connection ${msg.Data.connectionId}`);
      break;
    case "trade_subscribed":
      console.log(`Subscribed to trades for ${msg.Data.ticker} on connection ${msg.Data.connectionId}`);
      break;
    case "orderbook_subscribed":
      console.log(`Subscribed to order book for ${msg.Data.ticker} on connection ${msg.Data.connectionId}`);
      break;
    case "order_update":
      console.log("Order update:", msg.Data);
      // { connectionId, orderId, ticker, side, type, price, filledPrice, size, filledSize, fee, feeCurrency, status, time }
      break;
    case "position_update":
      console.log("Position update:", msg.Data);
      // { connectionId, positionId, ticker, side, size, avgPriceFix, avgPriceDyn, status }
      break;
    case "balance_update":
      console.log("Balance update:", msg.Data);
      // { connectionId, balances: [{ coin, total, free, locked }] }
      break;
    case "finres_update":
      console.log("FinRes update:", msg.Data);
      // { connectionId, finreses: [{ currency, result, fee, funds, available, blocked }] }
      break;
    case "trade_update":
      console.log("Trade update:", msg.Data);
      // { connectionId, ticker, trades: [{ price, size, side, time }] }
      break;
    case "orderbook_snapshot":
      console.log("Order book snapshot:", msg.Data);
      // { connectionId, ticker, asks: [...], bids: [...], BestAsk, BestBid }
      break;
    case "orderbook_update":
      console.log("Order book update:", msg.Data);
      // { connectionId, ticker, updates: [{ price, size, type }] }
      break;
    case "mark_price_update":
      console.log("Mark price update:", msg.Data);
      // { connectionId, ticker, markPrice }
      break;
    case "funding_update":
      console.log("Funding update:", msg.Data);
      // { connectionId, ticker, fundingRate, fundingTime }
      break;
    case "notification_snapshot":
      console.log("Notification snapshot:", msg.Data);
      // { notifications: [{ type, exchange, ticker, price, size, date, ... }] }
      break;
    case "notification_update":
      console.log("New notifications:", msg.Data);
      // { notifications: [{ type, exchange, ticker, price, size, date, ... }] }
      break;
    case "signal_levels_snapshot":
      console.log("Signal levels snapshot:", msg.Data);
      // { signalLevels: [{ id, connectionId, ticker, price, isTriggered, triggerTime, triggerRule }] }
      break;
    case "signal_level_placed":
      console.log("Signal level placed:", msg.Data);
      break;
    case "signal_level_triggered":
      console.log("Signal level triggered:", msg.Data);
      break;
    case "signal_level_removed":
      console.log("Signal level removed:", msg.Data);
      break;
    case "error":
      console.error("Socket error:", msg.Data.error);
      break;
  }
};

ws.onclose = () => console.log("Disconnected");

// Later: unsubscribe from market data
ws.send(JSON.stringify({
  Type: "trade_unsubscribe",
  Data: { connectionId: 1, ticker: "BTCUSDT" }
}));
ws.send(JSON.stringify({
  Type: "orderbook_unsubscribe",
  Data: { connectionId: 1, ticker: "BTCUSDT" }
}));

// Unsubscribe from connection updates
ws.send(JSON.stringify({
  Type: "unsubscribe",
  Data: { connectionId: 1 }
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
            "Data": {"connectionId": connection_id}
        }))

        # Subscribe to trades for a specific ticker
        await ws.send(json.dumps({
            "Type": "trade_subscribe",
            "Data": {"connectionId": connection_id, "ticker": ticker, "zoomIndex": 1}
        }))

        # Subscribe to order book for the same ticker.
        # zoomIndex / depthLevels / depthPercent are optional — see orderbook_subscribe notes.
        await ws.send(json.dumps({
            "Type": "orderbook_subscribe",
            "Data": {"connectionId": connection_id, "ticker": ticker, "zoomIndex": 0, "depthLevels": 50, "depthPercent": 0.5}
        }))

        # Subscribe to mark price + funding rate (futures only — no events on spot)
        await ws.send(json.dumps({
            "Type": "mark_price_subscribe",
            "Data": {"connectionId": connection_id, "ticker": ticker}
        }))
        await ws.send(json.dumps({
            "Type": "funding_subscribe",
            "Data": {"connectionId": connection_id, "ticker": ticker}
        }))

        # Listen for updates
        async for raw in ws:
            msg = json.loads(raw)
            msg_type = msg["Type"]

            if msg_type == "subscribed":
                print(f"Subscribed to connection {msg['Data']['connectionId']}")
            elif msg_type == "trade_subscribed":
                print(f"Subscribed to trades for {msg['Data']['ticker']}")
            elif msg_type == "orderbook_subscribed":
                print(f"Subscribed to order book for {msg['Data']['ticker']}")
            elif msg_type == "order_update":
                print(f"Order: {msg['Data']}")
            elif msg_type == "position_update":
                print(f"Position: {msg['Data']}")
            elif msg_type == "balance_update":
                print(f"Balance: {msg['Data']}")
            elif msg_type == "finres_update":
                print(f"FinRes: {msg['Data']}")
            elif msg_type == "trade_update":
                print(f"trades: {msg['Data']}")
                # { connectionId, ticker, trades: [{ price, size, side, time }] }
            elif msg_type == "orderbook_snapshot":
                print(f"Order book snapshot: {len(msg['Data'].get('asks', []))} asks, {len(msg['Data'].get('bids', []))} bids")
                # { connectionId, ticker, asks, bids, BestAsk, BestBid }
            elif msg_type == "orderbook_update":
                print(f"Order book update: {len(msg['Data'].get('updates', []))} levels changed")
                # { connectionId, ticker, updates: [{ price, size, type }] }
            elif msg_type == "mark_price_update":
                print(f"Mark price: {msg['Data']['ticker']} = {msg['Data']['markPrice']}")
                # { connectionId, ticker, markPrice }
            elif msg_type == "funding_update":
                print(f"Funding: {msg['Data']['ticker']} rate={msg['Data']['fundingRate']} at {msg['Data']['fundingTime']}")
                # { connectionId, ticker, fundingRate, fundingTime }
            elif msg_type == "notification_snapshot":
                print(f"Notification snapshot: {len(msg['Data'].get('notifications', []))} notifications")
                # { notifications: [{ type, exchange, ticker, price, size, date, ... }] }
            elif msg_type == "notification_update":
                print(f"New notifications: {len(msg['Data'].get('notifications', []))} items")
                # { notifications: [{ type, exchange, ticker, price, size, date, ... }] }
            elif msg_type == "signal_levels_snapshot":
                print(f"Signal levels snapshot: {len(msg['Data'].get('signalLevels', []))} levels")
                # { signalLevels: [{ id, connectionId, ticker, price, isTriggered, triggerTime, triggerRule }] }
            elif msg_type == "signal_level_placed":
                print(f"Signal level placed: {msg['Data']}")
            elif msg_type == "signal_level_triggered":
                print(f"Signal level triggered: {msg['Data']}")
            elif msg_type == "signal_level_removed":
                print(f"Signal level removed: {msg['Data']}")
            elif msg_type == "error":
                print(f"error: {msg['Data']['error']}")

asyncio.run(listen_updates(connection_id=1, ticker="BTCUSDT"))
```
