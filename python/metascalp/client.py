"""MetaScalp REST API client."""

from __future__ import annotations

import aiohttp
from typing import Any, Optional

HTTP_PORT_START = 17845
HTTP_PORT_END = 17855


class MetaScalpApiError(Exception):
    """Raised when the MetaScalp API returns an error response."""

    def __init__(self, status: int, error: str, path: str):
        self.status = status
        self.error = error
        self.path = path
        super().__init__(f"MetaScalp API error {status} on {path}: {error}")


class MetaScalpClient:
    """HTTP REST client for MetaScalp API.

    Usage:
        client = await MetaScalpClient.discover()
        connections = await client.get_connections()
    """

    def __init__(self, port: int):
        self.port = port
        self.base_url = f"http://127.0.0.1:{port}"
        self._session: Optional[aiohttp.ClientSession] = None

    @classmethod
    async def discover(cls, timeout: float = 1.0) -> MetaScalpClient:
        """Scan ports 17845-17855 to find a running MetaScalp instance."""
        timeout_obj = aiohttp.ClientTimeout(total=timeout)
        async with aiohttp.ClientSession(timeout=timeout_obj) as session:
            for port in range(HTTP_PORT_START, HTTP_PORT_END + 1):
                try:
                    async with session.get(f"http://127.0.0.1:{port}/ping") as resp:
                        if resp.status == 200:
                            data = await resp.json()
                            if data.get("app") == "MetaScalp":
                                return cls(port)
                except (aiohttp.ClientError, OSError):
                    continue
        raise ConnectionError(
            f"MetaScalp not found on ports {HTTP_PORT_START}-{HTTP_PORT_END}"
        )

    async def _ensure_session(self) -> aiohttp.ClientSession:
        if self._session is None or self._session.closed:
            self._session = aiohttp.ClientSession()
        return self._session

    async def close(self) -> None:
        """Close the underlying HTTP session."""
        if self._session and not self._session.closed:
            await self._session.close()

    async def _get(self, path: str) -> Any:
        session = await self._ensure_session()
        async with session.get(f"{self.base_url}{path}") as resp:
            data = await resp.json()
            if resp.status >= 400:
                raise MetaScalpApiError(resp.status, data.get("error", ""), path)
            return data

    async def _post(self, path: str, body: Any) -> Any:
        session = await self._ensure_session()
        async with session.post(
            f"{self.base_url}{path}", json=body
        ) as resp:
            data = await resp.json()
            if resp.status >= 400:
                raise MetaScalpApiError(resp.status, data.get("error", ""), path)
            return data

    async def _delete(self, path: str) -> Any:
        session = await self._ensure_session()
        async with session.delete(f"{self.base_url}{path}") as resp:
            data = await resp.json()
            if resp.status >= 400:
                raise MetaScalpApiError(resp.status, data.get("error", ""), path)
            return data

    # ---- Discovery ----

    async def ping(self) -> dict:
        """Check MetaScalp instance and get version."""
        return await self._get("/ping")

    # ---- Connections ----

    async def get_connections(self) -> dict:
        """List all active exchange connections."""
        return await self._get("/api/connections")

    async def get_connection(self, connection_id: int) -> dict:
        """Get a single connection by ID."""
        return await self._get(f"/api/connections/{connection_id}")

    # ---- Market Data Queries ----

    async def get_tickers(self, connection_id: int, refresh: bool = False) -> dict:
        """List available tickers on a connection. Set refresh=True to fetch fresh data from exchange."""
        qs = "?Refresh=true" if refresh else ""
        return await self._get(f"/api/connections/{connection_id}/tickers{qs}")

    # ---- Trading Data ----

    async def get_orders(self, connection_id: int, ticker: str) -> dict:
        """Get open orders for a ticker."""
        return await self._get(
            f"/api/connections/{connection_id}/orders?Ticker={ticker}"
        )

    async def get_positions(self, connection_id: int) -> dict:
        """Get open positions on a connection."""
        return await self._get(f"/api/connections/{connection_id}/positions")

    async def get_balance(self, connection_id: int) -> dict:
        """Get account balances on a connection."""
        return await self._get(f"/api/connections/{connection_id}/balance")

    # ---- Order Execution ----

    async def place_order(
        self,
        connection_id: int,
        *,
        ticker: str,
        side: int,
        price: float,
        size: float,
        type: int = 0,
        reduce_only: bool = False,
    ) -> dict:
        """Place an order on the exchange."""
        return await self._post(
            f"/api/connections/{connection_id}/orders",
            {
                "ticker": ticker,
                "side": side,
                "price": price,
                "size": size,
                "type": type,
                "reduceOnly": reduce_only,
            },
        )

    async def cancel_order(
        self,
        connection_id: int,
        *,
        ticker: str,
        order_id: int,
        type: int = 0,
    ) -> dict:
        """Cancel an existing order."""
        return await self._post(
            f"/api/connections/{connection_id}/orders/cancel",
            {"ticker": ticker, "orderId": order_id, "type": type},
        )

    # ---- UI Control ----

    async def change_ticker(
        self,
        *,
        ticker_pattern: Optional[str] = None,
        exchange: Optional[int] = None,
        market: Optional[int] = None,
        ticker: Optional[str] = None,
        binding: Optional[str] = None,
    ) -> dict:
        """Switch the active ticker in MetaScalp UI."""
        body: dict[str, Any] = {}
        if ticker_pattern is not None:
            body["tickerPattern"] = ticker_pattern
        if exchange is not None:
            body["exchange"] = exchange
        if market is not None:
            body["market"] = market
        if ticker is not None:
            body["ticker"] = ticker
        if binding is not None:
            body["binding"] = binding
        return await self._post("/api/change-ticker", body)

    async def open_combo(self, ticker: str) -> dict:
        """Open a combo layout for a ticker."""
        return await self._post("/api/combo", {"ticker": ticker})

    # ---- Signal Levels ----

    async def get_signal_levels(self, connection_id: int, ticker: str) -> dict:
        """Get signal levels for a ticker on a connection."""
        return await self._get(
            f"/api/connections/{connection_id}/signal-levels?Ticker={ticker}"
        )

    async def place_signal_level(self, connection_id: int, *, ticker: str, price: float) -> dict:
        """Place a signal level on a connection."""
        return await self._post(
            f"/api/connections/{connection_id}/signal-levels",
            {"Ticker": ticker, "Price": price},
        )

    async def remove_signal_level(self, connection_id: int, signal_level_id: int) -> dict:
        """Remove a signal level by ID."""
        return await self._delete(
            f"/api/connections/{connection_id}/signal-levels/{signal_level_id}"
        )

    async def remove_all_signal_levels(self, connection_id: int, ticker: str) -> dict:
        """Remove all signal levels for a ticker on a connection."""
        return await self._delete(
            f"/api/connections/{connection_id}/signal-levels?Ticker={ticker}"
        )

    async def remove_triggered_signal_levels(self) -> dict:
        """Remove all triggered signal levels."""
        return await self._delete("/api/signal-levels/triggered")
