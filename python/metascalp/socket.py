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
    ) -> None:
        """Subscribe to order book updates for a specific ticker.

        When zoom_index is 0 (default), receives full order book + incremental updates.
        When zoom_index > 1, price levels are aggregated into zoomed buckets.

        Optional depth filters (must be > 0):
        - depth_levels: trims the snapshot to the top N price levels per side (asks ascending,
          bids descending), applied AFTER zoom and depth_percent. Filters the snapshot ONLY —
          incremental updates are unaffected.
        - depth_percent: per-side band as a percentage, anchored on best ask / best bid (NOT mid).
          Asks kept where price <= bestAsk * (1 + depth_percent / 100); bids where
          price >= bestBid * (1 - depth_percent / 100). Applies to both the snapshot and
          subsequent updates; the band refreshes from the latest known best ask / best bid on
          each event. If a side's anchor is unknown, that side is not filtered (degrades open).
        bestAsk / bestBid payload fields are never filtered.

        Events: 'orderbook_snapshot' (once), then 'orderbook_update' (continuous)
        """
        data: dict = {"connectionId": connection_id, "ticker": ticker, "zoomIndex": zoom_index}
        if depth_levels is not None:
            data["depthLevels"] = depth_levels
        if depth_percent is not None:
            data["depthPercent"] = depth_percent
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
