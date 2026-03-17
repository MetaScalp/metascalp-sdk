import type { SocketEventMap } from './types';

const WS_PORT_START = 17845;
const WS_PORT_END = 17855;

type EventName = keyof SocketEventMap;
type Listener<K extends EventName> = (data: SocketEventMap[K]) => void;

/**
 * WebSocket client for MetaScalp real-time updates.
 * Supports connection-level subscriptions (orders, positions, balances)
 * and market data subscriptions (trades, order book) scoped by connectionId + ticker.
 */
export class MetaScalpSocket {
  private ws: WebSocket | null = null;
  private listeners = new Map<string, Set<Function>>();
  private _port: number;
  private _connected = false;

  constructor(port: number) {
    this._port = port;
  }

  /**
   * Scans ports 17845–17855 to find the MetaScalp WebSocket server.
   * Returns a connected socket or throws if not found.
   */
  static async discover(timeoutMs = 1000): Promise<MetaScalpSocket> {
    for (let port = WS_PORT_START; port <= WS_PORT_END; port++) {
      try {
        const socket = new MetaScalpSocket(port);
        await socket.connect(timeoutMs);
        return socket;
      } catch {
        // try next port
      }
    }
    throw new Error(`MetaScalp WebSocket not found on ports ${WS_PORT_START}-${WS_PORT_END}`);
  }

  get port(): number {
    return this._port;
  }

  get connected(): boolean {
    return this._connected;
  }

  /**
   * Connect to the WebSocket server.
   */
  connect(timeoutMs = 5000): Promise<void> {
    return new Promise((resolve, reject) => {
      const url = `ws://127.0.0.1:${this._port}/`;

      // Support both browser WebSocket and Node.js ws
      const WS = typeof WebSocket !== 'undefined' ? WebSocket : require('ws');
      this.ws = new WS(url) as WebSocket;

      const timer = setTimeout(() => {
        this.ws?.close();
        reject(new Error(`Connection timeout after ${timeoutMs}ms`));
      }, timeoutMs);

      this.ws.onopen = () => {
        clearTimeout(timer);
        this._connected = true;
        this.emit('connected', undefined as any);
        resolve();
      };

      this.ws.onerror = () => {
        clearTimeout(timer);
        reject(new Error(`Failed to connect to ws://127.0.0.1:${this._port}/`));
      };

      this.ws.onmessage = (event: MessageEvent) => {
        try {
          const raw = JSON.parse(typeof event.data === 'string' ? event.data : event.data.toString());
          const msg = normalize(raw);
          if (msg.type && msg.data !== undefined) {
            this.emit(msg.type as EventName, msg.data);
          }
        } catch {
          // ignore malformed messages
        }
      };

      this.ws.onclose = () => {
        this._connected = false;
        this.emit('disconnected', undefined as any);
      };
    });
  }

  /**
   * Disconnect from the WebSocket server.
   */
  disconnect(): void {
    this.ws?.close();
    this.ws = null;
    this._connected = false;
  }

  // ---- Connection-level subscriptions ----
  // Use these to receive order, position, balance, and finres updates
  // for ALL tickers on a connection.
  // Events: 'order_update', 'position_update', 'balance_update', 'finres_update'

  /**
   * Subscribe to order, position, balance, and finres updates for a connection.
   * This covers ALL tickers on the connection.
   *
   * Events you'll receive:
   * - `order_update` — order created/modified/filled/cancelled
   * - `position_update` — position opened/changed/closed
   * - `balance_update` — account balances changed
   * - `finres_update` — financial results recalculated
   */
  subscribe(connectionId: number): void {
    this.send('subscribe', { connectionId });
  }

  /**
   * Unsubscribe from connection-level updates.
   */
  unsubscribe(connectionId: number): void {
    this.send('unsubscribe', { connectionId });
  }

  // ---- Market data subscriptions ----
  // Use these to receive real-time market data for a SPECIFIC ticker on a connection.
  // These are independent from subscribe() — you can use one without the other.
  // Events: 'trade_update', 'orderbook_snapshot', 'orderbook_update'

  /**
   * Subscribe to real-time trade updates for a specific ticker.
   * Independent from subscribe() — only sends trade data for this exact ticker.
   *
   * Event: `trade_update`
   */
  subscribeTrades(connectionId: number, ticker: string): void {
    this.send('trade_subscribe', { connectionId, ticker });
  }

  /**
   * Unsubscribe from trade updates for a specific ticker.
   */
  unsubscribeTrades(connectionId: number, ticker: string): void {
    this.send('trade_unsubscribe', { connectionId, ticker });
  }

  /**
   * Subscribe to order book updates for a specific ticker.
   * You will receive one `orderbook_snapshot` followed by `orderbook_update` events.
   * Independent from subscribe() — only sends order book data for this exact ticker.
   *
   * Events: `orderbook_snapshot` (once), then `orderbook_update` (continuous)
   */
  subscribeOrderBook(connectionId: number, ticker: string): void {
    this.send('orderbook_subscribe', { connectionId, ticker });
  }

  /**
   * Unsubscribe from order book updates for a specific ticker.
   */
  unsubscribeOrderBook(connectionId: number, ticker: string): void {
    this.send('orderbook_unsubscribe', { connectionId, ticker });
  }

  // ---- Event handling ----

  /**
   * Register an event listener.
   */
  on<K extends EventName>(event: K, listener: Listener<K>): this {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    this.listeners.get(event)!.add(listener);
    return this;
  }

  /**
   * Remove an event listener.
   */
  off<K extends EventName>(event: K, listener: Listener<K>): this {
    this.listeners.get(event)?.delete(listener);
    return this;
  }

  /**
   * Register a one-time event listener.
   */
  once<K extends EventName>(event: K, listener: Listener<K>): this {
    const wrapper = ((data: SocketEventMap[K]) => {
      this.off(event, wrapper as Listener<K>);
      listener(data);
    }) as Listener<K>;
    return this.on(event, wrapper);
  }

  /**
   * Wait for a specific event. Useful for awaiting acknowledgements.
   */
  waitFor<K extends EventName>(event: K, timeoutMs = 5000): Promise<SocketEventMap[K]> {
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        this.off(event, handler);
        reject(new Error(`Timeout waiting for '${event}'`));
      }, timeoutMs);
      const handler = ((data: SocketEventMap[K]) => {
        clearTimeout(timer);
        resolve(data);
      }) as Listener<K>;
      this.once(event, handler);
    });
  }

  // ---- Internals ----

  private send(type: string, data: Record<string, unknown>): void {
    if (!this.ws || !this._connected) {
      throw new Error('Not connected');
    }
    this.ws.send(JSON.stringify({ type, data }));
  }

  private emit(event: string, data: unknown): void {
    const set = this.listeners.get(event);
    if (set) {
      for (const fn of set) {
        try { fn(data); } catch { /* listener error */ }
      }
    }
  }
}

/**
 * Normalize PascalCase keys from server to camelCase.
 */
function normalize(obj: unknown): any {
  if (obj === null || obj === undefined) return obj;
  if (Array.isArray(obj)) return obj.map(normalize);
  if (typeof obj !== 'object') return obj;
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(obj as Record<string, unknown>)) {
    const camel = k.charAt(0).toLowerCase() + k.slice(1);
    out[camel] = normalize(v);
  }
  return out;
}
