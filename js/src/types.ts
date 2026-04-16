// ============ REST Types ============

export interface PingResponse {
  app: string;
  version: string;
}

export interface Connection {
  id: number;
  name: string;
  exchange: string;
  exchangeId: number;
  market: string;
  marketType: number;
  state: number;
  viewMode: boolean;
  demoMode: boolean;
}

export interface ConnectionsResponse {
  connections: Connection[];
}

export interface Ticker {
  name: string;
  baseAsset: string;
  quoteAsset: string;
  isTradingAllowed: boolean;
  priceIncrement: number;
  sizeIncrement: number;
  minSize: number;
  maxSize: number | null;
}

export interface TickersResponse {
  connectionId: number;
  count: number;
  tickers: Ticker[];
}

export interface Order {
  id: number;
  ticker: string;
  clientId: string | null;
  side: number;
  price: number;
  size: number;
  filledSize: number;
  filledPrice: number;
  remainingSize: number;
  status: number;
  type: number;
  triggerPrice: number | null;
  createDate: string;
}

export interface OrdersResponse {
  connectionId: number;
  ticker: string;
  count: number;
  orders: Order[];
}

export interface Position {
  id: number;
  ticker: string;
  side: number;
  size: number;
  avgPrice: number;
  marginMode: number;
}

export interface PositionsResponse {
  connectionId: number;
  count: number;
  positions: Position[];
}

export interface Balance {
  coin: string;
  total: number;
  free: number;
  locked: number;
}

export interface BalanceResponse {
  connectionId: number;
  count: number;
  balances: Balance[];
}

export interface PlaceOrderRequest {
  ticker: string;
  side: number;
  price: number;
  size: number;
  type?: number;
  reduceOnly?: boolean;
}

export interface PlaceOrderResponse {
  status: string;
  clientId: string;
  executionTimeMs: number;
}

export interface CancelOrderRequest {
  ticker: string;
  orderId: number;
  type?: number;
}

export interface ChangeTickerRequest {
  tickerPattern?: string;
  exchange?: number;
  market?: number;
  ticker?: string;
  binding?: string;
}

export interface ComboRequest {
  ticker: string;
}

// ============ Socket Types ============

export interface OrderUpdateData {
  connectionId: number;
  orderId: number;
  ticker: string;
  side: string;
  type: string;
  price: number;
  filledPrice: number;
  size: number;
  filledSize: number;
  fee: number;
  feeCurrency: string;
  status: string;
  time: string;
}

export interface PositionUpdateData {
  connectionId: number;
  positionId: number;
  ticker: string;
  side: string;
  size: number;
  avgPrice: number;
  avgPriceFix: number;
  avgPriceDyn: number;
  status: string;
}

export interface BalanceUpdateData {
  connectionId: number;
  balances: Balance[];
}

export interface Finres {
  currency: string;
  result: number;
  fee: number;
  funds: number;
  available: number;
  blocked: number;
}

export interface FinresUpdateData {
  connectionId: number;
  finreses: Finres[];
}

export interface Trade {
  price: number;
  size: number;
  side: string;
  time: string;
}

export interface TradeUpdateData {
  connectionId: number;
  ticker: string;
  trades: Trade[];
}

export interface OrderBookOrder {
  price: number;
  size: number;
  type: string;
}

export interface OrderBookSnapshotData {
  connectionId: number;
  ticker: string;
  asks: OrderBookOrder[];
  bids: OrderBookOrder[];
  bestAsk: OrderBookOrder | null;
  bestBid: OrderBookOrder | null;
}

export interface OrderBookUpdateData {
  connectionId: number;
  ticker: string;
  updates: OrderBookOrder[];
}

export interface SignalLevel {
  id: number;
  connectionId: number;
  ticker: string;
  price: number;
  isTriggered: boolean;
  triggerTime: string | null;
  triggerRule: string;
}

export interface SignalLevelsResponse {
  connectionId: number;
  ticker: string;
  count: number;
  signalLevels: SignalLevel[];
}

export interface PlaceSignalLevelRequest {
  ticker: string;
  price: number;
}

export interface SignalLevelsSnapshotData {
  signalLevels: SignalLevel[];
}

export interface SignalLevelPlacedData {
  id: number;
  connectionId: number;
  ticker: string;
  price: number;
  isTriggered: boolean;
  triggerTime: string | null;
  triggerRule: string;
}

export interface SignalLevelTriggeredData {
  id: number;
  triggerTime: string;
}

export interface SignalLevelRemovedData {
  id: number;
}

export interface Notification {
  type: string;
  exchange: string;
  exchangeId: number;
  exchangeLogo: string;
  market: string;
  marketType: string;
  ticker: string;
  price: number;
  size: number;
  tabName: string;
  color: string;
  date: string;
}

export interface NotificationSnapshotData {
  notifications: Notification[];
}

export interface NotificationUpdateData {
  notifications: Notification[];
}

export interface OrderBookSettings {
    // Trading
    notificationTradeHasBeenMade?: boolean;
    orderTypeDefault?: number;
    defaultOrderCoin?: number;
    defaultOrderUsd?: number;
    orderSlippageCoin?: number;
    orderSlippageUsd?: number;
    closeByMarket?: boolean;

    // OrderBook
    amountBarFilledAt?: number;
    largeAmount?: number;
    largeAmount2?: number;
    amountBarFilter?: number;
    amountBarFilledAtUsd?: number;
    largeAmountUsd?: number;
    largeAmountUsd2?: number;
    amountBarFilterUsd?: number;
    notificationLargeAmountDetected?: boolean;
    notificationLargeAmount2Detected?: boolean;
    useLargeAmountDetectionArea?: boolean;
    largeAmountDetectionMinValue?: number;
    largeAmountDetectionMaxValue?: number;
    showRuler?: string;
    zoomType?: string;
    autoZoom?: boolean;
    zoomPercent?: number;
    rowHeight?: number;
    slimLevelsFactor?: number;
    basicLevelsFactor?: number;
    notificationSignalLevelTriggered?: boolean;
    autoscroll?: boolean;
    fullDepth?: boolean;

    // Ticks
    ticksLargeAmount?: number;
    ticksLargeAmountUsd?: number;
    sizeType?: string;
    notificationTradeHasBeenMadeTicks?: boolean;

    // Clusters
    showClusters?: boolean;
    clusterTimeFrame?: string;

    // General
    soundNotification?: boolean;
}

export interface OrderBookSettingsResponse {
    connectionId: number;
    ticker: string;
    settings: OrderBookSettings;
}

export interface SocketEventMap {
  order_update: OrderUpdateData;
  position_update: PositionUpdateData;
  balance_update: BalanceUpdateData;
  finres_update: FinresUpdateData;
  trade_update: TradeUpdateData;
  orderbook_snapshot: OrderBookSnapshotData;
  orderbook_update: OrderBookUpdateData;
  subscribed: { connectionId: number };
  unsubscribed: { connectionId: number };
  trade_subscribed: { connectionId: number; ticker: string };
  trade_unsubscribed: { connectionId: number; ticker: string };
  orderbook_subscribed: { connectionId: number; ticker: string };
  orderbook_unsubscribed: { connectionId: number; ticker: string };
  notification_subscribed: Record<string, never>;
  notification_unsubscribed: Record<string, never>;
  notification_snapshot: NotificationSnapshotData;
  notification_update: NotificationUpdateData;
  signal_level_subscribed: Record<string, never>;
  signal_level_unsubscribed: Record<string, never>;
  signal_levels_snapshot: SignalLevelsSnapshotData;
  signal_level_placed: SignalLevelPlacedData;
  signal_level_triggered: SignalLevelTriggeredData;
  signal_level_removed: SignalLevelRemovedData;
  signal_levels_removed_all: Record<string, never>;
  signal_levels_removed_triggered: Record<string, never>;
  error: { error: string };
  connected: void;
  disconnected: void;
}
