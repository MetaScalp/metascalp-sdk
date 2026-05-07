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
