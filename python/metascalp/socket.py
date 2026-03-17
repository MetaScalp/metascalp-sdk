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

    def subscribe(self, connection_id: int) -> None:
        """Subscribe to order, position, balance, and finres updates."""
        self._send("subscribe", {"connectionId": connection_id})

    def unsubscribe(self, connection_id: int) -> None:
        """Unsubscribe from connection-level updates."""
        self._send("unsubscribe", {"connectionId": connection_id})

    # ---- Market data subscriptions ----

    def subscribe_trades(self, connection_id: int, ticker: str) -> None:
        """Subscribe to real-time trade updates for a specific ticker."""
        self._send("trade_subscribe", {"connectionId": connection_id, "ticker": ticker})

    def unsubscribe_trades(self, connection_id: int, ticker: str) -> None:
        """Unsubscribe from trade updates."""
        self._send("trade_unsubscribe", {"connectionId": connection_id, "ticker": ticker})

    def subscribe_order_book(self, connection_id: int, ticker: str) -> None:
        """Subscribe to order book updates (snapshot + incremental)."""
        self._send("orderbook_subscribe", {"connectionId": connection_id, "ticker": ticker})

    def unsubscribe_order_book(self, connection_id: int, ticker: str) -> None:
        """Unsubscribe from order book updates."""
        self._send("orderbook_unsubscribe", {"connectionId": connection_id, "ticker": ticker})

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
