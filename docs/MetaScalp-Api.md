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
| `POST /api/combo` | Open a combo layout for a ticker (`Activate: false` opens it in the background) |
| `POST /api/close-last-tab` | Close the main window's last tab (test surface; refuses to close the only tab) |
| `POST /api/ui/tabs/{tabId}/close` | Close ONE tab by id (ids from `GET /api/ui/state`; refuses a window's only tab) |
| `POST /api/ui/tabs/{tabId}/activate` | Switch to ONE tab by id |
| `GET /api/connections` | List all active exchange connections |
| `GET /api/connections/{id}/...` | Query tickers, orders, positions, balances for a connection |
| `POST /api/connections/{id}/orders` | Place an order on a connection |
| `POST /api/connections/{id}/orders/cancel` | Cancel a single order |
| `POST /api/connections/{id}/orders/cancel-all` | Cancel all orders for a ticker |
| `GET /api/connections/{id}/orderbook-snapshot?Ticker=` | One-shot fresh order book snapshot from the exchange REST endpoint |
| `GET /api/link-groups/{groupId}/orderbook-snapshots` | Read-only order books of every panel currently in link group N (live membership; per-entry `reason` on partial failure) |
| `GET /api/connections/{id}/cluster-snapshot` | Get cluster (volume profile) snapshot data |
| `GET /api/connections/{id}/leverage-limits?Ticker=` | Max leverage and max position size, live from the venue's own risk-limit tiers |
| `GET /api/connections/{id}/signal-levels?Ticker=` | List signal levels for a ticker |
| `POST /api/connections/{id}/signal-levels` | Place a signal level |
| `DELETE /api/connections/{id}/signal-levels/{slId}` | Remove a single signal level |
| `DELETE /api/connections/{id}/signal-levels?Ticker=` | Remove all signal levels for a ticker |
| `DELETE /api/signal-levels/triggered` | Remove all triggered signal levels |
| `PUT /api/connections/{id}/signal-levels/{slId}` | Modify an existing signal level in place (partial) |
| `GET /api/connections/{id}/user-levels?Ticker=` | List user (plain) levels for a ticker |
| `POST /api/connections/{id}/user-levels` | Place a user level |
| `PUT /api/connections/{id}/user-levels/{ulId}` | Modify an existing user level in place (partial) |
| `DELETE /api/connections/{id}/user-levels/{ulId}` | Remove a single user level |
| `DELETE /api/connections/{id}/user-levels?Ticker=` | Remove all user levels for a ticker |
| `GET /api/connections/{id}/annotations?Ticker=` | Read all three chart-annotation lists for a ticker |
| `PUT /api/connections/{id}/annotations/{type}?Ticker=` | Replace the whole annotation list for one type |
| `POST /api/connections/{id}/annotations/{type}?Ticker=` | Append one annotation of a type |
| `DELETE /api/connections/{id}/annotations/{type}/{index}?Ticker=` | Remove one annotation by zero-based index |
| `DELETE /api/connections/{id}/annotations?Ticker=` | Clear all three annotation lists for a ticker |
| `POST /api/notifications` | Inject a custom row into the notification feed |
| `GET /api/ui/state` | Full read-only inventory of the open UI (windows, tabs, documents) |
| `GET /api/ui/windows/{windowId}` | The same inventory object for one window |
| `POST /api/ui/windows` | Open a terminal window of a given type at an explicit place/size/monitor |
| `POST /api/ui/windows/{windowId}/close` | Close one terminal window through the app's own close path |
| `POST /api/ui/windows/{windowId}/activate` | Bring one terminal window to the front |
| `PUT /api/ui/documents/{externalId}/link-number` | Set one panel's link (binding-group) number |
| `PUT /api/ui/documents/{externalId}/ticker` | Re-point one addressed panel to a different market |
| `GET /api/connections/{id}/orderbook-settings?Ticker=` | Get order book settings for a ticker |
| `PUT /api/connections/{id}/orderbook-settings?Ticker=` | Update order book settings (partial) |
| `GET /api/screener/templates` | List saved screener templates (the synthetic Default, id `-1`, is always first) |
| `GET /api/screener/templates/{templateId}` | Read one template's resolved configuration |
| `POST /api/screener/templates` | Create a template (name + optional settings blob) |
| `PUT /api/screener/templates/{templateId}` | Update a template (partial) |
| `DELETE /api/screener/templates/{templateId}` | Delete a template |
| `GET /api/screener/templates/{templateId}/data` | One headless snapshot of a template's full row set — no screener window is opened |

### WebSocket streaming

Connect via WebSocket to receive **real-time updates** for your exchange connections.

- Connect to `ws://127.0.0.1:{port}/` (same port as HTTP — scan ports `17845`–`17855`)
- **Connection-level subscriptions:** Send a `subscribe` message with a connection ID to receive order, position, balance, and finres updates
- **Market data subscriptions:** Send `trade_subscribe`, `orderbook_subscribe`, `mark_price_subscribe`, `index_price_subscribe`, or `funding_subscribe` with a connection ID + ticker to receive trade, order book, mark price, index price, or funding updates for a specific symbol
- **Notification subscriptions:** Send `notification_subscribe` to receive app-wide notification events (no connection ID required)
- **Signal level subscriptions:** Send `signal_level_subscribe` to receive signal level events (no connection ID required)
- **User level subscriptions:** Send `user_level_subscribe` to receive user (plain) level lifecycle events (no connection ID required)
- **Chart annotation subscriptions:** Send `annotation_subscribe` with a connection ID + ticker to receive a one-shot `annotations_snapshot` of the current shapes
- **UI change subscriptions:** Send `ui_subscribe` to receive a `ui_snapshot` of the open UI followed by `ui_update` events (no connection ID required)
- **MetaBroker analytics subscriptions:** Send `density_map_subscribe`, `large_trades_subscribe`, or `liquidations_subscribe` to stream the MetaBroker density map / large trades / liquidations feeds — the same data the terminal's analytics windows show (no connection ID required; no MetaBroker login needed)
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
PUT  /api/connections/{id}/signal-levels/{slId}    → modify a signal level in place
GET  /api/connections/{id}/user-levels?Ticker=     → list user levels
POST /api/connections/{id}/user-levels             → place a user level
GET  /api/connections/{id}/annotations?Ticker=     → read all chart annotations
PUT  /api/connections/{id}/annotations/lines?Ticker= → replace the trend-line list
POST /api/notifications                            → inject a custom notification
GET  /api/ui/state                                 → inventory of the open UI
PUT  /api/ui/documents/{externalId}/ticker         → re-point one panel
GET  /api/connections/{id}/orderbook-settings?Ticker= → get orderbook settings
PUT  /api/connections/{id}/orderbook-settings?Ticker= → update orderbook settings
```

### Typical WebSocket client flow

```
1. Connect
   ws = new WebSocket("ws://127.0.0.1:17845/")

2. Subscribe to a connection (orders, positions, balances, finres)
   → {"Type":"subscribe","Data":{"connectionId":1}}
   ← {"Type":"subscribed","Data":{"ConnectionId":1}}

3. Subscribe to market data for a specific ticker
   → {"Type":"trade_subscribe","Data":{"connectionId":1,"ticker":"BTCUSDT","zoomIndex":1}}
   ← {"Type":"trade_subscribed","Data":{"ConnectionId":1,"Ticker":"BTCUSDT","ZoomIndex":1}}
   → {"Type":"orderbook_subscribe","Data":{"connectionId":1,"ticker":"BTCUSDT","zoomIndex":0,"depthLevels":50,"depthPercent":0.5}}
   ← {"Type":"orderbook_subscribed","Data":{"ConnectionId":1,"Ticker":"BTCUSDT","ZoomIndex":0,"DepthLevels":50,"DepthPercent":0.5}}

4. Subscribe to notifications (app-wide, no connection ID needed)
   → {"Type":"notification_subscribe","Data":{}}
   ← {"Type":"notification_subscribed","Data":{}}
   ← {"Type":"notification_snapshot","Data":{"notifications":[...]}}

5. Subscribe to signal levels, user levels, annotations, UI events
   → {"Type":"signal_level_subscribe","Data":{}}
   ← {"Type":"signal_level_subscribed","Data":{}}
   ← {"Type":"signal_levels_snapshot","Data":{"signalLevels":[...]}}
   → {"Type":"user_level_subscribe","Data":{}}
   ← {"Type":"user_level_subscribed","Data":{}}
   ← {"Type":"user_levels_snapshot","Data":{"userLevels":[...]}}
   → {"Type":"annotation_subscribe","Data":{"connectionId":1,"ticker":"BTCUSDT"}}
   ← {"Type":"annotation_subscribed","Data":{"ConnectionId":1,"Ticker":"BTCUSDT"}}
   ← {"Type":"annotations_snapshot","Data":{"connectionId":1,"ticker":"BTCUSDT","lineAnnotations":[...],...}}
   → {"Type":"ui_subscribe","Data":{}}
   ← {"Type":"ui_subscribed","Data":{}}
   ← {"Type":"ui_snapshot","Data":{"windows":[...],"standaloneCharts":[...]}}

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
   ← {"Type":"ui_update","Data":{"kind":"tabAdded","windowId":3,"window":{...}}}

7. Unsubscribe when done
   → {"Type":"signal_level_unsubscribe","Data":{}}
   ← {"Type":"signal_level_unsubscribed","Data":{}}
   → {"Type":"notification_unsubscribe","Data":{}}
   ← {"Type":"notification_unsubscribed","Data":{}}
   → {"Type":"trade_unsubscribe","Data":{"connectionId":1,"ticker":"BTCUSDT"}}
   ← {"Type":"trade_unsubscribed","Data":{"ConnectionId":1,"Ticker":"BTCUSDT"}}
   → {"Type":"orderbook_unsubscribe","Data":{"connectionId":1,"ticker":"BTCUSDT"}}
   ← {"Type":"orderbook_unsubscribed","Data":{"ConnectionId":1,"Ticker":"BTCUSDT"}}
   → {"Type":"unsubscribe","Data":{"connectionId":1}}
   ← {"Type":"unsubscribed","Data":{"ConnectionId":1}}
```

> **Field-name casing.** The envelope keys `Type` / `Data` are PascalCase. **Inbound** `Data` field names are matched case-insensitively — `connectionId` and `ConnectionId` are both accepted. **Outbound**, the server emits two casings: real-time *update/snapshot* payloads use **camelCase** field names, while subscribe/unsubscribe *acknowledgement* echoes (`subscribed`, `trade_subscribed`, `orderbook_subscribed`, …) use **PascalCase** field names. The examples throughout this document show the exact casing the server emits.

---

## Endpoint Reference

> **Request/response casing.** Request body and query field names are matched **case-insensitively** — `ticker` and `Ticker` are both accepted. Responses are emitted with a fixed casing: envelope/wrapper keys (`status`, `error`, `connections`, `connectionId`, `count`, `tickers`, `orders`, …) are **camelCase**, while the objects inside the typed list fields (connection, ticker, order, position, balance, signal-level and user-level items) are **PascalCase**. The JSON examples below show the exact casing the server emits.

### Request body parsing — applies to every endpoint with a JSON body

| Condition | HTTP | Error message |
|---|---|---|
| Body is not valid JSON | `400` | `Malformed request body: not valid JSON.` |
| Unknown property in body | `400` | `Unknown field '{field}' in request body.` |

Two endpoint families answer with their own fixed wording instead of the shared text:
`POST /api/ui/windows/{windowId}/close` and `/activate` answer `Malformed request body.`, and the
screener-template routes answer `Request body is not valid JSON.`

#### ⚠️ Breaking change for clients that send extra fields

An unrecognised property used to be accepted and silently dropped. It is now a **`400`**, on *every*
body endpoint — **including `POST /api/connections/{id}/orders` and the cancel routes**. An SDK or
script that sends a field this reference does not list will stop working, and **no order will be
placed**. Send only the documented fields.

#### Read-modify-write is supported

The identity and server-owned fields a `GET` emits are **accepted and ignored** on the matching
`PUT`/`POST`, so a client can send back the object it just read:

| Object | Accepted-and-ignored on write |
|---|---|
| User level | `id`, `connectionId`, `ticker` |
| Signal level | `id`, `connectionId`, `ticker`, `isTriggered`, `triggerTime` |
| Screener template | `id` |
| Annotations (all three types) | `index` |

A value supplied in one of these fields never re-keys, moves or re-triggers anything — the route
parameters remain authoritative.

Two known objects are **not** round-trippable verbatim: a user level whose `date` is non-null (`GET`
emits ISO-8601, `PUT` takes raw epoch seconds — a deliberate, long-standing asymmetry), and the
order-book settings `PUT`, which takes the bare settings object rather than the
`{connectionId, ticker, settings}` envelope that `GET` returns.

### Discovery

#### Ping

```
GET http://127.0.0.1:{port}/ping
```

**Response `200 OK`:**
```json
{ "app": "MetaScalp", "version": "1.0.2860" }
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

Two **mutually exclusive** payload shapes are accepted — the shape decides the behaviour, there is no
request flag:

| Field     | Type     | Description |
|-----------|----------|-------------|
| `Ticker`  | string   | Opens **one** combo layout for a single ticker (unchanged behaviour). Not a pattern, e.g. `"BTCUSDT"`. The combo opens on the currently active exchange and market connection. |
| `Tickers` | string[] | Opens **one combo layout per ticker, in the order given**. |
| `Activate` | bool | Optional, default `true`. Whether the newly opened combo takes focus. Accepted on **both** shapes. |

The `Tickers` form is a write validated **all-or-nothing**: the whole list is validated first, and if
*any* ticker resolves on no eligible connection, **nothing is opened** and the request returns `400`
naming the rejected tickers.

`Activate` decides whether the newly opened combo takes focus:

- `true` or omitted — the combo opens and becomes the active/focused layout (unchanged default). On the
  `Tickers` form exactly one window (the last in the list) comes to the front, as today.
- `false` — the combo opens **in the background**: whatever window/tab you were on stays active. On the
  `Tickers` form *no* element of the list steals focus.

**Response**

**`200 OK`:**
```json
{ "status": "ok" }
```

**`400 Bad Request`:**

| Condition              | Error message                                    |
|------------------------|--------------------------------------------------|
| Missing or empty `Ticker` | `Invalid request body. 'ticker' is required.` |
| Both `Ticker` and `Tickers` supplied, or an empty `Tickers` array with no `Ticker` | `400` |
| One or more tickers resolve on no eligible connection | `400` naming the rejected tickers (nothing is opened) |

**Example**

```bash
# Single ticker
curl -X POST http://127.0.0.1:17845/api/combo \
  -H "Content-Type: application/json" \
  -d '{"Ticker": "BTCUSDT"}'

# One combo per ticker, in order
curl -X POST http://127.0.0.1:17845/api/combo \
  -H "Content-Type: application/json" \
  -d '{"Tickers": ["BTCUSDT", "ETHUSDT", "SOLUSDT"]}'

# Open in the background (the current window/tab keeps focus)
curl -X POST http://127.0.0.1:17845/api/combo \
  -H "Content-Type: application/json" \
  -d '{"Ticker": "BTCUSDT", "Activate": false}'
```

---

### Close Last Tab

Closes the **last** tab of the main window through the same path a user's tab-close click takes. It is a test surface (memory-leak loops open a combo, then close it here); no request body. It is a fixed-target convenience route (no id) — to close a **specific** tab by id use [`POST /api/ui/tabs/{tabId}/close`](#close-a-tab).

```
POST http://127.0.0.1:{port}/api/close-last-tab
```

**Response**

| Field | Value |
|---|---|
| `status` | `"ok"` when a tab was closed, `"skipped"` when only one tab remained (it is never closed) |

```bash
curl -X POST http://127.0.0.1:17845/api/close-last-tab
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
  "connections": [
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
{ "error": "Connection {ConnectionId} not found" }
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

Returns all available trading pairs on a connection. By default returns cached data. Set `Refresh=true` to fetch fresh ticker data from the exchange.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/tickers
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/tickers?Refresh=true
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
  "connectionId": 1,
  "ticker": "BTCUSDT",
  "count": 2,
  "orders": [
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
  "connectionId": 1,
  "count": 1,
  "positions": [
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
  "connectionId": 1,
  "count": 3,
  "balances": [
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
| `Price`      | decimal | yes*     |         | For `Limit`: the limit price. For `Stop`/`StopLoss`/`TakeProfit`: the **trigger price**. Not required for `Market`. |
| `Size`       | decimal | yes      |         | Order size (must be > 0) |
| `Type`       | integer | no       | `0`     | `0` Limit, `1` Stop, `2` StopLoss, `3` TakeProfit, `4` Market |
| `ReduceOnly` | boolean | no       | `false` | Close position only, do not open new |

> **Stop / StopLoss / TakeProfit orders.** Pass `Price` as the trigger price (e.g. `64500` to trigger when BTC drops to $64,500).
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
{ "status": "ok" }
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
{ "status": "ok", "cancelledCount": 5 }
```

Returns `cancelledCount: 0` if there are no open orders for that ticker.

**Example:**
```bash
curl -X POST http://127.0.0.1:17845/api/connections/1/orders/cancel-all \
  -H "Content-Type: application/json" \
  -d '{"Ticker": "BTCUSDT"}'
```

#### Get Leverage Limits

Read-only. Reports, **live from the venue**, the maximum leverage the venue allows and the maximum
position size available for a ticker at the current (or an explicitly supplied) leverage. Values come
from the venue's own risk-limit tiers — nothing is locally computed, cached or defaulted.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/leverage-limits?Ticker=BTCUSDT
```

| Query Parameter | Type    | Required | Description |
|-----------------|---------|----------|-------------|
| `Ticker`        | string  | yes      | Trading pair symbol |
| `Leverage`      | decimal | no       | Report the max position at this leverage instead of the venue's current leverage. Must be > 0. |

**Response `200 OK`:**

| Field         | Type      | Description |
|---------------|-----------|-------------|
| `connectionId`| integer   | Connection ID |
| `ticker`      | string    | Trading pair |
| `leverage`    | decimal   | The leverage the max position is reported for — the supplied `Leverage` query param if given, otherwise the venue's current leverage for this ticker |
| `maxLeverage` | decimal?  | Highest leverage the venue allows for this ticker. `null` means the venue reports no cap (e.g. a spot market with no leverage tiers) — **never `0`** |
| `maxPosition` | decimal?  | Maximum position size available at the reported `leverage` (re-read per leverage). `null` means the venue reports no cap — **never `0`** |

```bash
curl "http://127.0.0.1:17845/api/connections/1/leverage-limits?Ticker=BTCUSDT&Leverage=10"
```

---

### Market data

#### Get Order Book Snapshot

Always fetches a **fresh** order book snapshot from the exchange REST endpoint — no cache lookup, no WebSocket subscription side effects. Intended as a one-shot complement to `orderbook_subscribe` with `fetchSnapshot=false`: subscribe to deltas cheaply, then call this endpoint when (and only when) you actually need to seed the book.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/orderbook-snapshot?Ticker=BTCUSDT
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

Returns the current cluster (volume profile / footprint) data for a ticker on a connection. The response always contains 100 time columns (oldest first, newest last), each holding bid/ask volumes at every price level. By default only the newest 10 columns carry data (the cluster backend's default page); the remaining columns are empty time slots. Pass `Columns` to fill more history.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/cluster-snapshot?Ticker=BTCUSDT&TimeFrame=M5&ZoomIndex=1
```

**Query parameters:**

| Parameter   | Type   | Required | Default | Description |
|-------------|--------|----------|---------|-------------|
| `Ticker`    | string | yes      |         | Trading pair symbol |
| `TimeFrame` | string | yes      |         | Cluster timeframe — see [ClusterTimeFrame values](#clustertimeframe-values) |
| `ZoomIndex` | int    | no       | `1`     | Price aggregation factor. `1` = no aggregation (raw price levels). Higher values group price levels into buckets of `ZoomIndex * PriceIncrement`. |
| `Columns`   | int    | no       | `0`     | History depth: how many columns to fill with data, counted back from the newest (1 = newest). `0` / omitted = the backend default page (10 columns). Values above 100 are clamped to 100. The history is fetched from the cluster backend in pages of 5 columns, so larger values take longer. |

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

- `Columns` — always 100 time-period columns, ordered chronologically (oldest first, newest last); only the newest `Columns` (default 10) carry data, the rest are empty time slots with `AsksSum`/`BidsSum` = 0 and no `Items`
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

# Fill the whole 100-column history (20 backend pages of 5 columns)
curl "http://127.0.0.1:17845/api/connections/1/cluster-snapshot?Ticker=BTCUSDT&TimeFrame=M5&Columns=100"
```

#### Order Books of a Link Group

Read-only. Returns a fresh REST order-book snapshot for **every panel currently in link group
`{groupId}`**. Membership is read **live** from the UI's own grouping — a panel unlinked or closed a
moment ago is already absent — and nothing about linking is changed by this request.

A link group is a **number** (the link value the UI shows), not a stored entity: an in-range group with
no live members is a legitimate **empty result** (`200` with an empty `members` list), *not* a `404`.
Valid group ids are `1`–`500`.

```
GET http://127.0.0.1:{port}/api/link-groups/{groupId}/orderbook-snapshots
```

**Partial failure never fails the whole response.** Each member is reported independently: a good book
carries `ok: true` and a `book` object; a member that could not be snapshotted carries `ok: false` and
a stable machine-readable `reason` token. One failing book never turns into a `500`.

| Field | Type | Description |
|---|---|---|
| `groupId` | integer | The link group (binding) id, 1–500 |
| `members` | array | One entry per panel currently live in the group (empty when the group has no live members) |
| `members[].externalId` | guid | The panel's document id |
| `members[].kind` | string? | `orderBook` or `chart` (`null` when the document could not be resolved) |
| `members[].connectionId` | integer? | The panel's connection id (`null` when unset/unresolved) |
| `members[].ticker` | string? | The panel's ticker (`null` when unset/unresolved) |
| `members[].ok` | boolean | `true` when a snapshot was returned, else `false` |
| `members[].book` | object | Present only when `ok: true`. Same shape as `orderbook-snapshot`: `updateId`, `asks`, `bids`, `bestAsk`, `bestBid` |
| `members[].reason` | string | Present only when `ok: false`. A machine-readable token (below) |

| `reason` token | Meaning |
|---|---|
| `resolve_failed` | The document could not be resolved from the layout |
| `not_an_order_book` | The member is a chart (no order book to snapshot) |
| `no_connection` | The panel has no connection set |
| `no_ticker` | The panel has no ticker set |
| `connection_not_open` | The panel's connection is not currently open |
| `market_service_unavailable` | The connection has no active market service |
| `unsupported_exchange` | The exchange has no REST snapshot endpoint (e.g. the Bybit family) |
| `snapshot_unavailable` | The exchange returned no snapshot |
| `snapshot_failed` | Fetching the snapshot threw |

**Status codes:** `200` (including an empty group); `400` for a non-numeric or out-of-range `groupId`.

```bash
curl "http://127.0.0.1:17845/api/link-groups/3/orderbook-snapshots"
```

---

### Signal Levels

Signal levels are price alerts that trigger automatically when the market price crosses the specified threshold. Once triggered, the signal level is marked as triggered (not removed) and a notification is sent. Signal level updates are also pushed via WebSocket to subscribed clients.

All signal level endpoints (except "Remove all triggered") require a valid `{ConnectionId}` in the URL path, subject to the same [connection validation errors](#trading-operations) as trading endpoints.

<a name="level-note-and-appearance"></a>
##### Note and appearance (both level types)

Signal levels and user levels share one optional note + appearance block. Every field is optional on
`POST` and `PUT`, every field is returned by `GET`, and **an omitted field keeps today's behaviour** —
unset means the level is drawn exactly as the theme draws it now.

| Field           | Type                    | Description |
|-----------------|-------------------------|-------------|
| `note`          | string                  | Free-text note stored on the level. `null` when unset. |
| `lineThickness` | number                  | Stroke thickness. |
| `lineStyle`     | string \| int           | Enum name (case-insensitive) or int, e.g. `"Dashed"`. |
| `lineColor`     | string                  | Hex colour. |
| `textSize`      | number                  | Label font size. |
| `textColor`     | string                  | Hex colour. |
| `textAlignment` | string \| int           | Enum name (case-insensitive) or int. |
| `textStyle`     | string \| int           | Enum name (case-insensitive) or int. |

`GET` emits the enum-backed fields as their **string name** (e.g. `"Dashed"`), and `null` for any field
the level does not override. An invalid enum name or value on write → `400 Invalid '{field}'.` —
validated before anything is stored.

> **Where the note is drawn.** On a **signal** level the note is rendered as the level's label (a signal
> level has no `name` of its own, so the slot is free — except on a level that has already fired, whose
> label carries the trigger time instead). On a **user** level the note is currently **stored and
> returned but never drawn**: a user level has a single label slot and `name` already occupies it. The
> user-level note is an API-level attribute until that layout question is settled.

#### Get Signal Levels

Returns all signal levels for a specific ticker on a connection.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/signal-levels?Ticker=BTCUSDT
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
      "Id": 1,
      "ConnectionId": 1,
      "Ticker": "BTCUSDT",
      "Price": 95000.00,
      "IsTriggered": false,
      "TriggerTime": null,
      "TriggerRule": "GreaterThanEqual"
    },
    {
      "Id": 2,
      "ConnectionId": 1,
      "Ticker": "BTCUSDT",
      "Price": 90000.00,
      "IsTriggered": true,
      "TriggerTime": "2026-04-13T10:30:00+00:00",
      "TriggerRule": "LessThanEqual"
    }
  ]
}
```

Signal level fields:

| Field         | Type         | Description |
|---------------|--------------|-------------|
| `Id`          | integer      | Signal level ID |
| `ConnectionId`| integer      | Connection this signal level belongs to |
| `Ticker`      | string       | Trading pair symbol |
| `Price`       | decimal      | Price threshold |
| `IsTriggered` | boolean      | Whether the signal has been triggered |
| `TriggerTime` | string (ISO)?| When the signal was triggered (null if not triggered) |
| `TriggerRule` | string       | `"LessThanEqual"` or `"GreaterThanEqual"` |

Each level additionally carries `Note` and the seven appearance fields — see
[Note and appearance](#level-note-and-appearance). They are `null` when unset.

> **Read-modify-write.** `Id`, `ConnectionId`, `Ticker`, `IsTriggered` and `TriggerTime` are
> *accepted and ignored* by `PUT`, so the object returned here can be edited and PUT straight back.
> Trigger state is owned by the signal engine and is never settable over the API.

#### Place Signal Level

Places a new signal level at a specific price. When no `TriggerRule` is supplied, the trigger rule is determined automatically from the current order book best ask — in that case the order book must be active for this ticker, and the request fails if no market data is available.

```
POST http://127.0.0.1:{port}/api/connections/{ConnectionId}/signal-levels
Content-Type: application/json
```

**Request body:**

| Field         | Type    | Required | Description |
|---------------|---------|----------|-------------|
| `Ticker`      | string  | yes      | Trading pair symbol |
| `Price`       | decimal | yes      | Price threshold (must be > 0) |
| `TriggerRule` | string  | no       | Firing direction: `"LessThanEqual"` (fires when a trade price ≤ `Price`) or `"GreaterThanEqual"` (fires when ≥ `Price`), case-insensitive. Omitted → derived from the best ask: `Price ≤ bestAsk ? LessThanEqual : GreaterThanEqual`. |
| `Note` + appearance | — | no | The optional note + 7 appearance fields — see [Note and appearance](#level-note-and-appearance). Omitted → unset. |

**Response `200 OK`:**
```json
{ "status": "ok" }
```

**`400 Bad Request`:**

| Condition | Error message |
|-----------|---------------|
| Missing `Ticker`/`Price` | `Invalid request body. 'ticker' and 'price' are required.` |
| Price <= 0 | `Price must be greater than zero` |
| Unknown `TriggerRule` | `Invalid 'triggerRule'. Allowed values: LessThanEqual, GreaterThanEqual.` |
| No order book data (rule omitted) | `No market data for 'BTCUSDT'. Subscribe to order book data for this ticker first.` |

**Example:**
```bash
curl -X POST http://127.0.0.1:17845/api/connections/1/signal-levels \
  -H "Content-Type: application/json" \
  -d '{"Ticker": "BTCUSDT", "Price": 95000.00}'
```

#### Update Signal Level

Partial in-place update of an existing signal level. Only the supplied fields are applied; the level keeps its identity (the same `Id` row is modified — no delete + re-create). At least one of `Price` / `TriggerRule` must be present.

```
PUT http://127.0.0.1:{port}/api/connections/{ConnectionId}/signal-levels/{Id}
Content-Type: application/json
```

**Request body:**

| Field         | Type    | Required | Description |
|---------------|---------|----------|-------------|
| `Price`       | decimal | no       | New trigger price (must be > 0). Leaves `TriggerRule` untouched. |
| `TriggerRule` | string  | no       | `"LessThanEqual"` / `"GreaterThanEqual"`, case-insensitive. Leaves `Price` untouched. |
| `Note` + appearance | — | no | The optional note + 7 appearance fields — see [Note and appearance](#level-note-and-appearance). Supplied → overwritten; omitted → left untouched. |
| `Id`, `ConnectionId`, `Ticker`, `IsTriggered`, `TriggerTime` | — | no | **Accepted and ignored** so a `GET` object can be PUT straight back. |

> **Re-arm on price move.** If the level had already fired (`IsTriggered = true`) and its `Price` is changed, it is re-armed (`IsTriggered` back to `false`, `TriggerTime` cleared) so it can fire again at the new price.

**Response `200 OK`:**
```json
{ "status": "ok" }
```

**Errors:**

| Condition | HTTP Status | Error message |
|-----------|-------------|---------------|
| Invalid signal level ID | `400` | `Invalid signal level ID` |
| Empty body / all fields null | `400` | `Request body must set at least one of 'price', 'triggerRule', 'note' or an appearance field.` |
| Price <= 0 | `400` | `Price must be greater than zero` |
| Unknown `TriggerRule` | `400` | `Invalid 'triggerRule'. Allowed values: LessThanEqual, GreaterThanEqual.` |
| Invalid appearance value | `400` | `Invalid '{field}'.` |
| Level not found | `404` | `Signal level {id} not found` |

#### Remove Signal Level

Removes a single signal level by ID.

```
DELETE http://127.0.0.1:{port}/api/connections/{ConnectionId}/signal-levels/{Id}
```

**Response `200 OK`:**
```json
{ "status": "ok" }
```

#### Remove All Signal Levels (by ticker)

Removes all signal levels for a specific ticker on a connection.

```
DELETE http://127.0.0.1:{port}/api/connections/{ConnectionId}/signal-levels?Ticker=BTCUSDT
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

### User Levels

User levels are the plain price levels a trader draws by hand in the order book and on the chart (a horizontal price line with a text label). They are attached to a `(connectionId, ticker)`. A level created via the API is indistinguishable from one drawn by hand: same table, same label default, same visibility in every window showing that instrument. User level changes are also pushed via WebSocket to `user_level_subscribe` subscribers.

All user level endpoints require a valid `{ConnectionId}` in the URL path, subject to the same [connection validation errors](#trading-operations) as trading endpoints.

> **Note on `Date`.** The value is stored as epoch seconds; it is **returned** as a nullable ISO-8601 timestamp but **accepted on input** as the raw epoch-seconds integer (to preserve fidelity with the hand-drawn horizontal-ray path, which stores the raw chart time). A plain level has `Date = null`.

#### Get User Levels

Returns all user levels for a specific ticker on a connection.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/user-levels?Ticker=BTCUSDT
```

| Query Parameter | Type   | Required | Description |
|-----------------|--------|----------|-------------|
| `Ticker`        | string | yes      | Trading pair symbol |

**Response `200 OK`:**
```json
{
  "connectionId": 1,
  "ticker": "BTCUSDT",
  "count": 1,
  "userLevels": [
    {
      "Id": 1,
      "ConnectionId": 1,
      "Ticker": "BTCUSDT",
      "Price": 95000.00,
      "Name": "13.04.2026",
      "Note": "watch this one",
      "Date": null,
      "LineThickness": 2.0,
      "LineStyle": "Dashed",
      "LineColor": "#FFAA00",
      "TextSize": null,
      "TextColor": null,
      "TextAlignment": null,
      "TextStyle": null
    }
  ]
}
```

User level fields:

| Field         | Type         | Description |
|---------------|--------------|-------------|
| `Id`          | integer      | User level ID |
| `ConnectionId`| integer      | Connection this level belongs to |
| `Ticker`      | string       | Trading pair symbol |
| `Price`       | decimal      | Level price |
| `Name`        | string       | Label text |
| `Note`        | string?      | The level's own free-text note (`null` when unset). **Distinct from `Name`** — see [Note and appearance](#level-note-and-appearance). |
| `Date`        | string (ISO)?| Optional timestamp (null for a plain level) |

Each level additionally carries the seven appearance fields — see
[Note and appearance](#level-note-and-appearance). They are `null` when unset.

> **Read-modify-write.** `Id`, `ConnectionId` and `Ticker` are *accepted and ignored* by `PUT`, so the
> object returned here can be edited and PUT straight back. One exception: a level whose `Date` is
> non-null is not round-trippable verbatim — `GET` emits ISO-8601 while `PUT` takes raw epoch seconds.

#### Place User Level

Creates a user level for a ticker. Unlike signal levels there is no market-data precondition — the hand-drawn path has none, so the API does not add one.

```
POST http://127.0.0.1:{port}/api/connections/{ConnectionId}/user-levels
Content-Type: application/json
```

**Request body:**

| Field    | Type    | Required | Description |
|----------|---------|----------|-------------|
| `Ticker` | string  | yes      | Trading pair symbol |
| `Price`  | decimal | yes      | Level price (must be > 0) |
| `Name`   | string  | no       | Label. Omitted → the exact hand-drawn default the app uses: today's date as `dd.MM.yyyy`. |
| `Date`   | integer (epoch seconds) | no | Optional timestamp (the hand-drawn horizontal ray stores the chart time here). Omitted → `null` (a plain level). |
| `Note` + appearance | — | no | The optional note + 7 appearance fields — see [Note and appearance](#level-note-and-appearance). Omitted → unset. `Note` is a real stored field, **not** an alias of `Name`. |

**Response `200 OK`:**
```json
{ "status": "ok" }
```

**`400 Bad Request`:**

| Condition | Error message |
|-----------|---------------|
| Missing `Ticker`/`Price` | `Invalid request body. 'ticker' and 'price' are required.` |
| Price <= 0 | `Price must be greater than zero` |

#### Update User Level

Partial in-place update of an existing user level. Only the supplied fields are applied; the level keeps its identity (the same `Id` row is modified — no delete + re-create). At least one of `Price` / `Name` / `Date` must be present.

```
PUT http://127.0.0.1:{port}/api/connections/{ConnectionId}/user-levels/{Id}
Content-Type: application/json
```

**Request body:**

| Field    | Type    | Required | Description |
|----------|---------|----------|-------------|
| `Price`  | decimal | no       | New price (must be > 0). Leaves `Name` / `Date` untouched. |
| `Name`   | string  | no       | New label. Leaves `Price` / `Date` untouched. |
| `Date`   | integer (epoch seconds) | no | New timestamp. Leaves `Price` / `Name` untouched. |
| `Note` + appearance | — | no | The optional note + 7 appearance fields — see [Note and appearance](#level-note-and-appearance). Supplied → overwritten; omitted → left untouched. |
| `Id`, `ConnectionId`, `Ticker` | — | no | **Accepted and ignored** so a `GET` object can be PUT straight back. A different value never re-keys or moves the level. |

**Response `200 OK`:**
```json
{ "status": "ok" }
```

**Errors:**

| Condition | HTTP Status | Error message |
|-----------|-------------|---------------|
| Invalid user level ID | `400` | `Invalid user level ID` |
| Empty body / all fields null | `400` | `Request body must set at least one of 'price', 'name', 'date', 'note' or an appearance field.` |
| Price <= 0 | `400` | `Price must be greater than zero` |
| Invalid appearance value | `400` | `Invalid '{field}'.` |
| Level not found | `404` | `User level {id} not found` |

#### Remove User Level

Removes a single user level by ID.

```
DELETE http://127.0.0.1:{port}/api/connections/{ConnectionId}/user-levels/{Id}
```

**Response `200 OK`:**
```json
{ "status": "ok" }
```

#### Remove All User Levels (by ticker)

Removes all user levels for a specific ticker on a connection.

```
DELETE http://127.0.0.1:{port}/api/connections/{ConnectionId}/user-levels?Ticker=BTCUSDT
```

**Response `200 OK`:**
```json
{ "status": "ok" }
```

---

### Chart Annotations

Chart annotations are the free-form shapes a trader draws on the chart, attached to a `(connectionId, ticker)`. There are three separate lists — trend **`lines`**, **`horizontal-lines`** and **`horizontal-rays`**. A shape created via the API is indistinguishable from a hand-drawn one by construction: every write (API and chart sidebar alike) goes through the same annotation service, so an API-created shape appears identically in every chart environment showing that instrument (both the SciChart and TradingView engines read the same rows — the store is per-`(connectionId, ticker)`, not per-engine). A write redraws every already-open chart of that `(connectionId, ticker)` immediately, without reopening. Shapes a trader draws by hand on the TradingView chart (`horizontal_line`, `horizontal_ray` and `trend_line` tools) are written back into the same shared store and appear in `GET` exactly like API-created shapes.

> **No per-item identity.** Annotations carry no id — the only handle is the **zero-based index** within its own list. `GET` stamps each item with its `index`; `POST` appends one; `DELETE .../{type}/{index}` removes by that index (out of range → `404`, list unchanged). The primary write is `PUT .../{type}`, a whole-list replace for one type. Because addressing is by index and every edit replaces the list, concurrent editors are **last-writer-wins** (inherent to the id-less model, not a defect) — read immediately before you `PUT` to minimise the window.

`{type}` is one of `lines` / `horizontal-lines` / `horizontal-rays`. Geometry by type:

| `{type}` | Item geometry |
|----------|---------------|
| `lines` | `{ "x1": ISO-8601 datetime, "x2": ISO-8601 datetime, "y1": number, "y2": number }` |
| `horizontal-lines` | `{ "y": number }` |
| `horizontal-rays` | `{ "y": number, "x1": ISO-8601 datetime }` |

> **Prices are venue prices — in both directions.** The `y` / `y1` / `y2` fields are the instrument's real market price (e.g. `0.2` for ADAUSDT). POST/PUT a venue price and the shape lands on the chart at that price; GET returns the on-screen venue price of every shape, including hand-drawn ones. The conversion (the chart internally scales its price axis per instrument) is done per `(connectionId, ticker)` from that instrument's own price increment. Timestamp fields (`x1` / `x2`) are passed through unchanged.
>
> **Unknown / zero-increment instrument.** The conversion needs the instrument's price increment. If the `Ticker` is not known on the connection, or its price increment is ≤ 0, GET/PUT/POST are rejected with `400 Ticker '{ticker}' not found on connection {connectionId}` — the API never silently falls back to raw axis units.

The `?Ticker=` query parameter is required on **every** annotation route. All routes are subject to the same [connection validation errors](#trading-operations) as trading endpoints.

#### Get Annotations

Returns all three annotation lists for a ticker. Each item carries its geometry plus its zero-based `index` in its own list — the only handle usable for `DELETE .../{type}/{index}`.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/annotations?Ticker=BTCUSDT
```

**Response `200 OK`:**
```json
{
  "connectionId": 1,
  "ticker": "BTCUSDT",
  "lineAnnotations": [
    { "index": 0, "x1": "2026-04-13T10:00:00", "x2": "2026-04-13T12:00:00", "y1": 95000.0, "y2": 96500.0 }
  ],
  "horizontalLineAnnotations": [
    { "index": 0, "y": 94000.0 }
  ],
  "horizontalRayAnnotations": [
    { "index": 0, "y": 97000.0, "x1": "2026-04-13T11:30:00" }
  ]
}
```

| List | Item fields |
|------|-------------|
| `lineAnnotations` | `index`, `x1`, `x2` (ISO-8601 datetime), `y1`, `y2` (venue price) |
| `horizontalLineAnnotations` | `index`, `y` (venue price) |
| `horizontalRayAnnotations` | `index`, `y` (venue price), `x1` (ISO-8601 datetime) |

#### Replace Annotations (whole list, one type)

The primary write: replaces the **entire** list for `{type}` with the posted array. The body is a JSON array of items of that type's geometry (a null / empty body clears the list). This is how a client persists edits — read with `GET`, mutate the array client-side, `PUT` it back.

```
PUT http://127.0.0.1:{port}/api/connections/{ConnectionId}/annotations/{type}?Ticker=BTCUSDT
Content-Type: application/json
```

**Request body** (example for `lines`):
```json
[
  { "x1": "2026-04-13T10:00:00", "x2": "2026-04-13T12:00:00", "y1": 95000.0, "y2": 96500.0 }
]
```

**Response `200 OK`:**
```json
{ "status": "ok" }
```

#### Append Annotation

Appends a single annotation to the `{type}` list. The body is **one item** (not an array) of that type's geometry. Implemented as read-modify-write inside the one action (read the current list, append, replace the whole list) — the list is never cached across requests. The appended item lands at the last index.

```
POST http://127.0.0.1:{port}/api/connections/{ConnectionId}/annotations/{type}?Ticker=BTCUSDT
Content-Type: application/json
```

**Request body** (example for `horizontal-rays`):
```json
{ "y": 97000.0, "x1": "2026-04-13T11:30:00" }
```

**Response `200 OK`:**
```json
{ "status": "ok" }
```

#### Remove Annotation (by index)

Removes the annotation at the given zero-based `{index}` in the `{type}` list. An out-of-range index is a `404` and the stored list is left unchanged. The `index` is the value stamped on each item by `GET`.

```
DELETE http://127.0.0.1:{port}/api/connections/{ConnectionId}/annotations/{type}/{index}?Ticker=BTCUSDT
```

**Response `200 OK`:**
```json
{ "status": "ok" }
```

#### Remove All Annotations

Clears all three annotation lists (lines, horizontal-lines, horizontal-rays) for the `(connectionId, ticker)` in one call — the same action the «clear all shapes» sidebar command performs.

```
DELETE http://127.0.0.1:{port}/api/connections/{ConnectionId}/annotations?Ticker=BTCUSDT
```

**Response `200 OK`:**
```json
{ "status": "ok" }
```

#### Annotation errors

| Condition | HTTP Status | Error message |
|-----------|-------------|---------------|
| Unknown `{type}` | `400` | `Unknown annotation type '{type}'. Allowed values: lines, horizontal-lines, horizontal-rays.` |
| Missing `Ticker` query | `400` | `Query parameter 'Ticker' is required` |
| Unknown ticker or price increment ≤ 0 | `400` | `Ticker '{ticker}' not found on connection {connectionId}` |
| Empty / wrong-shape POST body | `400` | `Request body must be a {type} annotation.` |
| Non-numeric index | `400` | `Invalid annotation index` |
| Index out of range | `404` | `{Type} annotation index {index} not found` |
| Unknown connection | `404` | `Connection {id} not found` |

---

### Custom Notifications

Push your own row into MetaScalp's notification feed (the «Line notifications» window). An injected notification is indistinguishable from a native one except for its event-type label: it shares the same feed, the same 200-row cap, the same ordering, and is delivered live over the WebSocket as `notification_update`. **Every field is optional** — a field you do not send renders as an empty cell. Nothing is persisted (in-memory only). The feed's sound and the settings checkboxes do not apply to an injected row.

#### Inject Notification

```
POST http://127.0.0.1:{port}/api/notifications
Content-Type: application/json
```

**Request body** (all fields optional):

| Field          | Type    | Required | Description |
|----------------|---------|----------|-------------|
| `Time`         | string (ISO-8601) | no | Notification timestamp. Omitted → the request time (server UTC now). |
| `ConnectionId` | integer | no       | When supplied, fills the exchange icon + S/F badge and the exchange/market/colour cells from that connection. Omitted → those cells stay empty. Unknown id → `404`. |
| `Ticker`       | string  | no       | Instrument label. Omitted → empty cell. |
| `EventType`    | string  | no       | Free-text label shown in the Event Type column, verbatim (no fixed enum). Omitted → empty cell. |
| `Size`         | decimal | no       | Size cell. Omitted → empty (null, no sentinel). |
| `Price`        | decimal | no       | Price cell. Omitted → empty (null, no sentinel). |
| `TabName`      | string  | no       | Tab-name cell. Omitted → empty cell. |

**Response `200 OK`:**
```json
{ "status": "ok" }
```

The row is broadcast to subscribed WebSocket clients as a `notification_update` whose `type` is the supplied `EventType` (or, for native notifications, the notification-type name as before).

> **Notifications globally disabled still answers `200`.** This endpoint injects directly into the in-memory feed and its WebSocket broadcast; it does not consult the user's global «show notifications» toggle or the per-type settings checkboxes. A request therefore succeeds (and is delivered to subscribed WS clients) even when the desktop UI would suppress the equivalent native pop-up. Treat delivery as feed-level, not UI-visibility-level.

**Errors:**

| Condition | HTTP Status | Error message |
|-----------|-------------|---------------|
| Unknown `ConnectionId` | `404` | `Connection {id} not found` |

**Example:**
```bash
curl -X POST http://127.0.0.1:17845/api/notifications \
  -H "Content-Type: application/json" \
  -d '{"ConnectionId": 1, "Ticker": "BTCUSDT", "EventType": "My bot signal", "Price": 95000.0, "Size": 0.5}'
```

---

### UI Inventory & Control

Two read-only endpoints describe the whole open interface — every window, its tabs, and the order books and charts inside each — plus standalone chart windows. The tree is rebuilt entirely from the saved layout in the database, never by reading the live on-screen windows (each runs on its own UI thread and cannot be touched from the API). Every field is always present: a value the system does not store, or that only exists at runtime, is reported as `null`, never omitted. The two `PUT` control routes below address one panel by its stable GUID.

> All UI endpoints use **camelCase** field names throughout (responses and request bodies).

#### Get UI State

Returns the whole open UI as `{ "windows": [ ... ], "standaloneCharts": [ ... ] }`. Each window carries its geometry, monitor and tabs; each tab its documents (order books and charts); each document its identity, connection, ticker, link number and docking position, plus kind-specific fields.

```
GET http://127.0.0.1:{port}/api/ui/state
```

**Window fields:**

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `id` | integer | no | Persisted window id. |
| `type` | string | no | Window type (`Workspace` / `MainWorkspace` / `LightWorkspace`). A window has no title and no persisted focus flag — the «title» is a tab concept (`tab.name`) and the «active» signal is `tab.isSelected`. |
| `geometry` | object | no | `{ top, left, width, height, isFullScreen, pinOnTop, isTopBarHidden }`. `pinOnTop` is nullable (tri-state in the model). |
| `monitor` | integer | always null | Would be computed from geometry vs the screen list, but screen enumeration lives in a UI assembly the API does not reference — reported as `null`. |
| `tabs` | array | no | The window's tabs (see below). |

**Tab fields:**

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `id` | integer | no | Persisted tab id. |
| `name` | string | yes | Tab name (the «title»). |
| `orderId` | integer | no | Tab ordering index. |
| `isSelected` | boolean | no | Whether this is the active tab in its window (the only persisted «active» signal). |
| `color` | string | yes | Tab colour, when set. |
| `documents` | array | no | The order books and charts on this tab. |

**Document fields (common):**

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `id` | integer | no | Persisted document id. |
| `externalId` | string (GUID) | no | The stable id the docking layout XML joins on (its `ContentId`) — use it for the `PUT /api/ui/documents/{externalId}/...` routes. |
| `kind` | string | no | `orderBook` or `chart`. |
| `connectionId` | integer | yes | Owning connection; `null` when the document is not bound to a connection. |
| `ticker` | string | yes | Instrument label; `null` when unset. |
| `linkNumber` | integer | yes | The link/binding group id read from the runtime binding bus. `null` when the document is not a binding source. |
| `layoutPosition` | object | yes | `{ paneGroupIndex, documentIndex, orientation }` parsed from the tab's AvalonDock layout XML (matched by `externalId` == `ContentId`). `null` for standalone charts and for documents absent from the layout XML. |
| `orderBook` | object | yes | Order-book details (below); `null` when `kind != orderBook`. |
| `chart` | object | yes | Chart details (below); `null` when `kind != chart`. |

**Order-book details (`document.orderBook`):**

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `baseAsset` | string | yes | Base asset. |
| `quoteAsset` | string | yes | Quote asset. |
| `priceIncrement` | decimal | no | Price tick size. |
| `sizeIncrement` | decimal | no | Size step. |
| `minSize` | decimal | no | Minimum order size. |
| `maxSize` | decimal | yes | Maximum order size, when defined. |
| `stepPrice` | decimal | yes | Step price, when defined. |
| `state` | string | no | Order-book state (`Open` / `Closing`). |
| `isTradingAllowed` | boolean | no | Whether trading is enabled on this book. |
| `pinMarketType` | boolean | no | Whether the market type is pinned. |
| `zoomIndex` | integer | yes | The order-book zoom (aggregation) index, resolved from the persisted order-book settings set. `null` only when the document has no connection/ticker or no settings row exists. This is **not** the market-data per-WS-subscription `zoomIndex`. |

**Chart details (`document.chart`):**

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `timeFrame` | string | no | Chart time frame enum name (`None`, `M1`, …). |
| `engine` | string | no | Chart engine: `Regular` or `TradingView`. |
| `xVisibleRangeDiff` | integer | no | Visible x-range span. |
| `pinMarketType` | boolean | no | Whether the market type is pinned. |
| `geometry` | object | yes | Own window geometry for standalone charts; `null` for charts docked inside a tab. |

`standaloneCharts` is a top-level array of chart documents (same document shape) for chart windows not attached to any tab; each includes its own `chart.geometry` and never appears under a tab.

#### Get One Window

Returns exactly the same single-window object as that window's entry in `/api/ui/state` — same geometry, tabs and documents.

```
GET http://127.0.0.1:{port}/api/ui/windows/{windowId}
```

**Errors:**

| Condition | HTTP Status | Error message |
|-----------|-------------|---------------|
| Non-numeric `windowId` | `400` | `Invalid window ID` |
| Unknown window | `404` | `Window {id} not found` |

#### Set Link Number

Sets the link (binding-group) number of one addressed panel — the same `linkNumber` reported by `GET /api/ui/state`. `{externalId}` is the document's GUID.

```
PUT http://127.0.0.1:{port}/api/ui/documents/{externalId}/link-number
Content-Type: application/json
```

**Request body:**
```json
{ "linkNumber": 3 }
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `linkNumber` | integer | yes | Binding-group id: `1` = active window, `2`–`500` = a user link, `9999` = cancel link. Must be positive. |

Persisted immediately via the binding bus (a DB-backed write — no UI thread is used). The stored link changes at once; a panel already on screen may not fully re-wire its live link until it is reloaded. Affects only the addressed document.

**Response `200 OK`:**
```json
{ "status": "ok" }
```

**Errors:**

| Condition | HTTP Status | Error message |
|-----------|-------------|---------------|
| Non-GUID `externalId` | `400` | `Invalid document ID` |
| Missing `linkNumber` | `400` | `Request body must set 'linkNumber'.` |
| Non-positive `linkNumber` | `400` | `linkNumber must be a positive binding-group id (1-500, or 9999 to cancel).` |
| Unknown document | `404` | `Document {externalId} not found` |

#### Re-ticker a Panel

Re-points one addressed panel (order book or chart) to a different market. This targets **only that panel** — unlike `POST /api/change-ticker`, which drives the active window and a whole named link group. `{externalId}` is the document's GUID.

```
PUT http://127.0.0.1:{port}/api/ui/documents/{externalId}/ticker
Content-Type: application/json
```

**Request body:**
```json
{ "ticker": "ETHUSDT", "connectionId": 77 }
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `ticker` | string | yes | The instrument to switch the panel to. |
| `connectionId` | integer | no | Target connection. Omit to keep the panel's current connection (a plain re-ticker on the same venue). |

Asynchronous with respect to the UI thread: the API resolves the document and dispatches a change-ticker action to that panel's own live listener, which re-resolves the ticker and re-tickers itself on its own window dispatcher; the visible change and its persistence happen inside the panel.

**Response `200 OK`:**
```json
{ "status": "ok" }
```

**Errors:**

| Condition | HTTP Status | Error message |
|-----------|-------------|---------------|
| Non-GUID `externalId` | `400` | `Invalid document ID` |
| Missing `ticker` | `400` | `Request body must set 'ticker'.` |
| No connection to inherit | `400` | `connectionId is required: the document has no current connection to inherit.` |
| Unknown document | `404` | `Document {externalId} not found` |

#### Window addressing

The three window lifecycle routes below address a window by **`{windowId}` *plus* `windowType`**.
Window ids are **per-surface, not global**: a workspace, a chart, a screener and a watchlist can all be
id `1`, so the body's `windowType` is what routes the id to the correct window.

`windowType` is one of: `workspace`, `lightWorkspace`, `chart`, `tradingViewChart`, `watchlist`,
`screener`, `lineNotifications`, `finres`, `userTrades`, `listing`.

The four **singleton** surfaces (`lineNotifications`, `finres`, `userTrades`, `listing`) ignore the
path id entirely — each has one fixed row.

#### Open a Window

Opens ONE terminal window of the requested type at an explicit position and size, on an explicit
monitor. **Geometry is entirely optional**: omit `top` / `left` / `height` / `width` and the window
lands exactly where it lands today for that type — including MetaScalp's open-at-the-cursor placement
where the app does that today. A supplied rect is **clamped** to fit the target monitor. Position is in
virtual-desktop coordinates; `monitor` (0-based) selects the screen.

```
POST http://127.0.0.1:{port}/api/ui/windows
Content-Type: application/json
```

| Field | Type | Required | Description |
|---|---|---|---|
| `windowType` | string | yes | One of the ten types above. |
| `top` | number | no | Virtual-desktop Y. Omit for today's default placement. |
| `left` | number | no | Virtual-desktop X. Omit for today's default placement. |
| `height` | number | no | Window height. Omit for the type's default. |
| `width` | number | no | Window width. Omit for the type's default. |
| `monitor` | integer | no | 0-based target screen. Omit to place by virtual-desktop coordinate. |
| `templateId` | integer | no | **Screener only.** Open the screener already showing this saved template's filters and rows. Omit for today's Default view. The synthetic Default (`-1`) is accepted. Ignored for any non-screener `windowType`. |

**Response `200 OK`:** `{ windowId, windowType, top, left, height, width }` — where the window
*actually* landed (post-clamp). For the four singleton types `windowId` is `null`: each has a single
fixed row (id `1`, which would collide with the main workspace) and its close/activate routes ignore
the id, so there is no addressable per-surface id to return.

> **Opening the screener on a template.** For `windowType: "screener"` a supplied `templateId` is
> threaded onto the window so it loads that template exactly as picking it by hand would; because the
> choice is persisted with the window, it also becomes the window's template on the next launch.
> Omitting `templateId` is bit-for-bit today's behaviour.

**Errors:** malformed body → `400 Malformed request body.`; missing `windowType` → `400` with the
supported list; an unknown or unsupported-standalone type (e.g. `settings`, which is modal, or the
full-screen chart, which needs an existing chart) → `400` naming the supported list; an unknown screener
`templateId` → `404 Screener template {templateId} not found`. All validated **before** any window opens.

```bash
curl -X POST http://127.0.0.1:17845/api/ui/windows \
  -H "Content-Type: application/json" \
  -d '{"windowType": "watchlist", "top": 100, "left": 200, "height": 450, "width": 300, "monitor": 0}'
```

#### Close a Window

Closes ONE terminal window through the app's **own** close path (the same path a manual ✕ uses, so it
inherits today's close-confirmation-popup behaviour). `finres` is **hidden** rather than destroyed,
matching its manual toggle.

```
POST http://127.0.0.1:{port}/api/ui/windows/{windowId}/close
Content-Type: application/json
```

| Field | Type | Required | Description |
|---|---|---|---|
| `windowType` | string | yes | One of the ten types above. |

**Response `200 OK`:** `{ windowType, windowId, closed, outcome }`.

> **The main workspace can never be closed through the API** — it would quit the terminal. A `windowId`
> that resolves to the main workspace → `400 The main workspace cannot be closed through the API.`

**Errors:** non-numeric `{windowId}` → `400 Invalid window ID`; malformed body →
`400 Malformed request body.`; missing / unknown / unsupported `windowType` → `400` naming the
supported list; unknown window id → `404`. All validated **before** any window is closed.

#### Activate a Window

Brings ONE terminal window to the front on its own window thread. Same id-addressing as close.

```
POST http://127.0.0.1:{port}/api/ui/windows/{windowId}/activate
Content-Type: application/json
```

| Field | Type | Required | Description |
|---|---|---|---|
| `windowType` | string | yes | One of the ten types above. |

**Response `200 OK`:** `{ windowType, windowId, activated, outcome }`.

**Errors:** non-numeric `{windowId}` → `400 Invalid window ID`; malformed body → `400`; missing /
unknown / unsupported `windowType` → `400` naming the supported list; the main workspace → `400`;
unknown window id → `404`.

#### Close a Tab

Closes the ONE tab addressed by `{tabId}` through the app's **own** tab-close path (the same teardown
the tab's own ✕ runs — every document in the tab is torn down and its DB rows deleted). `{tabId}` is
the `id` a tab carries under `GET /api/ui/state` (Window → Tab). Tab ids are **globally unique**, so no
window type is needed: the id alone addresses the tab, and the window it belongs to and its sibling tabs
stay open.

```
POST http://127.0.0.1:{port}/api/ui/tabs/{tabId}/close
```

**No request body is required** (an empty body or `{}` is accepted; a malformed body or an unknown
property → `400`).

**Response `200 OK`:** `{ tabId, closed, outcome }`.

> **Unlike a hand-close, the API path raises NO confirmation prompt** (a modal would block the HTTP
> response); the interactive prompt — and its «Show a warning when closing windows and tabs» setting —
> is unchanged.

> **A window's ONLY tab cannot be closed** → `400` with a message (it would leave an empty workspace);
> there is no silent no-op.

**Errors:** non-numeric `{tabId}` → `400 Invalid tab ID`; malformed body / unknown body property →
`400`; an id no open window hosts → `404 Tab {tabId} not found.` All validated **before** any tab is
closed.

```bash
curl -X POST http://127.0.0.1:17845/api/ui/tabs/42/close
```

#### Activate a Tab

Makes the tab addressed by `{tabId}` the selected one in its window, by driving the SAME header
selection a user's tab click drives (which also persists it). Same id-addressing as close; no request
body is required. Idempotent when the tab is already selected.

```
POST http://127.0.0.1:{port}/api/ui/tabs/{tabId}/activate
```

**Response `200 OK`:** `{ tabId, activated, outcome }`.

**Errors:** non-numeric `{tabId}` → `400 Invalid tab ID`; malformed body / unknown body property →
`400`; an id no open window hosts → `404 Tab {tabId} not found.`

```bash
curl -X POST http://127.0.0.1:17845/api/ui/tabs/42/activate
```

#### Still-deferred UI lifecycle routes

Opening/closing an individual **panel** (a single order book or chart inside a tab) is still not
available. Whole-window lifecycle and per-**tab** close/activate ARE available (see above); only the
panel-level routes below remain deferred — faking them by writing the database directly would diverge
the on-screen layout from the saved model. These routes do **not** exist:

| Route | Intent |
|-------|--------|
| `POST /api/ui/windows/{windowId}/order-books` | Open an order book in a window's active tab |
| `POST /api/ui/windows/{windowId}/charts` | Open a chart in a window's active tab |
| `DELETE /api/ui/documents/{externalId}` | Close one addressed panel |

---

### Order Book Settings

Read and update order book display and trading settings for a specific ticker on a connection. The update endpoint uses partial semantics — only send the fields you want to change; omitted fields keep their current values.

All order book settings endpoints require a valid `{ConnectionId}` in the URL path, subject to the same [connection validation errors](#trading-operations) as trading endpoints.

> **Working volumes are paired USD/coin mirrors.** `tradeAmountUsd1..5` ↔ `tradeAmount1..5`: writing
> **one** side recomputes the other at the **live best ask** (truncated to the ticker's size increment,
> floored at its min size), so the terminal's own volume buttons show USD and coin figures that agree
> with the current price. Writing **both** sides of a pair keeps the values you sent.
>
> The conversion needs a live price, so it needs an **open order book** for that connection + ticker.
> If none is open → **`409`** (open the order book so the conversion has a live price); unknown ticker →
> `404`. **Nothing is written in either case.**

#### Get Order Book Settings

Returns all order book settings for a specific ticker on a connection.

```
GET http://127.0.0.1:{port}/api/connections/{ConnectionId}/orderbook-settings?Ticker=BTCUSDT
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
    "NotificationTradeHasBeenMade": true,
    "OrderTypeDefault": 0,
    "DefaultOrderCoin": 0.001,
    "DefaultOrderUsd": 100.0,
    "OrderSlippageCoin": 0.0,
    "OrderSlippageUsd": 0.0,
    "CloseByMarket": false,
    "AmountBarFilledAt": 10000.0,
    "LargeAmount": 10000.0,
    "LargeAmount2": 20000.0,
    "AmountBarFilter": 0.0,
    "AmountBarFilledAtUsd": 30000.0,
    "LargeAmountUsd": 20000.0,
    "LargeAmountUsd2": 30000.0,
    "AmountBarFilterUsd": 0.0,
    "NotificationLargeAmountDetected": false,
    "NotificationLargeAmount2Detected": false,
    "UseLargeAmountDetectionArea": false,
    "LargeAmountDetectionMinValue": 0.0,
    "LargeAmountDetectionMaxValue": 0.0,
    "ShowRuler": "Percent",
    "ZoomType": "Absolute",
    "ZoomStepMode": "Logarithmic",
    "AutoZoom": false,
    "ZoomPercent": 2.0,
    "RowHeight": 12.0,
    "SlimLevelsFactor": 10.0,
    "BasicLevelsFactor": 50.0,
    "NotificationSignalLevelTriggered": true,
    "Autoscroll": false,
    "FullDepth": true,
    "TicksLargeAmount": 0.0,
    "TicksLargeAmountUsd": 0.0,
    "SizeType": "Coin",
    "NotificationTradeHasBeenMadeTicks": false,
    "ShowExecutedOrders": true,
    "ShowClusters": false,
    "ClusterTimeFrame": "M1",
    "SoundNotification": true
  }
}
```

Settings field reference:

**Trading**

| Field | Type | Description |
|-------|------|-------------|
| `NotificationTradeHasBeenMade` | boolean | Notify when a trade is executed |
| `OrderTypeDefault` | integer | Default order type (`0` Limit, `4` Market) |
| `DefaultOrderCoin` | decimal | Default order size in base asset |
| `DefaultOrderUsd` | decimal | Default order size in USD |
| `OrderSlippageCoin` | decimal | Order slippage allowance in base asset |
| `OrderSlippageUsd` | decimal | Order slippage allowance in USD |
| `CloseByMarket` | boolean | Close positions using market orders |

**Order Book**

| Field | Type | Description |
|-------|------|-------------|
| `AmountBarFilledAt` | decimal | Amount bar fill threshold (base asset) |
| `LargeAmount` | decimal | Large amount highlight threshold (base asset) |
| `LargeAmount2` | decimal | Second large amount highlight threshold (base asset) |
| `AmountBarFilter` | decimal | Minimum amount to display in order book (base asset) |
| `AmountBarFilledAtUsd` | decimal | Amount bar fill threshold (USD) |
| `LargeAmountUsd` | decimal | Large amount highlight threshold (USD) |
| `LargeAmountUsd2` | decimal | Second large amount highlight threshold (USD) |
| `AmountBarFilterUsd` | decimal | Minimum amount to display in order book (USD) |
| `NotificationLargeAmountDetected` | boolean | Notify when large amount is detected |
| `NotificationLargeAmount2Detected` | boolean | Notify when second large amount is detected |
| `UseLargeAmountDetectionArea` | boolean | Use price range for large amount detection |
| `LargeAmountDetectionMinValue` | decimal | Minimum price for large amount detection area |
| `LargeAmountDetectionMaxValue` | decimal | Maximum price for large amount detection area |
| `ShowRuler` | string | Ruler display mode: `"None"`, `"Points"`, `"Percent"`, `"PercentVolume"` |
| `ZoomType` | string | Zoom type: `"Absolute"`, `"Percentage"` |
| `ZoomStepMode` | string | How one mouse-wheel notch moves the compression: `"Linear"` (by one unit) or `"Logarithmic"` (to the next rung of the 1, 2, 5, 10, 20, 50 … ladder). `null` on a settings row saved before this field existed — the terminal treats that as `"Linear"` |
| `AutoZoom` | boolean | Enable automatic zoom |
| `ZoomPercent` | decimal | Zoom percentage value |
| `RowHeight` | decimal | Order book row height in pixels |
| `SlimLevelsFactor` | decimal | Factor for slim price levels |
| `BasicLevelsFactor` | decimal | Factor for basic price levels |
| `NotificationSignalLevelTriggered` | boolean | Notify when a signal level is triggered |
| `Autoscroll` | boolean | Auto-scroll order book to current price |
| `FullDepth` | boolean | Show full order book depth |
| `SizeType` | string | Size display type: `"Coin"`, `"Usd"` |
| `SoundNotification` | boolean | Enable sound notifications |

**Ticks**

| Field | Type | Description |
|-------|------|-------------|
| `TicksLargeAmount` | decimal | Large tick highlight threshold (base asset) |
| `TicksLargeAmountUsd` | decimal | Large tick highlight threshold (USD) |
| `NotificationTradeHasBeenMadeTicks` | boolean | Notify on large ticks |
| `ShowExecutedOrders` | boolean | Paint the terminal's own executed-order overlay (fill chips + position-size chip) on the tape. Default `true`; absent behaves as `true` |

**Clusters**

| Field | Type | Description |
|-------|------|-------------|
| `ShowClusters` | boolean | Show cluster (volume profile) data |
| `ClusterTimeFrame` | string | Cluster timeframe: `"M1"`, `"M5"`, `"M15"`, `"M30"`, `"H1"`, `"H4"`, `"D1"` |

**Enum values:**

| Field | Valid values |
|-------|-------------|
| `ShowRuler` | `"None"`, `"Points"`, `"Percent"`, `"PercentVolume"` |
| `ZoomType` | `"Absolute"`, `"Percentage"` |
| `ZoomStepMode` | `"Linear"`, `"Logarithmic"` |
| `SizeType` | `"Coin"`, `"Usd"` |
| `ClusterTimeFrame` | `"M1"`, `"M5"`, `"M15"`, `"M30"`, `"H1"`, `"H4"`, `"D1"` |

#### Update Order Book Settings

Partial update — only send the fields you want to change. Omitted fields keep their current values.

```
PUT http://127.0.0.1:{port}/api/connections/{ConnectionId}/orderbook-settings?Ticker=BTCUSDT
Content-Type: application/json
```

| Query Parameter | Type   | Required | Description |
|-----------------|--------|----------|-------------|
| `Ticker`        | string | yes      | Trading pair symbol |

**Request body** (only include fields to update):
```json
{
  "LargeAmountUsd": 50000,
  "RowHeight": 14,
  "Autoscroll": true
}
```

**Response `200 OK`:** Same shape as GET — returns the full updated settings object.

**`400 Bad Request`:**

| Condition | Error message |
|-----------|---------------|
| Missing ticker | `Query parameter 'Ticker' is required` |
| Invalid enum value | `Invalid value 'X' for field 'ShowRuler'. Valid values: None, Points, Percent, PercentVolume` |
| No settings found | `No order book settings found for this ticker` |

**Examples:**

```bash
# Get order book settings
curl "http://127.0.0.1:17845/api/connections/1/orderbook-settings?Ticker=BTCUSDT"

# Update order book settings (partial)
curl -X PUT http://127.0.0.1:17845/api/connections/1/orderbook-settings?Ticker=BTCUSDT \
  -H "Content-Type: application/json" \
  -d '{"LargeAmountUsd": 50000, "RowHeight": 14, "Autoscroll": true}'
```

---

### Screener Templates

A screener template is a saved screener configuration — its columns, filters and market selections.
Templates are addressed by numeric id. A **synthetic Default** template with id `-1` always exists and
is always returned first by the list route; it resolves to the default configuration exactly like an
open screener window's fallback.

#### List Templates

```
GET http://127.0.0.1:{port}/api/screener/templates
```

Returns `{ count, templates[] }`. Each template is `{ id, name, settings }`, where `settings` carries
the whole configuration (`columnSettings[]`, `columnFilters[]`, `marketFilters[]`, `coinTags[]`,
`numberOfRows`, `activeFilter`, the new-coin flags, …).

#### Get One Template

```
GET http://127.0.0.1:{port}/api/screener/templates/{templateId}
```

Returns the single `{ id, name, settings }`.

**Errors:** non-numeric `{templateId}` → `400 Invalid template ID`; unknown id (including the synthetic
Default `-1`) → `404 Screener template {id} not found`.

#### Create Template

```
POST http://127.0.0.1:{port}/api/screener/templates
Content-Type: application/json
```

| Field | Type | Required | Description |
|---|---|---|---|
| `name` | string | yes | Template name. |
| `settings` | object | no | The full settings blob. Omit and the template is created with the default configuration. |

The full `settings` blob (columns / filters / market selections) is persisted, so a template created
here carries its filters and columns, not just its name. Returns the created `{ id, name, settings }`.

**Errors:** missing / blank `name` → `400 Request body must carry a non-empty 'name'.`; a body that is
not valid JSON → `400 Request body is not valid JSON.`

#### Update Template

```
PUT http://127.0.0.1:{port}/api/screener/templates/{templateId}
Content-Type: application/json
```

| Field | Type | Required | Description |
|---|---|---|---|
| `name` | string | no | A null / omitted `name` keeps the stored name. |
| `settings` | object | no | A null / omitted `settings` keeps the stored blob. |
| `id` | integer | no | **Accepted and ignored** so a `GET` object can be PUT straight back. The template is addressed by `{templateId}`. |

Returns the updated `{ id, name, settings }`.

**Errors:** non-numeric `{templateId}` → `400`; a body that is not valid JSON → `400`; unknown id →
`404 Screener template {id} not found` (checked **before** any write).

#### Delete Template

```
DELETE http://127.0.0.1:{port}/api/screener/templates/{templateId}
```

Returns `{ status: "ok" }`.

**Errors:** non-numeric `{templateId}` → `400`; unknown id (including the synthetic Default `-1`) →
`404 Screener template {id} not found`.

#### Snapshot a Template's Data (headless)

Returns **ONE headless snapshot** of the full row set of the given template — every row an open screener
window on the same template would show — **without opening a screener window**. A fresh background
screener socket is subscribed, the first complete frame captured, and the socket disposed. **There is no
streaming.** The synthetic Default (`-1`) is answered too.

```
GET http://127.0.0.1:{port}/api/screener/templates/{templateId}/data
```

**Response `200 OK`:** `{ templateId, count, rows[] }`. Each row is
`{ ticker, wireTicker, isNewCoin, exchange, listedExchanges[], columns[] }`, where `exchange` /
`listedExchanges` are screener exchange keys (`"binance_s"`, `"bybit_f"`, `"polymarket"`) and each
`columns[]` entry is `{ type, timeFrame, time, metric, value }`.

**Errors:** non-numeric `{templateId}` → `400 Invalid template ID`; unknown id →
`404 Screener template {id} not found` (checked **before** any subscribe). If the screener backend
produces no rows within the deadline, `rows` is an empty array (`count = 0`).

```bash
curl "http://127.0.0.1:17845/api/screener/templates/-1/data"
```

---

### Real-time WebSocket

Connect via WebSocket to receive live updates. Subscribe by connection ID for order, position, balance, and finres events. Subscribe by connection ID + ticker for real-time trade, order book, mark price, index price, and funding market data — or for chart annotation snapshots. Subscribe globally (no ID) for notifications, signal levels, user levels, and UI change events.

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

> **Casing.** The envelope keys `Type` / `Data` are PascalCase. Inbound `Data` field names are matched case-insensitively (`connectionId` and `ConnectionId` both work). Outbound update/snapshot payloads use **camelCase** field names; subscribe/unsubscribe acknowledgement echoes use **PascalCase** field names. Examples below show the exact emitted casing.

#### Messages you send

**Connection-level subscriptions** — subscribe by connection ID to receive order, position, balance, and finres updates:

| Type | Data | Description |
|---|---|---|
| `subscribe` | `{ "connectionId": 123 }` | Subscribe to updates for a connection. Connection must be active in MetaScalp. Idempotent — re-subscribing is a no-op. |
| `unsubscribe` | `{ "connectionId": 123 }` | Stop receiving updates for a connection. Idempotent. |

**Market data subscriptions** — subscribe by connection ID + ticker to receive trade, order book, mark price, index price, or funding updates for a specific symbol:

| Type | Data | Description |
|---|---|---|
| `trade_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT", "zoomIndex": 1 }` | Subscribe to real-time trade updates. When `zoomIndex` > 1, trades are aggregated by zoomed price level before sending. Re-subscribing updates the zoom index. Connection and ticker must be valid. |
| `trade_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop receiving trade updates for that ticker. Idempotent. |
| `orderbook_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT", "zoomIndex": 0, "depthLevels": 50, "depthPercent": 0.5 }` | Subscribe to order book updates for a specific ticker on a connection. You will receive an initial snapshot followed by incremental updates. When `zoomIndex` > 1, price levels are aggregated into zoomed buckets. Re-subscribing replaces `zoomIndex` / `depthLevels` / `depthPercent` atomically. Connection must be active. Idempotent. Optional `depthLevels` (top-N per side, snapshot only), `depthPercent` (per-side band on best ask / best bid, snapshot + updates) and `fetchSnapshot` (default `true`) — see notes below. |
| `orderbook_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop receiving order book updates for that ticker. Idempotent. |
| `mark_price_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Subscribe to mark price updates for a specific ticker. No initial snapshot — only live updates. Connection must be active. Idempotent. |
| `mark_price_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop receiving mark price updates for that ticker. Idempotent. |
| `index_price_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Subscribe to index price updates for a specific ticker (futures only). No initial snapshot — only live updates. Connection must be active. Idempotent. |
| `index_price_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop receiving index price updates for that ticker. Idempotent. |
| `funding_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Subscribe to funding rate updates for a specific ticker. No initial snapshot — only live updates. Not all exchanges or markets emit funding events. Connection must be active. Idempotent. |
| `funding_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop receiving funding updates for that ticker. Idempotent. |

**`orderbook_subscribe` optional fields:**

- **`zoomIndex`** *(int, default `0`)* — price aggregation factor. When `> 1`, levels are bucketed into zoomed price slots and sizes summed. Affects both snapshot and updates.
- **`depthLevels`** *(int, optional, must be ≥ 1)* — keep at most N price levels per side (asks ascending by price, bids descending), applied **after** zoom and `depthPercent`. **Filters the snapshot only — incremental updates are unaffected**, so the client should maintain its own top-N view as updates arrive.
- **`depthPercent`** *(decimal, optional, must be > 0)* — per-side band as a percentage, anchored on **best ask** / **best bid** (NOT the mid):
  - Asks: keeps `price ≤ bestAsk × (1 + depthPercent / 100)`.
  - Bids: keeps `price ≥ bestBid × (1 − depthPercent / 100)`.
  - Applies to **both the snapshot and subsequent updates**. The band refreshes from the latest known best ask / best bid (snapshots, plus any `bestAsk` / `bestBid` entries on update events).
  - If a side's anchor is unknown (e.g. an empty side at snapshot time), that side is **not filtered** until an anchor arrives (degrades open).
- **`fetchSnapshot`** *(bool, default `true`)* — when `false` AND this subscriber is the first to ask for the ticker, the exchange REST snapshot fetch is skipped — only the WS delta feed is subscribed, and no `orderbook_snapshot` is emitted to this subscriber. Useful for mass-subscribing to 100+ tickers without hitting exchange REST rate limits. Seed state separately via `GET /api/connections/{id}/orderbook-snapshot` when needed. If a later subscriber requests a snapshot (or the UI joins), it is fetched lazily and delivered to all subscribers.
- `bestAsk` / `bestBid` payload fields are **never filtered** — they always represent best of book.

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

**User level subscriptions** — subscribe globally (no data payload) to receive user (plain) level lifecycle events (placed, updated, removed):

| Type | Data | Description |
|---|---|---|
| `user_level_subscribe` | `{}` | Subscribe to user level events. Receives an initial snapshot of all user levels, then live events. Idempotent. |
| `user_level_unsubscribe` | `{}` | Stop receiving user level events. Idempotent. |

**Chart annotation subscriptions** — subscribe per `(connectionId, ticker)` to receive the current shapes. This family delivers a subscribe/unsubscribe ack and a **one-shot snapshot only** — there is no `annotations_updated` push (the annotation service exposes no change-notification mechanism). A client that needs fresh state after an edit re-subscribes or re-issues `GET .../annotations`:

| Type | Data | Description |
|---|---|---|
| `annotation_subscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Acknowledge + one-shot `annotations_snapshot` for that `(connectionId, ticker)`. If the ticker is unknown on the connection or its price increment is ≤ 0, the server emits an `error` frame instead of the snapshot. |
| `annotation_unsubscribe` | `{ "connectionId": 123, "ticker": "BTCUSDT" }` | Stop the annotation subscription for that `(connectionId, ticker)`. Idempotent. |

**UI change subscriptions** — subscribe globally (no data payload) to observe the open UI. On subscribe the server acknowledges with `ui_subscribed` and immediately pushes a `ui_snapshot` whose `Data` is identical to `GET /api/ui/state` (same camelCase shape). After that, a `ui_update` is pushed per observed change. Read-only — the server never opens, closes or mutates any UI element here:

| Type | Data | Description |
|---|---|---|
| `ui_subscribe` | `{}` | Subscribe to UI change events. Receives a `ui_snapshot`, then `ui_update` events. Idempotent. |
| `ui_unsubscribe` | `{}` | Stop receiving UI change events. Idempotent. |

**MetaBroker analytics subscriptions (density map, large trades, liquidations)** — app-wide market-intelligence feeds relayed from the MetaBroker backend: the same data the terminal's Density Map, Large Trades and Liquidations windows show. No connection ID required. Each socket holds at most **one subscription per feed** — re-subscribing REPLACES the config (no unsubscribe needed to change filters), and each socket gets its own upstream feed, independent of the windows. **No MetaBroker login is needed** for any of the three: the liquidations upstream is the screener-v2 hub signed with the shared service key, the same connection the density map and large trades streams use:

| Type | Data | Description |
|---|---|---|
| `density_map_subscribe` | `{ "exchangeMarkets": [{ "exchange": "binance", "market": "futures", "bdsMode": "auto", "bdsValue": 1000000 }], "largeCoefficient": 3, "mediumCoefficient": 2, "smallCoefficient": 1, "largeLifetimeMinutes": 5, "mediumLifetimeMinutes": 5, "smallLifetimeMinutes": 5, "includedQuoteAssets": ["USDT"] }` | Subscribe to density-wall notifications (each wall reported once, on first sight of its id; a wall id is delivered at most once per subscription, in the snapshot or in an update, even when the feed removes and later re-broadcasts it). `exchangeMarkets` is required and must be non-empty (`[]` is rejected); every other field is optional with the defaults shown. Enums accept names or wire numbers — `exchange`: binance, gate, bybit, kucoin, bitget, mexc, okx, bingx, htx, bitmart, lbank, hyperliquid, upbit, asterdex, lighter, xt, edgex, bitunix, ourbit, whitebit, blofin, weex, polymarket; `market`: spot \| futures; `bdsMode`: manual \| auto (default manual; `bdsValue` default 1000000 USD). A `density_map_snapshot` follows the ack, then `density_map_update` batches. Re-subscribing replaces the config: the server acks with `density_map_subscribed` and sends a fresh `density_map_snapshot` of the walls that match the new filters; walls already notified are not replayed as updates. |
| `density_map_unsubscribe` | `{}` | Stop the density map stream (tears down the upstream feed). Idempotent. |
| `large_trades_subscribe` | `{ "exchangeMarkets": [{ "exchange": "binance", "market": "futures" }], "aggregationMs": 500, "minAmountUsd": 100000, "largeCoefficient": 3, "mediumCoefficient": 2, "smallCoefficient": 1, "includedQuoteAssets": ["USDT", "USDC", "OTHER"] }` | Subscribe to aggregated large trade prints. No snapshot — rows are final, append-only; history starts at subscribe time. `aggregationMs` 0–60000 (default 500; `0` = every raw print individually; out of range is refused with an `error` frame naming the field); optional `minAmountUsd` floor; same `exchangeMarkets` shape and defaults as the density feed. |
| `large_trades_unsubscribe` | `{}` | Stop the large trades stream. Idempotent. |
| `liquidations_subscribe` | `{ "exchanges": ["binance", "bybit", "okx", "bitget", "gate", "htx", "aster", "lighter"], "minNotionalUsd": 1000, "minImpactBps": null, "assetClass": "all", "side": "all", "coin": "", "window": "h1", "backfill": 200 }` | Subscribe to the cross-exchange liquidations feed (futures only). **No MetaBroker login needed.** `exchanges` is required and must be non-empty; `window` (m5 \| m15 \| h1 \| h4 \| h24) affects `totals` / `topTokens` only, never the rows; `backfill` (0–500; out of range is refused with an `error` frame naming the field) is the snapshot row count; `side` filters by the side of the **liquidated** position; `coin` is a case-insensitive prefix on the resolved coin (`BTC`, not `BTCUSDT`). A `liquidations_snapshot` follows every subscribe/replace. |
| `liquidations_unsubscribe` | `{}` | Stop the liquidations stream. Idempotent. |

#### Messages you receive

##### Acknowledgements

| Type | Data | When |
|---|---|---|
| `subscribed` | `{ "ConnectionId": 123 }` | After successful connection subscribe |
| `unsubscribed` | `{ "ConnectionId": 123 }` | After successful connection unsubscribe |
| `trade_subscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT", "ZoomIndex": 1 }` | After successful trade subscribe |
| `trade_unsubscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful trade unsubscribe |
| `orderbook_subscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT", "ZoomIndex": 0, "DepthLevels": 50, "DepthPercent": 0.5 }` | After successful order book subscribe. Echoes any non-null `DepthLevels` / `DepthPercent`. |
| `orderbook_unsubscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful order book unsubscribe |
| `mark_price_subscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful mark price subscribe |
| `mark_price_unsubscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful mark price unsubscribe |
| `index_price_subscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful index price subscribe |
| `index_price_unsubscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful index price unsubscribe |
| `funding_subscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful funding subscribe |
| `funding_unsubscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful funding unsubscribe |
| `notification_subscribed` | `{}` | After successful notification subscribe (a `notification_snapshot` follows immediately) |
| `notification_unsubscribed` | `{}` | After successful notification unsubscribe |
| `signal_level_subscribed` | `{}` | After successful signal level subscribe (a `signal_levels_snapshot` follows immediately) |
| `signal_level_unsubscribed` | `{}` | After successful signal level unsubscribe |
| `user_level_subscribed` | `{}` | After successful user level subscribe (a `user_levels_snapshot` follows immediately) |
| `user_level_unsubscribed` | `{}` | After successful user level unsubscribe |
| `annotation_subscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful annotation subscribe (an `annotations_snapshot` follows immediately) |
| `annotation_unsubscribed` | `{ "ConnectionId": 123, "Ticker": "BTCUSDT" }` | After successful annotation unsubscribe |
| `ui_subscribed` | `{}` | After successful UI subscribe (a `ui_snapshot` follows immediately) |
| `ui_unsubscribed` | `{}` | After successful UI unsubscribe |
| `density_map_subscribed` | `{}` | After successful density map subscribe (a `density_map_snapshot` follows immediately) |
| `density_map_unsubscribed` | `{}` | After successful density map unsubscribe |
| `large_trades_subscribed` | `{}` | After successful large trades subscribe (no snapshot — updates follow as trades happen) |
| `large_trades_unsubscribed` | `{}` | After successful large trades unsubscribe |
| `liquidations_subscribed` | `{}` | After successful liquidations subscribe (a `liquidations_snapshot` follows immediately) |
| `liquidations_unsubscribed` | `{}` | After successful liquidations unsubscribe |
| `error` | `{ "error": "..." }` | Invalid message, unknown type, bad connection ID, or missing ticker |

> Acknowledgement echoes are emitted with **PascalCase** field names (they echo the parsed request), unlike the camelCase update payloads below. The `orderbook_subscribed` ack echoes any non-null `DepthLevels` / `DepthPercent`, and echoes `FetchSnapshot` only when it was sent as `false`.

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
      { "price": 65123.50, "size": 0.15, "side": "Buy", "time": "2026-03-16T12:00:01.234+00:00", "highPrice": 65124.00, "lowPrice": 65123.00 },
      { "price": 65123.00, "size": 0.03, "side": "Sell", "time": "2026-03-16T12:00:01.235+00:00", "highPrice": 65123.00, "lowPrice": 65123.00 }
    ]
  }
}
```

| Field | Type | Description |
|---|---|---|
| `connectionId` | integer | Connection this trade data belongs to |
| `ticker` | string | Trading pair symbol |
| `trades` | array | Array of trades in this update |
| `trades[].price` | decimal | Trade price (the latest merged trade when aggregated) |
| `trades[].size` | decimal | Trade size (summed when aggregated) |
| `trades[].side` | string | `"Buy"` or `"Sell"` |
| `trades[].time` | string (ISO) | Trade timestamp |
| `trades[].highPrice` | decimal | Highest price among the trades merged into this entry |
| `trades[].lowPrice` | decimal | Lowest price among the trades merged into this entry |

> **Trade aggregation.** Trades are aggregated server-side using the order book's `AddingTicksForAPeriod` setting (the same value that drives the UI ticks section, default `200` ms; per-(connection, ticker)). Consecutive same-side trades that arrive within the window are merged into one entry — `size` is summed, `price` and `time` track the latest merged trade. A new entry is emitted when the side changes or the window expires. Each entry also carries `highPrice` and `lowPrice` — the highest and lowest price among the trades merged into that entry (the range the price travelled inside the tick), while `price` stays the latest merged trade. Set `AddingTicksForAPeriod = 0` in the order book settings to disable aggregation and receive the raw exchange stream — then every entry has `highPrice == lowPrice == price`. Changes to this setting are picked up live by active subscriptions. When subscribed with `zoomIndex > 1`, entries are re-grouped by zoomed price level and `highPrice` / `lowPrice` are the union of the merged entries' ranges.

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
| `bestAsk` | object | Best (lowest) ask price level |
| `bestBid` | object | Best (highest) bid price level |
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

**Index price update** — sent when the index price changes for a subscribed ticker (futures only):

```json
{
  "Type": "index_price_update",
  "Data": {
    "connectionId": 1,
    "ticker": "BTCUSDT",
    "indexPrice": 65120.8
  }
}
```

| Field | Type | Description |
|---|---|---|
| `connectionId` | integer | Connection this update belongs to |
| `ticker` | string | Trading pair symbol |
| `indexPrice` | decimal | Current index price |

> Like mark price, index price is only published on markets that expose an index price stream (typically futures). No initial snapshot — only live updates after the subscribe ack.

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

**Notification update** — pushed when new notifications arrive (~1 second batches). Covers both native notifications and rows injected via `POST /api/notifications` (for injected rows, `type` carries the supplied `EventType` string verbatim):

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
| *(custom)* | Any free-text event type supplied to `POST /api/notifications` |

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

**Signal levels snapshot** — sent once after `signal_level_subscribe`, contains all signal levels. Every level also carries `note` and the seven appearance fields (`null` when unset) — see [Note and appearance](#level-note-and-appearance):

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
        "triggerRule": "GreaterThanEqual",
        "note": "breakout watch",
        "lineThickness": 2.0,
        "lineStyle": "Dashed",
        "lineColor": "#FFAA00",
        "textSize": null,
        "textColor": null,
        "textAlignment": null,
        "textStyle": null
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
    "triggerRule": "GreaterThanEqual",
    "note": "breakout watch",
    "lineThickness": 2.0,
    "lineStyle": "Dashed",
    "lineColor": "#FFAA00",
    "textSize": null,
    "textColor": null,
    "textAlignment": null,
    "textStyle": null
  }
}
```

**Signal level updated** — pushed when a signal level is modified in place via `PUT .../signal-levels/{Id}` (price and/or rule changed, identity preserved). Same shape as `signal_level_placed`:

```json
{
  "Type": "signal_level_updated",
  "Data": {
    "id": 1,
    "connectionId": 1,
    "ticker": "BTCUSDT",
    "price": 96000.00,
    "isTriggered": false,
    "triggerTime": null,
    "triggerRule": "GreaterThanEqual",
    "note": "breakout watch",
    "lineThickness": 2.0,
    "lineStyle": "Dashed",
    "lineColor": "#FFAA00",
    "textSize": null,
    "textColor": null,
    "textAlignment": null,
    "textStyle": null
  }
}
```

**Signal level triggered** — pushed when a signal level is triggered by market price:

```json
{
  "Type": "signal_level_triggered",
  "Data": {
    "id": 1,
    "triggerTime": "2026-04-13T10:30:00+00:00"
  }
}
```

**Signal level removed** — pushed when a single signal level is removed:

```json
{
  "Type": "signal_level_removed",
  "Data": { "id": 1 }
}
```

**Signal levels removed all** — pushed when all signal levels (for a ticker or globally) are cleared:

```json
{
  "Type": "signal_levels_removed_all",
  "Data": {}
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
| `note` + appearance | — | The note and the seven appearance fields, `null` when unset — see [Note and appearance](#level-note-and-appearance) |

**User levels snapshot** — sent once after `user_level_subscribe`, contains all user levels. `date` is a nullable ISO-8601 timestamp. Every level also carries `note` and the seven appearance fields (`null` when unset) — see [Note and appearance](#level-note-and-appearance):

```json
{
  "Type": "user_levels_snapshot",
  "Data": {
    "userLevels": [
      {
        "id": 1,
        "connectionId": 1,
        "ticker": "BTCUSDT",
        "price": 95000.00,
        "name": "13.04.2026",
        "note": "watch this one",
        "date": null,
        "lineThickness": 2.0,
        "lineStyle": "Dashed",
        "lineColor": "#FFAA00",
        "textSize": null,
        "textColor": null,
        "textAlignment": null,
        "textStyle": null
      }
    ]
  }
}
```

**User level placed** — pushed when a new user level is created (by hand or via the API):

```json
{
  "Type": "user_level_placed",
  "Data": {
    "id": 1,
    "connectionId": 1,
    "ticker": "BTCUSDT",
    "price": 95000.00,
    "name": "13.04.2026",
    "note": "watch this one",
    "date": null,
    "lineThickness": 2.0,
    "lineStyle": "Dashed",
    "lineColor": "#FFAA00",
    "textSize": null,
    "textColor": null,
    "textAlignment": null,
    "textStyle": null
  }
}
```

**User level updated** — pushed when a user level is modified in place (identity preserved). Same shape as `user_level_placed`.

**User level removed** — pushed when one or more user levels are deleted. Removal is a batch (covers both `DELETE .../{Id}` and `DELETE ?Ticker=`), so the ids are carried as an array:

```json
{
  "Type": "user_level_removed",
  "Data": { "ids": [1, 2] }
}
```

**User levels removed all** — pushed when all user levels (globally) are cleared:

```json
{
  "Type": "user_levels_removed_all",
  "Data": {}
}
```

| User Level Field | Type | Description |
|---|---|---|
| `id` | integer | User level ID |
| `connectionId` | integer | Connection this level belongs to |
| `ticker` | string | Trading pair symbol |
| `price` | decimal | Level price |
| `name` | string | Label text |
| `date` | string (ISO)? | Optional timestamp (null for a plain level) |

**Annotations snapshot** — sent once after `annotation_subscribe` for that `(connectionId, ticker)`. One-shot: it is **not** re-pushed on change (there is no `annotations_updated` event — re-subscribe or re-issue `GET .../annotations` for fresh state). Same shape and the same **venue prices** as `GET .../annotations`:

```json
{
  "Type": "annotations_snapshot",
  "Data": {
    "connectionId": 1,
    "ticker": "BTCUSDT",
    "lineAnnotations": [
      { "index": 0, "x1": "2026-04-13T10:00:00", "x2": "2026-04-13T12:00:00", "y1": 95000.0, "y2": 96500.0 }
    ],
    "horizontalLineAnnotations": [
      { "index": 0, "y": 94000.0 }
    ],
    "horizontalRayAnnotations": [
      { "index": 0, "y": 97000.0, "x1": "2026-04-13T11:30:00" }
    ]
  }
}
```

> The `y` / `y1` / `y2` values are venue prices, identical to what `GET .../annotations` returns. If the `ticker` is unknown on the connection or its price increment is ≤ 0, the server emits an `error` frame instead of the snapshot (a socket cannot answer 400) — it never silently returns raw chart axis units.

**UI snapshot** — sent once after `ui_subscribe`. Its `Data` is identical to the `GET /api/ui/state` response (same camelCase shape):

```json
{
  "Type": "ui_snapshot",
  "Data": {
    "windows": [ ... ],
    "standaloneCharts": [ ... ]
  }
}
```

**UI update** — pushed per observed UI change after `ui_subscribe`. The payload carries a `kind` plus kind-specific fields:

| `kind` | When | Data fields |
|---|---|---|
| `tabAdded` | A tab was added to a window | `kind`, `windowId`, `window` (the full window subtree, same shape as one entry of `windows[]`) |
| `documentOpened` | A chart document was opened | `kind`, `documentId`, `externalId`, `connectionId` (null when unbound), `ticker`, `document` (same shape as a state document, `kind: "chart"`) |

```json
{
  "Type": "ui_update",
  "Data": {
    "kind": "tabAdded",
    "windowId": 3,
    "window": { "id": 3, "type": "Workspace", "geometry": { ... }, "monitor": null, "tabs": [ ... ] }
  }
}
```

> **Coverage.** The API observes only Application-layer events, so exactly the two `ui_update` kinds above are delivered today. The following changes are **not** emitted yet (and no polling is used): order-book document opened, ticker changed, window opened/closed, tab removed/activated, document closed/moved, and link-number changes. Clients that need the current state after such a change can re-request `GET /api/ui/state`.

**Density map snapshot / update** — `density_map_snapshot` carries the first-seen walls of the initial upstream snapshot (may be empty — it resolves the loading state); `density_map_update` carries newly seen walls (each wall is reported exactly once, on first sight of its id). Both use the same `notifications[]` shape:

```json
{
  "Type": "density_map_update",
  "Data": {
    "notifications": [
      {
        "id": "0d9c1a9e-7c31-4f0b-9a55-2f6f2b8f11aa",
        "exchange": "Binance",
        "exchangeLogo": "Binance.png",
        "market": "futures",
        "ticker": "BTCUSDT",
        "side": "bid",
        "price": 62000.5,
        "distancePercent": -1.35,
        "sizeUsd": 780000,
        "time": "2026-08-15T12:00:00+00:00"
      }
    ]
  }
}
```

`side` is `ask` (sell wall) or `bid` (buy wall); `distancePercent` is the wall's distance from the current price (±, capped ±10); `market` is `spot` or `futures`.

**Large trades update** — aggregated trade prints, append-only (no snapshot; rows are never updated or removed):

```json
{
  "Type": "large_trades_update",
  "Data": {
    "trades": [
      {
        "id": "5b2f7c31-9a55-4f0b-8f11-0d9c1a9eaa22",
        "exchange": "Binance",
        "exchangeLogo": "Binance.png",
        "market": "futures",
        "ticker": "BTCUSDT",
        "side": "buy",
        "minPrice": 62712.9,
        "maxPrice": 62736.7,
        "sizeUsd": 188139.4,
        "tradeCount": 17,
        "category": "medium",
        "time": "2026-08-15T12:00:00+00:00"
      }
    ]
  }
}
```

`minPrice` / `maxPrice` span the prints merged into the aggregation window (`minPrice == maxPrice` and `tradeCount == 1` when `aggregationMs` is `0`); `category` is `small` / `medium` / `large` per the configured coefficients.

**Liquidations snapshot / update / metadata** — `liquidations_snapshot` follows every subscribe/replace (backfill rows + aggregates; may be empty), `liquidations_update` carries live rows batched newest-first, and `liquidations_metadata` refreshes the aggregates on every upstream map tick (sub-second) for the configured `window`:

```json
{
  "Type": "liquidations_snapshot",
  "Data": {
    "liquidations": [
      {
        "time": "2026-08-15T12:00:00+00:00",
        "exchange": "bybit",
        "symbol": "BTCUSDT",
        "coin": "BTC",
        "side": "short",
        "assetClass": "crypto",
        "price": 64230.5,
        "size": 0.012,
        "notionalUsd": 770.77,
        "impactBps": 0.01
      }
    ],
    "totals": { "longUsd": 1250430.55, "shortUsd": 84210.0, "longCount": 37, "shortCount": 4 },
    "topTokens": [
      { "token": "BTC", "longUsd": 900000.0, "shortUsd": 50000.0, "longCount": 20, "shortCount": 2 }
    ]
  }
}
```

`side` is the side of the **liquidated** position (`long` = longs got liquidated, price fell); `impactBps` is `null` when the market's 24h turnover is unknown; `totals` may be `null`. `liquidations_update` carries only `liquidations[]`; `liquidations_metadata` carries only `totals` + `topTokens[]` (top 10 tokens by combined USD, biggest first).

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
| Mark price / index price / funding subscribe (valid) | Server responds `mark_price_subscribed` / `index_price_subscribed` / `funding_subscribed`. Updates start flowing as the exchange publishes them (no initial snapshot). |
| Mark price / index price / funding subscribe (invalid) | Server responds `error`. No subscription created. |
| Mark price / index price / funding subscribe (duplicate) | Idempotent — responds with confirmation, no duplicate events. |
| Mark price / index price / funding unsubscribe | Server responds `mark_price_unsubscribed` / `index_price_unsubscribed` / `funding_unsubscribed`. Updates stop for that connection + ticker. |
| Notification subscribe | Server responds `notification_subscribed`. Sends snapshot of recent notifications, then live updates. |
| Notification unsubscribe | Server responds `notification_unsubscribed`. Notification updates stop. |
| Signal level subscribe | Server responds `signal_level_subscribed`. Sends snapshot of all signal levels, then live events (placed, updated, triggered, removed). |
| Signal level unsubscribe | Server responds `signal_level_unsubscribed`. Signal level updates stop. |
| User level subscribe | Server responds `user_level_subscribed`. Sends snapshot of all user levels, then live events (placed, updated, removed). |
| User level unsubscribe | Server responds `user_level_unsubscribed`. User level updates stop. |
| Annotation subscribe (valid) | Server responds `annotation_subscribed`, immediately followed by a one-shot `annotations_snapshot`. No further pushes — re-subscribe for fresh state. |
| Annotation subscribe (unknown ticker / bad increment) | Server responds `annotation_subscribed`, then emits an `error` frame instead of the snapshot. |
| UI subscribe | Server responds `ui_subscribed`. Sends a `ui_snapshot`, then `ui_update` events per observed change. |
| UI unsubscribe | Server responds `ui_unsubscribed`. UI updates stop. |
| Client disconnects | All subscriptions (connection-level, market data, notifications, signal levels, user levels, annotations, UI) are cleaned up automatically. Exchange market data subscriptions are released when no more clients need them. |
| Multiple connections | A single client can subscribe to multiple connection IDs simultaneously. |
| Multiple tickers | A single client can subscribe to trades/order book for multiple tickers on the same or different connections. |
| Multiple clients | Multiple clients can subscribe to the same connection ID or ticker. Each receives its own copy of events. |

#### Error messages

| Condition | Error message |
|---|---|
| Subscribe to non-existent connection | `Connection {id} not found or not active` |
| Trade/orderbook subscribe with missing ticker | `Ticker is required for trade subscription` / `Ticker is required for order book subscription` |
| Trade/orderbook subscribe with invalid connection | `Connection {id} not found or not active` |
| Mark price / index price / funding subscribe with missing ticker | `Ticker is required for mark price subscription` / `Ticker is required for index price subscription` / `Ticker is required for funding subscription` |
| Mark price / index price / funding subscribe with invalid connection | `Connection {id} not found or not active` |
| Annotation subscribe with missing ticker | `Ticker is required for annotation subscription` |
| Annotation subscribe with invalid connection | `Connection {id} not found or not active` |
| Annotation subscribe with unknown ticker or price increment ≤ 0 | `Ticker '{ticker}' not found on connection {connectionId}` (emitted instead of the snapshot) |
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
| 28    | Phemex      | |
| 29    | Dydx        | |
| 30    | Bitfinex    | |
| 31    | Coinbase    | |
| 32    | Kraken      | |
| 33    | ApexOmni    | |
| 34    | Toobit      | |
| 35    | OrangeX     | |
| 36    | Bitmex      | |
| 37    | Bitrue      | |
| 38    | Bithumb     | |
| 39    | CoinW       | |
| 40    | Gemini      | |
| 41    | Grvt        | |
| 42    | Polymarket  | |

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
| 10    | Alpha          | Binance Alpha market |
| 11    | Bond           | Bond markets |
| 12    | Currency       | Currency markets |
| 13    | Prediction     | Prediction markets (e.g. Polymarket) |

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

async function getTickers(port, ConnectionId, refresh = false) {
  const qs = refresh ? '?Refresh=true' : '';
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/tickers${qs}`);
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

async function getClusterSnapshot(port, ConnectionId, ticker, timeFrame, zoomIndex = 1, columns = 0) {
  const params = new URLSearchParams({ Ticker: ticker, TimeFrame: timeFrame, ZoomIndex: zoomIndex });
  if (columns > 0) params.set("Columns", columns); // history depth, up to 100 (default: backend page of 10)
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/cluster-snapshot?${params}`);
  return r.json();
}

async function getSignalLevels(port, ConnectionId, ticker) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/signal-levels?Ticker=${ticker}`);
  return r.json();
}

async function placeSignalLevel(port, ConnectionId, ticker, price) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/signal-levels`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ Ticker: ticker, Price: price })
  });
  return r.json();
}

async function removeSignalLevel(port, ConnectionId, signalLevelId) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/signal-levels/${signalLevelId}`, {
    method: "DELETE"
  });
  return r.json();
}

async function getOrderBookSettings(port, ConnectionId, ticker) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/orderbook-settings?Ticker=${ticker}`);
  return r.json();
}

async function updateOrderBookSettings(port, ConnectionId, ticker, settings) {
  const r = await fetch(`http://127.0.0.1:${port}/api/connections/${ConnectionId}/orderbook-settings?Ticker=${ticker}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(settings)
  });
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
  const { connections } = await getConnections(port);
  const conn = connections[0]; // pick first connection (items are PascalCase: conn.Id, conn.Name, …)

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

  // Signal levels
  const levels = await getSignalLevels(port, conn.Id, "BTCUSDT");
  await placeSignalLevel(port, conn.Id, "BTCUSDT", 95000.00);
  await removeSignalLevel(port, conn.Id, 1);

  // Order book settings
  const obSettings = await getOrderBookSettings(port, conn.Id, "BTCUSDT");
  await updateOrderBookSettings(port, conn.Id, "BTCUSDT", { LargeAmountUsd: 50000, RowHeight: 14 });

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

def get_tickers(port, connection_id, refresh=False):
    params = {"Refresh": "true"} if refresh else {}
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/tickers", params=params)
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

def get_cluster_snapshot(port, connection_id, ticker, time_frame, zoom_index=1, columns=0):
    params = {"Ticker": ticker, "TimeFrame": time_frame, "ZoomIndex": zoom_index}
    if columns > 0:
        params["Columns"] = columns  # history depth, up to 100 (default: backend page of 10)
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/cluster-snapshot",
                     params=params)
    return r.json()

def get_signal_levels(port, connection_id, ticker):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/signal-levels",
                     params={"Ticker": ticker})
    return r.json()

def place_signal_level(port, connection_id, ticker, price):
    payload = {"Ticker": ticker, "Price": price}
    r = requests.post(f"http://127.0.0.1:{port}/api/connections/{connection_id}/signal-levels",
                      json=payload)
    return r.json()

def remove_signal_level(port, connection_id, signal_level_id):
    r = requests.delete(f"http://127.0.0.1:{port}/api/connections/{connection_id}/signal-levels/{signal_level_id}")
    return r.json()

def get_orderbook_settings(port, connection_id, ticker):
    r = requests.get(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orderbook-settings",
                     params={"Ticker": ticker})
    return r.json()

def update_orderbook_settings(port, connection_id, ticker, **settings):
    r = requests.put(f"http://127.0.0.1:{port}/api/connections/{connection_id}/orderbook-settings",
                     params={"Ticker": ticker}, json=settings)
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
    conn = data["connections"][0]  # pick first connection (items are PascalCase: conn["Id"], conn["Name"], …)

    # Query data
    tickers = get_tickers(port, conn["Id"])
    balance = get_balance(port, conn["Id"])
    orders = get_open_orders(port, conn["Id"], "BTCUSDT")
    positions = get_positions(port, conn["Id"])

    # Place a limit buy order
    result = place_order(port, conn["Id"], "BTCUSDT", side=1, price=65000.00, size=0.01)

    # Get cluster snapshot (5-minute timeframe, 2x zoom)
    clusters = get_cluster_snapshot(port, conn["Id"], "BTCUSDT", "M5", zoom_index=2)

    # Signal levels
    levels = get_signal_levels(port, conn["Id"], "BTCUSDT")
    place_signal_level(port, conn["Id"], "BTCUSDT", price=95000.00)
    remove_signal_level(port, conn["Id"], signal_level_id=1)

    # Order book settings
    ob_settings = get_orderbook_settings(port, conn["Id"], "BTCUSDT")
    update_orderbook_settings(port, conn["Id"], "BTCUSDT", LargeAmountUsd=50000, RowHeight=14)

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
      // { connectionId, orderId, ticker, side, type, price, filledPrice, size, filledSize, fee, feeCurrency, status, time }
      break;
    case "position_update":
      console.log("Position update:", msg.Data);
      // { connectionId, positionId, ticker, side, size, avgPrice, avgPriceFix, avgPriceDyn, status }
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
      // { connectionId, ticker, trades: [{ price, size, side, time, highPrice, lowPrice }] }
      break;
    case "orderbook_snapshot":
      console.log("Order book snapshot:", msg.Data);
      // { connectionId, ticker, asks: [...], bids: [...], bestAsk, bestBid }
      break;
    case "orderbook_update":
      console.log("Order book update:", msg.Data);
      // { connectionId, ticker, updates: [{ price, size, type }] }
      break;
    case "mark_price_update":
      console.log("Mark price update:", msg.Data);
      // { connectionId, ticker, markPrice }
      break;
    case "index_price_update":
      console.log("Index price update:", msg.Data);
      // { connectionId, ticker, indexPrice }
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
                # { connectionId, ticker, trades: [{ price, size, side, time, highPrice, lowPrice }] }
            elif msg_type == "orderbook_snapshot":
                print(f"Order book snapshot: {len(msg['Data'].get('asks', []))} asks, {len(msg['Data'].get('bids', []))} bids")
                # { connectionId, ticker, asks, bids, bestAsk, bestBid }
            elif msg_type == "orderbook_update":
                print(f"Order book update: {len(msg['Data'].get('updates', []))} levels changed")
                # { connectionId, ticker, updates: [{ price, size, type }] }
            elif msg_type == "mark_price_update":
                print(f"Mark price: {msg['Data']['ticker']} = {msg['Data']['markPrice']}")
                # { connectionId, ticker, markPrice }
            elif msg_type == "index_price_update":
                print(f"Index price: {msg['Data']['ticker']} = {msg['Data']['indexPrice']}")
                # { connectionId, ticker, indexPrice }
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
                print(f"Error: {msg['Data']['error']}")

asyncio.run(listen_updates(connection_id=1, ticker="BTCUSDT"))
```
