namespace MetaScalp.Sdk;

// ============ REST Models ============

public class PingResponse
{
    public string App { get; set; } = "";
    public string Version { get; set; } = "";
}

public class ConnectionDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Exchange { get; set; } = "";
    public int ExchangeId { get; set; }
    public string Market { get; set; } = "";
    public int MarketType { get; set; }
    public int State { get; set; }
    public bool ViewMode { get; set; }
    public bool DemoMode { get; set; }
}

public class ConnectionsResponse
{
    public List<ConnectionDto> Connections { get; set; } = new();
}

public class TickerDto
{
    public string Name { get; set; } = "";
    public string BaseAsset { get; set; } = "";
    public string QuoteAsset { get; set; } = "";
    public bool IsTradingAllowed { get; set; }
    public decimal PriceIncrement { get; set; }
    public decimal SizeIncrement { get; set; }
    public decimal MinSize { get; set; }
    public decimal? MaxSize { get; set; }
}

public class TickersResponse
{
    public long ConnectionId { get; set; }
    public int Count { get; set; }
    public List<TickerDto> Tickers { get; set; } = new();
}

public class OrderDto
{
    public long Id { get; set; }
    public string Ticker { get; set; } = "";
    public string? ClientId { get; set; }
    public int Side { get; set; }
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public decimal FilledSize { get; set; }
    public decimal FilledPrice { get; set; }
    public decimal RemainingSize { get; set; }
    public int Status { get; set; }
    public int Type { get; set; }
    public decimal? TriggerPrice { get; set; }
    public string CreateDate { get; set; } = "";
}

public class OrdersResponse
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public int Count { get; set; }
    public List<OrderDto> Orders { get; set; } = new();
}

public class PositionDto
{
    public long Id { get; set; }
    public string Ticker { get; set; } = "";
    public int Side { get; set; }
    public decimal Size { get; set; }
    public decimal AvgPrice { get; set; }
    public int MarginMode { get; set; }
}

public class PositionsResponse
{
    public long ConnectionId { get; set; }
    public int Count { get; set; }
    public List<PositionDto> Positions { get; set; } = new();
}

public class BalanceDto
{
    public string Coin { get; set; } = "";
    public decimal Total { get; set; }
    public decimal Free { get; set; }
    public decimal Locked { get; set; }
}

public class BalanceResponse
{
    public long ConnectionId { get; set; }
    public int Count { get; set; }
    public List<BalanceDto> Balances { get; set; } = new();
}

public class PlaceOrderRequest
{
    public string Ticker { get; set; } = "";
    public int Side { get; set; }
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public int Type { get; set; }
    public bool ReduceOnly { get; set; }
}

public class PlaceOrderResponse
{
    public string Status { get; set; } = "";
    public string ClientId { get; set; } = "";
    public double ExecutionTimeMs { get; set; }
}

public class CancelOrderRequest
{
    public string Ticker { get; set; } = "";
    public long OrderId { get; set; }
    public int Type { get; set; }
}

// ============ Socket Models ============

public class OrderUpdateData
{
    public long ConnectionId { get; set; }
    public long OrderId { get; set; }
    public string Ticker { get; set; } = "";
    public string Side { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Price { get; set; }
    public decimal FilledPrice { get; set; }
    public decimal Size { get; set; }
    public decimal FilledSize { get; set; }
    public decimal Fee { get; set; }
    public string? FeeCurrency { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset Time { get; set; }
}

public class PositionUpdateData
{
    public long ConnectionId { get; set; }
    public long PositionId { get; set; }
    public string Ticker { get; set; } = "";
    public string Side { get; set; } = "";
    public decimal Size { get; set; }
    public decimal AvgPrice { get; set; }
    public decimal AvgPriceFix { get; set; }
    public decimal AvgPriceDyn { get; set; }
    public string Status { get; set; } = "";
}

public class BalanceUpdateData
{
    public long ConnectionId { get; set; }
    public List<BalanceDto> Balances { get; set; } = new();
}

public class FinresDto
{
    public string Currency { get; set; } = "";
    public decimal Result { get; set; }
    public decimal Fee { get; set; }
    public decimal Funds { get; set; }
    public decimal Available { get; set; }
    public decimal Blocked { get; set; }
}

public class FinresUpdateData
{
    public long ConnectionId { get; set; }
    public List<FinresDto> Finreses { get; set; } = new();
}

public class TradeDto
{
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public string Side { get; set; } = "";
    public DateTimeOffset Time { get; set; }
}

public class TradeUpdateData
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public List<TradeDto> Trades { get; set; } = new();
}

public class OrderBookOrderDto
{
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public string Type { get; set; } = "";
}

public class OrderBookSnapshotData
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public List<OrderBookOrderDto> Asks { get; set; } = new();
    public List<OrderBookOrderDto> Bids { get; set; } = new();
    public OrderBookOrderDto? BestAsk { get; set; }
    public OrderBookOrderDto? BestBid { get; set; }
}

/// <summary>
/// Response from <see cref="MetaScalpClient.GetOrderBookSnapshotAsync"/>. Same shape as the WS
/// <c>orderbook_snapshot</c> message but with an additional <c>UpdateId</c> from the exchange.
/// </summary>
public class OrderBookSnapshotResponse
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public decimal UpdateId { get; set; }
    public List<OrderBookOrderDto> Asks { get; set; } = new();
    public List<OrderBookOrderDto> Bids { get; set; } = new();
    public OrderBookOrderDto? BestAsk { get; set; }
    public OrderBookOrderDto? BestBid { get; set; }
}

// ---- Cluster (footprint) snapshot ----

/// <summary>One price level of a cluster column.</summary>
public class ClusterItemDto
{
    public decimal Price { get; set; }
    /// <summary>Seller-initiated volume at this price.</summary>
    public decimal AskSize { get; set; }
    /// <summary>Buyer-initiated volume at this price.</summary>
    public decimal BidSize { get; set; }
}

public class ClusterColumnDto
{
    public DateTimeOffset StartTime { get; set; }
    public decimal AsksSum { get; set; }
    public decimal BidsSum { get; set; }
    /// <summary>Price levels ordered by price descending (highest first).</summary>
    public List<ClusterItemDto> Items { get; set; } = new();
}

/// <summary>Response of <c>GetClusterSnapshotAsync</c>. <see cref="Columns"/> is chronological: oldest first, newest last.</summary>
public class ClusterSnapshotResponse
{
    public string Ticker { get; set; } = "";
    public string TimeFrame { get; set; } = "";
    public int ZoomIndex { get; set; }
    public decimal PriceIncrement { get; set; }
    public List<ClusterColumnDto> Columns { get; set; } = new();
}

public class OrderBookUpdateData
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public List<OrderBookOrderDto> Updates { get; set; } = new();
}

public class MarkPriceUpdateData
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public decimal MarkPrice { get; set; }
}

public class FundingUpdateData
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public decimal FundingRate { get; set; }
    public DateTimeOffset FundingTime { get; set; }
}

public class NotificationDto
{
    public string Type { get; set; } = "";
    public string Exchange { get; set; } = "";
    public long ExchangeId { get; set; }
    public string ExchangeLogo { get; set; } = "";
    public string Market { get; set; } = "";
    public string MarketType { get; set; } = "";
    public string Ticker { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public string TabName { get; set; } = "";
    public string Color { get; set; } = "";
    public DateTimeOffset Date { get; set; }
}

public class NotificationSnapshotData
{
    public List<NotificationDto> Notifications { get; set; } = new();
}

public class NotificationUpdateData
{
    public List<NotificationDto> Notifications { get; set; } = new();
}

public class SignalLevelDto
{
    public long Id { get; set; }
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsTriggered { get; set; }
    public DateTimeOffset? TriggerTime { get; set; }
    public string TriggerRule { get; set; } = "";
}

public class SignalLevelsResponse
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public int Count { get; set; }
    public List<SignalLevelDto> SignalLevels { get; set; } = new();
}

public class PlaceSignalLevelRequest
{
    public string Ticker { get; set; } = "";
    public decimal Price { get; set; }
}

public class OrderBookSettingsDto
{
    // Trading
    public bool? NotificationTradeHasBeenMade { get; set; }
    public int? OrderTypeDefault { get; set; }
    public decimal? DefaultOrderCoin { get; set; }
    public decimal? DefaultOrderUsd { get; set; }
    public decimal? OrderSlippageCoin { get; set; }
    public decimal? OrderSlippageUsd { get; set; }
    public bool? CloseByMarket { get; set; }

    // OrderBook
    public decimal? AmountBarFilledAt { get; set; }
    public decimal? LargeAmount { get; set; }
    public decimal? LargeAmount2 { get; set; }
    public decimal? AmountBarFilter { get; set; }
    public decimal? AmountBarFilledAtUsd { get; set; }
    public decimal? LargeAmountUsd { get; set; }
    public decimal? LargeAmountUsd2 { get; set; }
    public decimal? AmountBarFilterUsd { get; set; }
    public bool? NotificationLargeAmountDetected { get; set; }
    public bool? NotificationLargeAmount2Detected { get; set; }
    public bool? UseLargeAmountDetectionArea { get; set; }
    public decimal? LargeAmountDetectionMinValue { get; set; }
    public decimal? LargeAmountDetectionMaxValue { get; set; }
    public string? ShowRuler { get; set; }
    public string? ZoomType { get; set; }
    /// <summary>Linear | Logarithmic — how one wheel notch moves the compression. Null on a row saved before the field existed (treated as Linear).</summary>
    public string? ZoomStepMode { get; set; }
    public bool? AutoZoom { get; set; }
    public decimal? ZoomPercent { get; set; }
    public decimal? RowHeight { get; set; }
    public decimal? SlimLevelsFactor { get; set; }
    public decimal? BasicLevelsFactor { get; set; }
    public bool? NotificationSignalLevelTriggered { get; set; }
    public bool? Autoscroll { get; set; }
    public bool? FullDepth { get; set; }

    // Ticks
    public decimal? TicksLargeAmount { get; set; }
    public decimal? TicksLargeAmountUsd { get; set; }
    public string? SizeType { get; set; }
    public bool? NotificationTradeHasBeenMadeTicks { get; set; }

    // Clusters
    public bool? ShowClusters { get; set; }
    public string? ClusterTimeFrame { get; set; }

    // General
    public bool? SoundNotification { get; set; }
}

public class OrderBookSettingsResponse
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public OrderBookSettingsDto Settings { get; set; } = new();
}

// ============ Signal Level Socket Models ============

public class SignalLevelsSnapshotData
{
    public List<SignalLevelDto> SignalLevels { get; set; } = new();
}

public class SignalLevelPlacedData
{
    public long Id { get; set; }
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsTriggered { get; set; }
    public DateTimeOffset? TriggerTime { get; set; }
    public string TriggerRule { get; set; } = "";
}

public class SignalLevelTriggeredData
{
    public long Id { get; set; }
    public DateTimeOffset TriggerTime { get; set; }
}

public class SignalLevelRemovedData
{
    public long Id { get; set; }
}

// ============ MetaBroker Analytics Models ============
// Density map / large trades / liquidations — app-wide feeds relayed from the MetaBroker
// backend (the same data the terminal's analytics windows show). Enum-like fields are
// strings on the wire; the server accepts names ("binance", "futures") or wire numbers.

public class AnalyticsExchangeMarket
{
    /// <summary>binance, gate, bybit, kucoin, bitget, mexc, okx, bingx, htx, bitmart, lbank,
    /// hyperliquid, upbit, asterdex, lighter, xt, edgex, bitunix, ourbit, whitebit, blofin,
    /// weex, polymarket.</summary>
    public string Exchange { get; set; } = "";
    /// <summary>"spot" | "futures".</summary>
    public string Market { get; set; } = "futures";
    /// <summary>"manual" | "auto" — 'auto' derives the base density size from the live order
    /// book. Default manual when omitted.</summary>
    public string? BdsMode { get; set; }
    /// <summary>Base density size in USD (manual mode, and the auto fallback). Default 1 000 000.</summary>
    public decimal? BdsValue { get; set; }
}

public class DensityMapSubscribeOptions
{
    /// <summary>Required, non-empty — an empty list is rejected.</summary>
    public List<AnalyticsExchangeMarket> ExchangeMarkets { get; set; } = new();
    /// <summary>Size-class coefficients over the effective BDS. Defaults: 3 / 2 / 1.</summary>
    public decimal? LargeCoefficient { get; set; }
    public decimal? MediumCoefficient { get; set; }
    public decimal? SmallCoefficient { get; set; }
    /// <summary>Minimum wall age per size class, minutes. Defaults: 5 / 5 / 5.</summary>
    public int? LargeLifetimeMinutes { get; set; }
    public int? MediumLifetimeMinutes { get; set; }
    public int? SmallLifetimeMinutes { get; set; }
    /// <summary>"USDT" | "USDC" | "OTHER". Default: ["USDT"].</summary>
    public List<string>? IncludedQuoteAssets { get; set; }
}

public class LargeTradesSubscribeOptions
{
    /// <summary>Required, non-empty — an empty list is rejected.</summary>
    public List<AnalyticsExchangeMarket> ExchangeMarkets { get; set; } = new();
    /// <summary>Tape-merge window, 0–60000 ms; 0 = every raw print individually. Default 500.</summary>
    public int? AggregationMs { get; set; }
    /// <summary>Optional USD floor below which prints are not emitted.</summary>
    public decimal? MinAmountUsd { get; set; }
    /// <summary>Size-class coefficients over the effective BDS. Defaults: 3 / 2 / 1.</summary>
    public decimal? LargeCoefficient { get; set; }
    public decimal? MediumCoefficient { get; set; }
    public decimal? SmallCoefficient { get; set; }
    /// <summary>"USDT" | "USDC" | "OTHER". Default: ["USDT", "USDC", "OTHER"].</summary>
    public List<string>? IncludedQuoteAssets { get; set; }
}

public class LiquidationsSubscribeOptions
{
    /// <summary>Required, non-empty. Valid names: binance, bybit, okx, bitget, gate, htx, aster, lighter.</summary>
    public List<string> Exchanges { get; set; } = new();
    /// <summary>Minimum USD size of a single liquidation. Default 0.</summary>
    public decimal? MinNotionalUsd { get; set; }
    /// <summary>Minimum notional / 24h turnover × 10000; rows with unknown turnover are dropped when set. Default off.</summary>
    public decimal? MinImpactBps { get; set; }
    /// <summary>"all" | "crypto" | "tradfi". Default all.</summary>
    public string? AssetClass { get; set; }
    /// <summary>Side of the LIQUIDATED position: "all" | "long" | "short". Default all.</summary>
    public string? Side { get; set; }
    /// <summary>Case-insensitive prefix on the resolved coin ("BTC", not "BTCUSDT"). Default "".</summary>
    public string? Coin { get; set; }
    /// <summary>"m5" | "m15" | "h1" | "h4" | "h24" — affects totals / top tokens only. Default h1.</summary>
    public string? Window { get; set; }
    /// <summary>Snapshot row count, 0–500. Default 200.</summary>
    public int? Backfill { get; set; }
}

/// <summary>One density wall notification — each wall is reported exactly once, on first sight of its id.</summary>
public class DensityMapNotificationDto
{
    public Guid Id { get; set; }
    public string Exchange { get; set; } = "";
    public string ExchangeLogo { get; set; } = "";
    /// <summary>"spot" | "futures".</summary>
    public string Market { get; set; } = "";
    public string Ticker { get; set; } = "";
    /// <summary>"ask" (sell wall) | "bid" (buy wall).</summary>
    public string Side { get; set; } = "";
    public decimal Price { get; set; }
    /// <summary>Distance from the current price, ± percent, capped ±10.</summary>
    public decimal DistancePercent { get; set; }
    public decimal SizeUsd { get; set; }
    public DateTimeOffset Time { get; set; }
}

public class DensityMapSnapshotData
{
    public List<DensityMapNotificationDto> Notifications { get; set; } = new();
}

public class DensityMapUpdateData
{
    public List<DensityMapNotificationDto> Notifications { get; set; } = new();
}

/// <summary>One aggregated trade print — final and append-only, never updated or removed.</summary>
public class LargeTradeDto
{
    public Guid Id { get; set; }
    public string Exchange { get; set; } = "";
    public string ExchangeLogo { get; set; } = "";
    /// <summary>"spot" | "futures".</summary>
    public string Market { get; set; } = "";
    public string Ticker { get; set; } = "";
    /// <summary>"buy" | "sell".</summary>
    public string Side { get; set; } = "";
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal SizeUsd { get; set; }
    public int TradeCount { get; set; }
    /// <summary>"small" | "medium" | "large".</summary>
    public string Category { get; set; } = "";
    public DateTimeOffset Time { get; set; }
}

public class LargeTradesUpdateData
{
    public List<LargeTradeDto> Trades { get; set; } = new();
}

public class LiquidationRowDto
{
    public DateTimeOffset Time { get; set; }
    /// <summary>Lowercase venue name; null for a venue the terminal does not know.</summary>
    public string? Exchange { get; set; }
    /// <summary>Exchange-native instrument (BTCUSDT, BTC-USDT-SWAP, ...).</summary>
    public string Symbol { get; set; } = "";
    /// <summary>Resolved base token (1000BONKUSDT → BONK).</summary>
    public string Coin { get; set; } = "";
    /// <summary>Side of the LIQUIDATED position: "long" = longs got liquidated (price fell).</summary>
    public string Side { get; set; } = "";
    /// <summary>"crypto" | "tradfi".</summary>
    public string AssetClass { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public decimal NotionalUsd { get; set; }
    /// <summary>NotionalUsd / 24h turnover × 10000; null when turnover is unknown.</summary>
    public decimal? ImpactBps { get; set; }
}

public class LiquidationsTotalsDto
{
    public decimal LongUsd { get; set; }
    public decimal ShortUsd { get; set; }
    public int LongCount { get; set; }
    public int ShortCount { get; set; }
}

public class TopTokenDto
{
    public string Token { get; set; } = "";
    public decimal LongUsd { get; set; }
    public decimal ShortUsd { get; set; }
    public int LongCount { get; set; }
    public int ShortCount { get; set; }
}

public class LiquidationsSnapshotData
{
    public List<LiquidationRowDto> Liquidations { get; set; } = new();
    public LiquidationsTotalsDto? Totals { get; set; }
    public List<TopTokenDto> TopTokens { get; set; } = new();
}

public class LiquidationsUpdateData
{
    public List<LiquidationRowDto> Liquidations { get; set; } = new();
}

public class LiquidationsMetadataData
{
    public LiquidationsTotalsDto? Totals { get; set; }
    public List<TopTokenDto> TopTokens { get; set; } = new();
}
