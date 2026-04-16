import type {
  PingResponse,
  ConnectionsResponse,
  Connection,
  TickersResponse,
  OrdersResponse,
  PositionsResponse,
  BalanceResponse,
  PlaceOrderRequest,
  PlaceOrderResponse,
  CancelOrderRequest,
  ChangeTickerRequest,
  ComboRequest,
  SignalLevelsResponse,
  PlaceSignalLevelRequest,
  OrderBookSettingsResponse,
  OrderBookSettings,
} from './types';

const HTTP_PORT_START = 17845;
const HTTP_PORT_END = 17855;

export class MetaScalpClient {
  private readonly baseUrl: string;

  constructor(port: number) {
    this.baseUrl = `http://127.0.0.1:${port}`;
  }

  /**
   * Scans ports 17845–17855 to find a running MetaScalp instance.
   * Returns a connected client or throws if not found.
   */
  static async discover(timeoutMs = 1000): Promise<MetaScalpClient> {
    for (let port = HTTP_PORT_START; port <= HTTP_PORT_END; port++) {
      try {
        const controller = new AbortController();
        const timer = setTimeout(() => controller.abort(), timeoutMs);
        const res = await fetch(`http://127.0.0.1:${port}/ping`, { signal: controller.signal });
        clearTimeout(timer);
        if (res.ok) {
          const data = await res.json() as PingResponse;
          if (data.app === 'MetaScalp') {
            return new MetaScalpClient(port);
          }
        }
      } catch {
        // try next port
      }
    }
    throw new Error(`MetaScalp not found on ports ${HTTP_PORT_START}-${HTTP_PORT_END}`);
  }

  get port(): number {
    return parseInt(this.baseUrl.split(':')[2]);
  }

  // ---- Discovery ----

  async ping(): Promise<PingResponse> {
    return this.get('/ping');
  }

  // ---- Connections ----

  async getConnections(): Promise<ConnectionsResponse> {
    return this.get('/api/connections');
  }

  async getConnection(connectionId: number): Promise<Connection> {
    return this.get(`/api/connections/${connectionId}`);
  }

  // ---- Market Data Queries ----

  async getTickers(connectionId: number, refresh = false): Promise<TickersResponse> {
    return this.get(`/api/connections/${connectionId}/tickers${refresh ? '?Refresh=true' : ''}`);
  }

  // ---- Trading Data ----

  async getOrders(connectionId: number, ticker: string): Promise<OrdersResponse> {
    return this.get(`/api/connections/${connectionId}/orders?Ticker=${encodeURIComponent(ticker)}`);
  }

  async getPositions(connectionId: number): Promise<PositionsResponse> {
    return this.get(`/api/connections/${connectionId}/positions`);
  }

  async getBalance(connectionId: number): Promise<BalanceResponse> {
    return this.get(`/api/connections/${connectionId}/balance`);
  }

  // ---- Order Execution ----

  async placeOrder(connectionId: number, order: PlaceOrderRequest): Promise<PlaceOrderResponse> {
    return this.post(`/api/connections/${connectionId}/orders`, order);
  }

  async cancelOrder(connectionId: number, request: CancelOrderRequest): Promise<{ status: string }> {
    return this.post(`/api/connections/${connectionId}/orders/cancel`, request);
  }

  // ---- UI Control ----

  async changeTicker(request: ChangeTickerRequest): Promise<{ status: string }> {
    return this.post('/api/change-ticker', request);
  }

  async openCombo(request: ComboRequest): Promise<{ status: string }> {
    return this.post('/api/combo', request);
  }

  // ---- Order Book Settings ----

  async getOrderBookSettings(connectionId: number, ticker: string): Promise<OrderBookSettingsResponse> {
    return this.get(`/api/connections/${connectionId}/orderbook-settings?Ticker=${encodeURIComponent(ticker)}`);
  }

  async updateOrderBookSettings(connectionId: number, ticker: string, settings: Partial<OrderBookSettings>): Promise<OrderBookSettingsResponse> {
    return this.put(`/api/connections/${connectionId}/orderbook-settings?Ticker=${encodeURIComponent(ticker)}`, settings);
  }

  // ---- Signal Levels ----

  async getSignalLevels(connectionId: number, ticker: string): Promise<SignalLevelsResponse> {
    return this.get(`/api/connections/${connectionId}/signal-levels?Ticker=${encodeURIComponent(ticker)}`);
  }

  async placeSignalLevel(connectionId: number, request: PlaceSignalLevelRequest): Promise<{ status: string }> {
    return this.post(`/api/connections/${connectionId}/signal-levels`, request);
  }

  async removeSignalLevel(connectionId: number, signalLevelId: number): Promise<{ status: string }> {
    return this.delete(`/api/connections/${connectionId}/signal-levels/${signalLevelId}`);
  }

  async removeAllSignalLevels(connectionId: number, ticker: string): Promise<{ status: string }> {
    return this.delete(`/api/connections/${connectionId}/signal-levels?Ticker=${encodeURIComponent(ticker)}`);
  }

  async removeTriggeredSignalLevels(): Promise<{ status: string }> {
    return this.delete('/api/signal-levels/triggered');
  }

  // ---- HTTP helpers ----

  private async get<T>(path: string): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`);
    if (!res.ok) {
      const body = await res.json().catch(() => ({}));
      throw new MetaScalpApiError(res.status, (body as any)?.error || res.statusText, path);
    }
    return res.json() as Promise<T>;
  }

  private async delete<T>(path: string): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`, { method: 'DELETE' });
    if (!res.ok) {
      const body = await res.json().catch(() => ({}));
      throw new MetaScalpApiError(res.status, (body as any)?.error || res.statusText, path);
    }
    return res.json() as Promise<T>;
  }

  private async post<T>(path: string, body: unknown): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      throw new MetaScalpApiError(res.status, (data as any)?.error || res.statusText, path);
    }
    return res.json() as Promise<T>;
  }

  private async put<T>(path: string, body: unknown): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      throw new MetaScalpApiError(res.status, (data as any)?.error || res.statusText, path);
    }
    return res.json() as Promise<T>;
  }
}

export class MetaScalpApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly error: string,
    public readonly path: string,
  ) {
    super(`MetaScalp API error ${status} on ${path}: ${error}`);
    this.name = 'MetaScalpApiError';
  }
}
