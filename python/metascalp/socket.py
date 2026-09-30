"""MetaScalp WebSocket client for real-time updates."""

from __future__ import annotations

import asyncio
import json
from typing import Any, Callable, Optional

import websockets
from websockets.client import WebSocketClientProtocol

WS_PORT_START = 17845
WS_PORT_END = 17855


def _normalize(obj: Any) -> Any:
    """Normalize PascalCase keys from server to camelCase."""
    if obj is None:
        return obj
    if isinstance(obj, list):
        return [_normalize(item) for item in obj]
    if isinstance(obj, dict):
        return {k[0].lower() + k[1:]: _normalize(v) for k, v in obj.items()}
    return obj


class MetaScalpSocket:
    """WebSocket client for MetaScalp real-time updates.

    Supports connection-level subscriptions (orders, positions, balances)
    and market data subscriptions (trades, order book) scoped by connectionId + ticker.

    Usage:
        socket = await MetaScalpSocket.discover()

        @socket.on('trade_update')
        def on_trade(data):
            print(data)

        socket.subscribe(connection_id)
        socket.subscribe_trades(connection_id, 'BTCUSDT')
        await socket.listen_forever()
    """

    def __init__(self, port: int):
        self.port = port
        self._ws: Optional[WebSocketClientProtocol] = None
        self._listeners: dict[str, list[Callable]] = {}
        self._connected = False

    @classmethod
    async def discover(cls, timeout: float = 1.0) -> MetaScalpSocket:
        """Scan ports 17845-17855 to find the MetaScalp WebSocket server."""
        for port in range(WS_PORT_START, WS_PORT_END + 1):
            try:
                ws = await asyncio.wait_for(
                    websockets.connect(f"ws://127.0.0.1:{port}/"),
                    timeout=timeout,
                )
                socket = cls(port)
                socket._ws = ws
                socket._connected = True
                return socket
            except (ConnectionRefusedError, OSError, asyncio.TimeoutError):
                continue
        raise ConnectionError(
            f"MetaScalp WebSocket not found on ports {WS_PORT_START}-{WS_PORT_END}"
        )

    @property
    def connected(self) -> bool:
        return self._connected

    async def connect(self, timeout: float = 5.0) -> None:
        """Connect to the WebSocket server."""
        self._ws = await asyncio.wait_for(
            websockets.connect(f"ws://127.0.0.1:{self.port}/"),
            timeout=timeout,
        )
        self._connected = True

    async def disconnect(self) -> None:
        """Disconnect from the WebSocket server."""
        if self._ws:
            await self._ws.close()
        self._ws = None
        self._connected = False

    # ---- Connection-level subscriptions ----
    # Use these to receive order, position, balance, and finres updates
    # for ALL tickers on a connection.
    # Events: 'order_update', 'position_update', 'balance_update', 'finres_update'

    def subscribe(self, connection_id: int) -> None:
        """Subscribe to order, position, balance, and finres updates for a connection.

        This covers ALL tickers on the connection.

        Events you'll receive:
        - 'order_update' — order created/modified/filled/cancelled
        - 'position_update' — position opened/changed/closed
        - 'balance_update' — account balances changed
        - 'finres_update' — financial results recalculated
        """
        self._send("subscribe", {"connectionId": connection_id})

    def unsubscribe(self, connection_id: int) -> None:
        """Unsubscribe from connection-level updates."""
        self._send("unsubscribe", {"connectionId": connection_id})

    # ---- Market data subscriptions ----
    # Use these to receive real-time market data for a SPECIFIC ticker on a connection.
    # These are independent from subscribe() — you can use one without the other.
    # Events: 'trade_update', 'orderbook_snapshot', 'orderbook_update'

    def subscribe_trades(self, connection_id: int, ticker: str) -> None:
        """Subscribe to real-time trade updates for a specific ticker.

        Independent from subscribe() — only sends trade data for this exact ticker.

        Event: 'trade_update'

        Trades are aggregated server-side using the order book's `AddingTicksForAPeriod`
        setting (per-(connection, ticker), default 200 ms). Consecutive same-side trades
        inside the window are merged into one entry — `size` is summed, `price` and `time`
        track the latest merged trade. Set `AddingTicksForAPeriod = 0` in the order book
        settings to receive the raw exchange stream.
        """
        self._send("trade_subscribe", {"connectionId": connection_id, "ticker": ticker})

    def unsubscribe_trades(self, connection_id: int, ticker: str) -> None:
        """Unsubscribe from trade updates for a specific ticker."""
        self._send("trade_unsubscribe", {"connectionId": connection_id, "ticker": ticker})

    def subscribe_order_book(
        self,
        connection_id: int,
        ticker: str,
        zoom_index: int = 0,
        depth_levels: int | None = None,
        depth_percent: float | None = None,
        fetch_snapshot: bool = True,
    ) -> None:
        """Subscribe to order book updates for a specific ticker.

        When zoom_index is 0 (default), receives full order book + incremental updates.
        When zoom_index > 1, price levels are aggregated into zoomed buckets.

        Optional filters:
        - depth_levels (must be >= 1): trims the snapshot to the top N price levels per side
          (asks ascending, bids descending), applied AFTER zoom and depth_percent. Filters the
          snapshot ONLY — incremental updates are unaffected.
        - depth_percent (must be > 0): per-side band as a percentage, anchored on best ask /
          best bid (NOT mid). Asks kept where price <= bestAsk * (1 + depth_percent / 100);
          bids where price >= bestBid * (1 - depth_percent / 100). Applies to both the
          snapshot and subsequent updates; the band refreshes from the latest known best ask /
          best bid on each event. If a side's anchor is unknown, that side is not filtered
          (degrades open).
        - fetch_snapshot (default True): when False AND this subscriber is the first to ask for
          the ticker, the exchange REST snapshot fetch is skipped — only the WS delta feed is
          subscribed. Useful for mass-subscribing 100+ tickers without hitting exchange REST
          rate limits. Seed state separately via MetaScalpClient.get_order_book_snapshot()
          when needed. If a later subscriber requests a snapshot, it is fetched lazily and
          delivered to all subscribers.
        bestAsk / bestBid payload fields are never filtered.

        Events: 'orderbook_snapshot' (once, unless fetch_snapshot=False), then 'orderbook_update'
        """
        data: dict = {"connectionId": connection_id, "ticker": ticker, "zoomIndex": zoom_index}
        if depth_levels is not None:
            data["depthLevels"] = depth_levels
        if depth_percent is not None:
            data["depthPercent"] = depth_percent
        # Only emit fetchSnapshot when non-default, for compatibility with older servers
        if not fetch_snapshot:
            data["fetchSnapshot"] = False
        self._send("orderbook_subscribe", data)

    def unsubscribe_order_book(self, connection_id: int, ticker: str) -> None:
        """Unsubscribe from order book updates for a specific ticker."""
        self._send("orderbook_unsubscribe", {"connectionId": connection_id, "ticker": ticker})

    def subscribe_mark_price(self, connection_id: int, ticker: str) -> None:
        """Subscribe to mark price updates for a specific ticker (futures only).

        No initial snapshot — only live updates as the exchange publishes them.
        Spot connections will not emit any updates even after a successful subscribe.

        Event: 'mark_price_update'
        """
        self._send("mark_price_subscribe", {"connectionId": connection_id, "ticker": ticker})

    def unsubscribe_mark_price(self, connection_id: int, ticker: str) -> None:
        """Unsubscribe from mark price updates for a specific ticker."""
        self._send("mark_price_unsubscribe", {"connectionId": connection_id, "ticker": ticker})

    def subscribe_funding(self, connection_id: int, ticker: str) -> None:
        """Subscribe to funding rate updates for a specific ticker (perpetual futures only).

        No initial snapshot — only live updates as the exchange publishes them.
        Spot and dated-futures connections will not emit any updates even after a successful subscribe.

        Event: 'funding_update'
        """
        self._send("funding_subscribe", {"connectionId": connection_id, "ticker": ticker})

    def unsubscribe_funding(self, connection_id: int, ticker: str) -> None:
        """Unsubscribe from funding rate updates for a specific ticker."""
        self._send("funding_unsubscribe", {"connectionId": connection_id, "ticker": ticker})

    # ---- Notification subscriptions ----
    # App-wide notifications (trades, signal levels, large amounts, screener).
    # Independent from subscribe() — no connectionId required.
    # Events: 'notification_snapshot', 'notification_update'

    def subscribe_notifications(self) -> None:
        """Subscribe to app-wide notifications.

        Receives a snapshot of recent notifications, then live updates.
        Independent from subscribe() — no connectionId required.

        Events: 'notification_snapshot' (once), then 'notification_update' (continuous)
        """
        self._send("notification_subscribe", {})

    def unsubscribe_notifications(self) -> None:
        """Unsubscribe from notification updates."""
        self._send("notification_unsubscribe", {})

    # ---- Signal level subscriptions ----

    def subscribe_signal_levels(self) -> None:
        """Subscribe to signal level events."""
        self._send("signal_level_subscribe", {})

    def unsubscribe_signal_levels(self) -> None:
        """Unsubscribe from signal level events."""
        self._send("signal_level_unsubscribe", {})

    # ---- MetaBroker analytics streams ----
    # Density map / large trades / liquidations — app-wide feeds relayed from the MetaBroker
    # backend (the same data the terminal's analytics windows show). No connection_id required.
    # One subscription per feed per socket: re-subscribing REPLACES the config.

    def subscribe_density_map(
        self,
        exchange_markets: list[dict],
        large_coefficient: float | None = None,
        medium_coefficient: float | None = None,
        small_coefficient: float | None = None,
        large_lifetime_minutes: int | None = None,
        medium_lifetime_minutes: int | None = None,
        small_lifetime_minutes: int | None = None,
        included_quote_assets: list[str] | None = None,
    ) -> None:
        """Subscribe to the MetaBroker density map notifications stream.

        Order book walls, each reported exactly once on first sight of its id.

        exchange_markets is required and must be non-empty; each entry is a dict like
        {"exchange": "binance", "market": "futures"}. The base density size is always
        auto (MetaBroker MB-702): "bdsMode": "manual" or a "bdsValue" in an entry is refused
        with an error frame naming the field (MET-1828); "bdsMode": "auto" is accepted as a
        no-op. Those two fields belong to subscribe_large_trades(). All other arguments
        default to the terminal's Density Map window defaults (coefficients 3/2/1,
        lifetimes 5 min, quote assets ["USDT"]; valid assets: USDT, USDC, OTHER).
        Re-subscribing replaces the config and re-sends a `density_map_snapshot` of the new filter's walls, without replaying already-notified walls.

        Events: 'density_map_snapshot' (after the ack; may be empty — it resolves the
        loading state), then 'density_map_update' (continuous).
        """
        data: dict = {"exchangeMarkets": exchange_markets}
        if large_coefficient is not None:
            data["largeCoefficient"] = large_coefficient
        if medium_coefficient is not None:
            data["mediumCoefficient"] = medium_coefficient
        if small_coefficient is not None:
            data["smallCoefficient"] = small_coefficient
        if large_lifetime_minutes is not None:
            data["largeLifetimeMinutes"] = large_lifetime_minutes
        if medium_lifetime_minutes is not None:
            data["mediumLifetimeMinutes"] = medium_lifetime_minutes
        if small_lifetime_minutes is not None:
            data["smallLifetimeMinutes"] = small_lifetime_minutes
        if included_quote_assets is not None:
            data["includedQuoteAssets"] = included_quote_assets
        self._send("density_map_subscribe", data)

    def unsubscribe_density_map(self) -> None:
        """Stop the density map stream (tears down the upstream feed)."""
        self._send("density_map_unsubscribe", {})

    def subscribe_large_trades(
        self,
        exchange_markets: list[dict],
        aggregation_ms: int | None = None,
        min_amount_usd: float | None = None,
        large_coefficient: float | None = None,
        medium_coefficient: float | None = None,
        small_coefficient: float | None = None,
        included_quote_assets: list[str] | None = None,
    ) -> None:
        """Subscribe to the MetaBroker large trades stream.

        Aggregated trade prints, final and append-only — no snapshot; history starts at
        subscribe time. exchange_markets uses the same shape and defaults as
        subscribe_density_map(). aggregation_ms is 0-60000 (default 500; 0 = every raw
        print individually); quote assets default to ["USDT", "USDC", "OTHER"].
        Re-subscribing replaces the config.

        Event: 'large_trades_update'
        """
        data: dict = {"exchangeMarkets": exchange_markets}
        if aggregation_ms is not None:
            data["aggregationMs"] = aggregation_ms
        if min_amount_usd is not None:
            data["minAmountUsd"] = min_amount_usd
        if large_coefficient is not None:
            data["largeCoefficient"] = large_coefficient
        if medium_coefficient is not None:
            data["mediumCoefficient"] = medium_coefficient
        if small_coefficient is not None:
            data["smallCoefficient"] = small_coefficient
        if included_quote_assets is not None:
            data["includedQuoteAssets"] = included_quote_assets
        self._send("large_trades_subscribe", data)

    def unsubscribe_large_trades(self) -> None:
        """Stop the large trades stream."""
        self._send("large_trades_unsubscribe", {})

    def subscribe_liquidations(
        self,
        exchanges: list[str],
        min_notional_usd: float | None = None,
        min_impact_bps: float | None = None,
        asset_class: str | None = None,
        side: str | None = None,
        coin: str | None = None,
        window: str | None = None,
        backfill: int | None = None,
    ) -> None:
        """Subscribe to the MetaBroker cross-exchange liquidations stream (futures only).

        No MetaBroker login is needed: the upstream is the screener-v2 hub signed with
        the shared service key.

        exchanges is required and must be non-empty; valid names: binance, bybit, okx,
        bitget, gate, htx, aster, lighter. asset_class: 'all' | 'crypto' | 'tradfi'
        (default all). side filters by the side of the LIQUIDATED position: 'all' |
        'long' | 'short' (default all). coin is a case-insensitive prefix on the
        resolved coin ('BTC', not 'BTCUSDT'). window ('m5'|'m15'|'h1'|'h4'|'h24',
        default h1) affects totals / top tokens only, never the rows. backfill is the
        snapshot row count, 0-500 (default 200). Re-subscribing replaces the filters
        and the server re-sends a snapshot.

        Events: 'liquidations_snapshot' (after every subscribe/replace),
        'liquidations_update' (live rows, newest first), 'liquidations_metadata'
        (totals + top tokens, sub-second cadence).
        """
        data: dict = {"exchanges": exchanges}
        if min_notional_usd is not None:
            data["minNotionalUsd"] = min_notional_usd
        if min_impact_bps is not None:
            data["minImpactBps"] = min_impact_bps
        if asset_class is not None:
            data["assetClass"] = asset_class
        if side is not None:
            data["side"] = side
        if coin is not None:
            data["coin"] = coin
        if window is not None:
            data["window"] = window
        if backfill is not None:
            data["backfill"] = backfill
        self._send("liquidations_subscribe", data)

    def unsubscribe_liquidations(self) -> None:
        """Stop the liquidations stream."""
        self._send("liquidations_unsubscribe", {})

    # ---- Event handling ----

    def on(self, event: str) -> Callable:
        """Decorator to register an event listener.

        Usage:
            @socket.on('trade_update')
            def on_trade(data):
                print(data)
        """
        def decorator(fn: Callable) -> Callable:
            if event not in self._listeners:
                self._listeners[event] = []
            self._listeners[event].append(fn)
            return fn
        return decorator

    def add_listener(self, event: str, fn: Callable) -> None:
        """Register an event listener."""
        if event not in self._listeners:
            self._listeners[event] = []
        self._listeners[event].append(fn)

    def remove_listener(self, event: str, fn: Callable) -> None:
        """Remove an event listener."""
        if event in self._listeners:
            self._listeners[event] = [f for f in self._listeners[event] if f is not fn]

    # ---- Message loop ----

    async def listen_forever(self) -> None:
        """Listen for messages and dispatch to registered listeners.

        Blocks until the connection is closed. Call this after subscribing.
        """
        if not self._ws:
            raise RuntimeError("Not connected")

        try:
            async for raw in self._ws:
                try:
                    data = _normalize(json.loads(raw))
                    msg_type = data.get("type", "")
                    msg_data = data.get("data")
                    self._emit(msg_type, msg_data)
                except json.JSONDecodeError:
                    continue
        except websockets.ConnectionClosed:
            pass
        finally:
            self._connected = False

    async def listen_once(self, timeout: float = 30.0) -> tuple[str, Any]:
        """Wait for and return a single message as (type, data)."""
        if not self._ws:
            raise RuntimeError("Not connected")
        raw = await asyncio.wait_for(self._ws.recv(), timeout=timeout)
        data = _normalize(json.loads(raw))
        msg_type = data.get("type", "")
        msg_data = data.get("data")
        self._emit(msg_type, msg_data)
        return msg_type, msg_data

    # ---- Internals ----

    def _send(self, msg_type: str, data: dict) -> None:
        if not self._ws or not self._connected:
            raise RuntimeError("Not connected")
        asyncio.get_event_loop().create_task(
            self._ws.send(json.dumps({"type": msg_type, "data": data}))
        )

    def _emit(self, event: str, data: Any) -> None:
        for fn in self._listeners.get(event, []):
            try:
                fn(data)
            except Exception:
                pass
