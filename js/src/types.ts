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

export interface MarkPriceUpdateData {
  connectionId: number;
  ticker: string;
  markPrice: number;
}

export interface FundingUpdateData {
  connectionId: number;
  ticker: string;
  fundingRate: number;
  fundingTime: string;
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

export interface GetOrderBookSnapshotOptions {
    /** Price aggregation factor. `0`/`1` = no aggregation; `> 1` = bucket and sum sizes. */
    zoomIndex?: number;
    /** Top-N price levels per side after zoom + percent. Must be >= 1 when specified. */
    depthLevels?: number;
    /** Per-side band as a percentage anchored on best ask / best bid. Must be > 0 when specified. */
    depthPercent?: number;
}

/** Response from `MetaScalpClient.getOrderBookSnapshot`. Same shape as the WS `orderbook_snapshot`
 * payload with an additional `updateId` from the exchange. */
export interface OrderBookSnapshotResponse {
    connectionId: number;
    ticker: string;
    updateId: number;
    asks: OrderBookOrder[];
    bids: OrderBookOrder[];
    bestAsk: OrderBookOrder | null;
    bestBid: OrderBookOrder | null;
}

// ============ MetaBroker Analytics Types ============
// Density map / large trades / liquidations — app-wide feeds relayed from the MetaBroker
// backend (the same data the terminal's analytics windows show). No connectionId required.
// One subscription per feed per socket: re-subscribing REPLACES the config.

export type AnalyticsExchangeName =
  | 'binance' | 'gate' | 'bybit' | 'kucoin' | 'bitget' | 'mexc' | 'okx' | 'bingx'
  | 'htx' | 'bitmart' | 'lbank' | 'hyperliquid' | 'upbit' | 'asterdex' | 'lighter'
  | 'xt' | 'edgex' | 'bitunix' | 'ourbit' | 'whitebit' | 'blofin' | 'weex' | 'polymarket';

export interface AnalyticsExchangeMarket {
  exchange: AnalyticsExchangeName;
  market: 'spot' | 'futures';
  /** Base density size mode: 'auto' derives it from the live order book. Default 'manual'. */
  bdsMode?: 'manual' | 'auto';
  /** Base density size in USD (used in 'manual' mode, and as the 'auto' fallback). Default 1 000 000. */
  bdsValue?: number;
}

export interface DensityMapSubscribeOptions {
  /** Required, non-empty — an empty list is rejected. */
  exchangeMarkets: AnalyticsExchangeMarket[];
  /** Size-class coefficients over the effective BDS. Defaults: 3 / 2 / 1. */
  largeCoefficient?: number;
  mediumCoefficient?: number;
  smallCoefficient?: number;
  /** Minimum wall age per size class, minutes. Defaults: 5 / 5 / 5. */
  largeLifetimeMinutes?: number;
  mediumLifetimeMinutes?: number;
  smallLifetimeMinutes?: number;
  /** 'USDT' | 'USDC' | 'OTHER'. Default: ['USDT']. */
  includedQuoteAssets?: string[];
}

export interface LargeTradesSubscribeOptions {
  /** Required, non-empty — an empty list is rejected. */
  exchangeMarkets: AnalyticsExchangeMarket[];
  /** Tape-merge window, 0–60000 ms; 0 = every raw print individually. Default 500. */
  aggregationMs?: number;
  /** Optional USD floor below which prints are not emitted. */
  minAmountUsd?: number;
  /** Size-class coefficients over the effective BDS. Defaults: 3 / 2 / 1. */
  largeCoefficient?: number;
  mediumCoefficient?: number;
  smallCoefficient?: number;
  /** 'USDT' | 'USDC' | 'OTHER'. Default: ['USDT', 'USDC', 'OTHER']. */
  includedQuoteAssets?: string[];
}

export type LiquidationExchangeName =
  | 'binance' | 'bybit' | 'okx' | 'bitget' | 'gate' | 'htx' | 'aster' | 'lighter';

export interface LiquidationsSubscribeOptions {
  /** Required, non-empty — an empty list is rejected. */
  exchanges: LiquidationExchangeName[];
  /** Minimum USD size of a single liquidation. Default 0. */
  minNotionalUsd?: number;
  /** Minimum notional / 24h turnover × 10000; rows with unknown turnover are dropped when set. Default off. */
  minImpactBps?: number;
  /** Default 'all'. TradFi = stocks, ETFs, indices, metals, commodities. */
  assetClass?: 'all' | 'crypto' | 'tradfi';
  /** Side of the LIQUIDATED position. Default 'all'. */
  side?: 'all' | 'long' | 'short';
  /** Case-insensitive prefix on the resolved coin ('BTC', not 'BTCUSDT'). Default ''. */
  coin?: string;
  /** Aggregation window for totals / topTokens only (never the rows). Default 'h1'. */
  window?: 'm5' | 'm15' | 'h1' | 'h4' | 'h24';
  /** Snapshot row count, 0–500. Default 200. */
  backfill?: number;
}

/** One density wall notification — each wall is reported exactly once, on first sight of its id. */
export interface DensityMapNotification {
  id: string;
  exchange: string;
  exchangeLogo: string;
  market: 'spot' | 'futures';
  ticker: string;
  side: 'ask' | 'bid';
  price: number;
  /** Distance from the current price, ± percent, capped ±10. */
  distancePercent: number;
  sizeUsd: number;
  time: string;
}

export interface DensityMapSnapshotData {
  notifications: DensityMapNotification[];
}

export interface DensityMapUpdateData {
  notifications: DensityMapNotification[];
}

/** One aggregated trade print — final and append-only, never updated or removed. */
export interface LargeTrade {
  id: string;
  exchange: string;
  exchangeLogo: string;
  market: 'spot' | 'futures';
  ticker: string;
  side: 'buy' | 'sell';
  minPrice: number;
  maxPrice: number;
  sizeUsd: number;
  tradeCount: number;
  category: 'small' | 'medium' | 'large';
  time: string;
}

export interface LargeTradesUpdateData {
  trades: LargeTrade[];
}

export interface LiquidationRow {
  time: string;
  /** Lowercase venue name; null for a venue the terminal does not know. */
  exchange: string | null;
  /** Exchange-native instrument (BTCUSDT, BTC-USDT-SWAP, ...). */
  symbol: string;
  /** Resolved base token (1000BONKUSDT → BONK). */
  coin: string;
  /** Side of the LIQUIDATED position: 'long' = longs got liquidated (price fell). */
  side: 'long' | 'short';
  assetClass: 'crypto' | 'tradfi';
  price: number;
  size: number;
  notionalUsd: number;
  /** notionalUsd / 24h turnover × 10000; null when turnover is unknown. */
  impactBps: number | null;
}

export interface LiquidationsTotals {
  longUsd: number;
  shortUsd: number;
  longCount: number;
  shortCount: number;
}

export interface TopToken {
  token: string;
  longUsd: number;
  shortUsd: number;
  longCount: number;
  shortCount: number;
}

export interface LiquidationsSnapshotData {
  liquidations: LiquidationRow[];
  totals: LiquidationsTotals | null;
  topTokens: TopToken[];
}

export interface LiquidationsUpdateData {
  liquidations: LiquidationRow[];
}

export interface LiquidationsMetadataData {
  totals: LiquidationsTotals | null;
  topTokens: TopToken[];
}

export interface SocketEventMap {
  order_update: OrderUpdateData;
  position_update: PositionUpdateData;
  balance_update: BalanceUpdateData;
  finres_update: FinresUpdateData;
  trade_update: TradeUpdateData;
  orderbook_snapshot: OrderBookSnapshotData;
  orderbook_update: OrderBookUpdateData;
  mark_price_update: MarkPriceUpdateData;
  funding_update: FundingUpdateData;
  subscribed: { connectionId: number };
  unsubscribed: { connectionId: number };
  trade_subscribed: { connectionId: number; ticker: string };
  trade_unsubscribed: { connectionId: number; ticker: string };
  orderbook_subscribed: { connectionId: number; ticker: string };
  orderbook_unsubscribed: { connectionId: number; ticker: string };
  mark_price_subscribed: { connectionId: number; ticker: string };
  mark_price_unsubscribed: { connectionId: number; ticker: string };
  funding_subscribed: { connectionId: number; ticker: string };
  funding_unsubscribed: { connectionId: number; ticker: string };
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
  density_map_subscribed: Record<string, never>;
  density_map_unsubscribed: Record<string, never>;
  density_map_snapshot: DensityMapSnapshotData;
  density_map_update: DensityMapUpdateData;
  large_trades_subscribed: Record<string, never>;
  large_trades_unsubscribed: Record<string, never>;
  large_trades_update: LargeTradesUpdateData;
  liquidations_subscribed: Record<string, never>;
  liquidations_unsubscribed: Record<string, never>;
  liquidations_snapshot: LiquidationsSnapshotData;
  liquidations_update: LiquidationsUpdateData;
  liquidations_metadata: LiquidationsMetadataData;
  error: { error: string };
  connected: void;
  disconnected: void;
}
