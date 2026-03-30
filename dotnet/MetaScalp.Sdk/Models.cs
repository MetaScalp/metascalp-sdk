using Newtonsoft.Json;

namespace MetaScalp.Sdk;

// ============ REST Models ============

public class PingResponse
{
    [JsonProperty("app")] public string App { get; set; } = "";
    [JsonProperty("version")] public string Version { get; set; } = "";
}

public class ConnectionDto
{
    [JsonProperty("id")] public long Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; } = "";
    [JsonProperty("exchange")] public string Exchange { get; set; } = "";
    [JsonProperty("exchangeId")] public int ExchangeId { get; set; }
    [JsonProperty("market")] public string Market { get; set; } = "";
    [JsonProperty("marketType")] public int MarketType { get; set; }
    [JsonProperty("state")] public int State { get; set; }
    [JsonProperty("viewMode")] public bool ViewMode { get; set; }
    [JsonProperty("demoMode")] public bool DemoMode { get; set; }
}

public class ConnectionsResponse
{
    [JsonProperty("connections")] public List<ConnectionDto> Connections { get; set; } = new();
}

public class TickerDto
{
    [JsonProperty("name")] public string Name { get; set; } = "";
    [JsonProperty("baseAsset")] public string BaseAsset { get; set; } = "";
    [JsonProperty("quoteAsset")] public string QuoteAsset { get; set; } = "";
    [JsonProperty("isTradingAllowed")] public bool IsTradingAllowed { get; set; }
    [JsonProperty("priceIncrement")] public decimal PriceIncrement { get; set; }
    [JsonProperty("sizeIncrement")] public decimal SizeIncrement { get; set; }
    [JsonProperty("minSize")] public decimal MinSize { get; set; }
    [JsonProperty("maxSize")] public decimal? MaxSize { get; set; }
}

public class TickersResponse
{
    [JsonProperty("connectionId")] public long ConnectionId { get; set; }
    [JsonProperty("count")] public int Count { get; set; }
    [JsonProperty("tickers")] public List<TickerDto> Tickers { get; set; } = new();
}

public class OrderDto
{
    [JsonProperty("id")] public long Id { get; set; }
    [JsonProperty("ticker")] public string Ticker { get; set; } = "";
    [JsonProperty("clientId")] public string? ClientId { get; set; }
    [JsonProperty("side")] public int Side { get; set; }
    [JsonProperty("price")] public decimal Price { get; set; }
    [JsonProperty("size")] public decimal Size { get; set; }
    [JsonProperty("filledSize")] public decimal FilledSize { get; set; }
    [JsonProperty("filledPrice")] public decimal FilledPrice { get; set; }
    [JsonProperty("remainingSize")] public decimal RemainingSize { get; set; }
    [JsonProperty("status")] public int Status { get; set; }
    [JsonProperty("type")] public int Type { get; set; }
    [JsonProperty("triggerPrice")] public decimal? TriggerPrice { get; set; }
    [JsonProperty("createDate")] public string CreateDate { get; set; } = "";
}

public class OrdersResponse
{
    [JsonProperty("connectionId")] public long ConnectionId { get; set; }
    [JsonProperty("ticker")] public string Ticker { get; set; } = "";
    [JsonProperty("count")] public int Count { get; set; }
    [JsonProperty("orders")] public List<OrderDto> Orders { get; set; } = new();
}

public class PositionDto
{
    [JsonProperty("id")] public long Id { get; set; }
    [JsonProperty("ticker")] public string Ticker { get; set; } = "";
    [JsonProperty("side")] public int Side { get; set; }
    [JsonProperty("size")] public decimal Size { get; set; }
    [JsonProperty("avgPrice")] public decimal AvgPrice { get; set; }
    [JsonProperty("marginMode")] public int MarginMode { get; set; }
}

public class PositionsResponse
{
    [JsonProperty("connectionId")] public long ConnectionId { get; set; }
    [JsonProperty("count")] public int Count { get; set; }
    [JsonProperty("positions")] public List<PositionDto> Positions { get; set; } = new();
}

public class BalanceDto
{
    [JsonProperty("coin")] public string Coin { get; set; } = "";
    [JsonProperty("total")] public decimal Total { get; set; }
    [JsonProperty("free")] public decimal Free { get; set; }
    [JsonProperty("locked")] public decimal Locked { get; set; }
}

public class BalanceResponse
{
    [JsonProperty("connectionId")] public long ConnectionId { get; set; }
    [JsonProperty("count")] public int Count { get; set; }
    [JsonProperty("balances")] public List<BalanceDto> Balances { get; set; } = new();
}

public class PlaceOrderRequest
{
    [JsonProperty("ticker")] public string Ticker { get; set; } = "";
    [JsonProperty("side")] public int Side { get; set; }
    [JsonProperty("price")] public decimal Price { get; set; }
    [JsonProperty("size")] public decimal Size { get; set; }
    [JsonProperty("type")] public int Type { get; set; }
    [JsonProperty("reduceOnly")] public bool ReduceOnly { get; set; }
}

public class PlaceOrderResponse
{
    [JsonProperty("status")] public string Status { get; set; } = "";
    [JsonProperty("clientId")] public string ClientId { get; set; } = "";
    [JsonProperty("executionTimeMs")] public double ExecutionTimeMs { get; set; }
}

public class CancelOrderRequest
{
    [JsonProperty("ticker")] public string Ticker { get; set; } = "";
    [JsonProperty("orderId")] public long OrderId { get; set; }
    [JsonProperty("type")] public int Type { get; set; }
}

// ============ Socket Models ============

public class OrderUpdateData
{
    [JsonProperty("connectionId")] public long ConnectionId { get; set; }
    [JsonProperty("orderId")] public long OrderId { get; set; }
    [JsonProperty("ticker")] public string Ticker { get; set; } = "";
    [JsonProperty("side")] public string Side { get; set; } = "";
    [JsonProperty("type")] public string Type { get; set; } = "";
    [JsonProperty("price")] public decimal Price { get; set; }
    [JsonProperty("filledPrice")] public decimal FilledPrice { get; set; }
    [JsonProperty("size")] public decimal Size { get; set; }
    [JsonProperty("filledSize")] public decimal FilledSize { get; set; }
    [JsonProperty("fee")] public decimal Fee { get; set; }
    [JsonProperty("feeCurrency")] public string? FeeCurrency { get; set; }
    [JsonProperty("status")] public string Status { get; set; } = "";
    [JsonProperty("time")] public DateTimeOffset Time { get; set; }
}

public class PositionUpdateData
{
    [JsonProperty("connectionId")] public long ConnectionId { get; set; }
    [JsonProperty("positionId")] public long PositionId { get; set; }
    [JsonProperty("ticker")] public string Ticker { get; set; } = "";
    [JsonProperty("side")] public string Side { get; set; } = "";
    [JsonProperty("size")] public decimal Size { get; set; }
    [JsonProperty("avgPrice")] public decimal AvgPrice { get; set; }
    [JsonProperty("avgPriceFix")] public decimal AvgPriceFix { get; set; }
    [JsonProperty("avgPriceDyn")] public decimal AvgPriceDyn { get; set; }
    [JsonProperty("status")] public string Status { get; set; } = "";
}

public class BalanceUpdateData
{
    [JsonProperty("connectionId")] public long ConnectionId { get; set; }
    [JsonProperty("balances")] public List<BalanceDto> Balances { get; set; } = new();
}

public class FinresDto
{
    [JsonProperty("currency")] public string Currency { get; set; } = "";
    [JsonProperty("result")] public decimal Result { get; set; }
    [JsonProperty("fee")] public decimal Fee { get; set; }
    [JsonProperty("funds")] public decimal Funds { get; set; }
    [JsonProperty("available")] public decimal Available { get; set; }
    [JsonProperty("blocked")] public decimal Blocked { get; set; }
}

public class FinresUpdateData
{
    [JsonProperty("connectionId")] public long ConnectionId { get; set; }
    [JsonProperty("finreses")] public List<FinresDto> Finreses { get; set; } = new();
}

public class TradeDto
{
    [JsonProperty("price")] public decimal Price { get; set; }
    [JsonProperty("size")] public decimal Size { get; set; }
    [JsonProperty("side")] public string Side { get; set; } = "";
    [JsonProperty("time")] public DateTimeOffset Time { get; set; }
}

public class TradeUpdateData
{
    [JsonProperty("connectionId")] public long ConnectionId { get; set; }
    [JsonProperty("ticker")] public string Ticker { get; set; } = "";
    [JsonProperty("trades")] public List<TradeDto> Trades { get; set; } = new();
}

public class OrderBookOrderDto
{
    [JsonProperty("price")] public decimal Price { get; set; }
    [JsonProperty("size")] public decimal Size { get; set; }
    [JsonProperty("type")] public string Type { get; set; } = "";
}

public class OrderBookSnapshotData
{
    [JsonProperty("connectionId")] public long ConnectionId { get; set; }
    [JsonProperty("ticker")] public string Ticker { get; set; } = "";
    [JsonProperty("asks")] public List<OrderBookOrderDto> Asks { get; set; } = new();
    [JsonProperty("bids")] public List<OrderBookOrderDto> Bids { get; set; } = new();
    [JsonProperty("bestAsk")] public OrderBookOrderDto? BestAsk { get; set; }
    [JsonProperty("bestBid")] public OrderBookOrderDto? BestBid { get; set; }
}

public class OrderBookUpdateData
{
    [JsonProperty("connectionId")] public long ConnectionId { get; set; }
    [JsonProperty("ticker")] public string Ticker { get; set; } = "";
    [JsonProperty("updates")] public List<OrderBookOrderDto> Updates { get; set; } = new();
}
