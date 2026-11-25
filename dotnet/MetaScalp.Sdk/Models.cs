using Newtonsoft.Json;

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
    /// <summary>Null when the request named no ticker (every open order of the account, MC-1131).</summary>
    public string? Ticker { get; set; }
    /// <summary>Only without a ticker: true = one account-wide exchange read, false = the Orders window's per-ticker reads.</summary>
    public bool? AccountWide { get; set; }
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

public class ComboRequest
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Ticker { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Tickers { get; set; }

    /// <summary>null (default) = not sent = the combo takes focus; false = opens in the background.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? Activate { get; set; }

    /// <summary>Connection ids from GET /api/connections: exactly these books, in this order. null = not sent.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<long>? Connections { get; set; }

    /// <summary>Exchanges as GET /api/connections returns them in «exchange» (case-insensitive): every active
    /// connection of these exchanges where the ticker trades, items in this order. A plain name converts to an
    /// item without a market (<c>Exchanges = new() { "MEXC", new("Binance", "futures") }</c>), and a
    /// <c>List&lt;string&gt;</c> or <c>string[]</c> of names converts to the whole list, as in earlier versions. Not
    /// together with <see cref="Connections"/>. null = not sent.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public ComboExchangeList? Exchanges { get; set; }

    /// <summary>Only with <see cref="Exchanges"/>: keep only this market on every listed exchange without a market
    /// of its own - "spot", "futures" (every futures/perpetual type) or an exact market type name,
    /// case-insensitive. null = not sent.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Market { get; set; }

    /// <summary>An arbitrage pair: one book per leg, in this order (the first leg on the left). Alone - with no
    /// other field but <see cref="Activate"/>; at least 2 legs. null = not sent.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<ComboLeg>? Legs { get; set; }
}

/// <summary>One <see cref="ComboRequest.Legs"/> item.</summary>
public class ComboLeg
{
    public ComboLeg(string exchange, string market, string ticker)
    {
        Exchange = exchange;
        Market = market;
        Ticker = ticker;
    }

    /// <summary>As GET /api/connections returns it in «exchange»; case, spaces and dots do not matter.</summary>
    [JsonProperty("exchange")]
    public string Exchange { get; set; }

    /// <summary>"spot", "futures" or an exact market type name, case-insensitive.</summary>
    [JsonProperty("market")]
    public string Market { get; set; }

    /// <summary>e.g. "ZKLUSDT"; the terminal finds the venue's own symbol and keeps the quote.</summary>
    [JsonProperty("ticker")]
    public string Ticker { get; set; }
}

/// <summary>POST /api/combo answer. Opened / Existing come with <see cref="ComboRequest.Legs"/> (Existing: the pair
/// was already open and was brought to the front).</summary>
public class ComboResponse : StatusResponse
{
    public bool? Opened { get; set; }

    public bool? Existing { get; set; }
}

/// <summary>POST /api/ui/windows. Geometry omitted: the window lands where it does today for its type.</summary>
public class OpenWindowRequest
{
    /// <summary>workspace, lightWorkspace, chart, tradingViewChart, watchlist, screener, lineNotifications, finres, userTrades or listing.</summary>
    public string WindowType { get; set; } = "";

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double? Top { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double? Left { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double? Height { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double? Width { get; set; }

    /// <summary>0-based target screen.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? Monitor { get; set; }

    /// <summary>Screener only: open it on this saved template.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public long? TemplateId { get; set; }

    /// <summary>chart / tradingViewChart only, with <see cref="Ticker"/>: the connection the chart opens on. Null: an empty chart, as before.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public long? ConnectionId { get; set; }

    /// <summary>chart / tradingViewChart only, with <see cref="ConnectionId"/>: the ticker, found by search on that connection (ETH finds ETHUSDT).</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Ticker { get; set; }
}

/// <summary>Where the window landed (post-clamp); <see cref="WindowId"/> is null for the singleton types.</summary>
public class OpenWindowResponse
{
    public long? WindowId { get; set; }
    public int WindowType { get; set; }
    public double Top { get; set; }
    public double Left { get; set; }
    public double Height { get; set; }
    public double Width { get; set; }
}

/// <summary>PUT /api/ui/documents/{externalId}/ticker.</summary>
public class SetDocumentTickerRequest
{
    public string Ticker { get; set; } = "";

    /// <summary>Null: the document's own connection. Required for an empty document.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public long? ConnectionId { get; set; }
}

/// <summary>The <see cref="ComboRequest.Exchanges"/> list. Converts from a list or array of plain names, so code
/// written for the earlier <c>List&lt;string&gt;</c> property still compiles.</summary>
public class ComboExchangeList : List<ComboExchange>
{
    public ComboExchangeList()
    {
    }

    public ComboExchangeList(IEnumerable<ComboExchange> items)
        : base(items)
    {
    }

    public static implicit operator ComboExchangeList(List<string> names)
    {
        return new ComboExchangeList(names.Select(n => new ComboExchange(n)));
    }

    public static implicit operator ComboExchangeList(string[] names)
    {
        return new ComboExchangeList(names.Select(n => new ComboExchange(n)));
    }
}

/// <summary>One <see cref="ComboRequest.Exchanges"/> item: the exchange and, optionally, its own market. Sent as
/// the plain name when <see cref="Market"/> is null, so older terminals still accept it.</summary>
[JsonConverter(typeof(ComboExchangeConverter))]
public class ComboExchange
{
    public ComboExchange(string exchange, string? market = null)
    {
        Exchange = exchange;
        Market = market;
    }

    public string Exchange { get; set; }

    /// <summary>"spot", "futures" or an exact market type name; null = the request's Market, or every market.</summary>
    public string? Market { get; set; }

    public static implicit operator ComboExchange(string exchange)
    {
        return new ComboExchange(exchange);
    }
}

internal class ComboExchangeConverter : JsonConverter<ComboExchange>
{
    public override void WriteJson(JsonWriter writer, ComboExchange? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        if (value.Market == null)
        {
            writer.WriteValue(value.Exchange);
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("exchange");
        writer.WriteValue(value.Exchange);
        writer.WritePropertyName("market");
        writer.WriteValue(value.Market);
        writer.WriteEndObject();
    }

    public override ComboExchange? ReadJson(JsonReader reader, Type objectType, ComboExchange? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.String)
        {
            return new ComboExchange((string)reader.Value!);
        }

        var item = Newtonsoft.Json.Linq.JObject.Load(reader);
        return new ComboExchange((string?)item["exchange"] ?? "", (string?)item["market"]);
    }
}

public class StatusResponse
{
    public string Status { get; set; } = "";
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

/// <summary>MC-898: open interest in base coin (never USD); derivative venues with open interest.</summary>
public class OpenInterestUpdateData
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public decimal OpenInterest { get; set; }
    public DateTimeOffset Time { get; set; }
    public string Unit { get; set; } = "base";
}

public class OpenInterestPoint
{
    public DateTimeOffset Time { get; set; }
    public decimal OpenInterest { get; set; }
}

public class OpenInterestHistoryResponse
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public string TimeFrame { get; set; } = "";
    public string Unit { get; set; } = "base";
    public bool HistoryAvailable { get; set; }
    public string? Reason { get; set; }
    public List<OpenInterestPoint> Points { get; set; } = new();
}

/// <summary>MET-2380: one open-interest candle by the second, from MetaScalp's cluster service (not from the exchange).</summary>
public class OpenInterestCandle
{
    /// <summary>START of the candle (UTC, on the timeframe grid).</summary>
    public DateTimeOffset Time { get; set; }
    /// <summary>The previous candle's close.</summary>
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
}

/// <summary>Response of <c>GetOpenInterestCandlesAsync</c>. <see cref="Candles"/> is oldest first.</summary>
public class OpenInterestCandlesResponse
{
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public string TimeFrame { get; set; } = "";
    public string Unit { get; set; } = "base";
    /// <summary>false when the service has nothing for the request (a contract it does not collect, a range with no
    /// candles, a time older than one day): Candles is empty, see Reason.</summary>
    public bool CandlesAvailable { get; set; }
    public string? Reason { get; set; }
    public List<OpenInterestCandle> Candles { get; set; } = new();
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
    /// <summary>Large volume levels 3 and 4 (coins, USD, their alerts): the same rows as LargeAmount / LargeAmount2.</summary>
    public decimal? LargeAmount3 { get; set; }
    public decimal? LargeAmount4 { get; set; }
    public decimal? LargeAmountUsd3 { get; set; }
    public decimal? LargeAmountUsd4 { get; set; }
    public bool? NotificationLargeAmount3Detected { get; set; }
    public bool? NotificationLargeAmount4Detected { get; set; }
    /// <summary>Show the expected PnL in $ under the stop-loss and take-profit labels.</summary>
    public bool? ShowSlTpPnl { get; set; }
    /// <summary>TradePrice | BestBidAsk — what triggers this instrument's signal levels. A row never changed reports TradePrice; any other value -> 400.</summary>
    public string? SignalLevelTriggerSource { get; set; }
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
    /// <summary>Legacy: true when AutoCenterThreshold &gt; 0; false sets it to 0, true while it is 0 sets the default.</summary>
    public bool? Autoscroll { get; set; }
    /// <summary>Auto-centering threshold, whole 0..10 (0 = off; N = re-centre beyond (10 - N)/10 of the half window height).</summary>
    public int? AutoCenterThreshold { get; set; }
    public bool? FullDepth { get; set; }

    // Ticks
    public decimal? TicksLargeAmount { get; set; }
    public decimal? TicksLargeAmountUsd { get; set; }
    public string? SizeType { get; set; }
    public bool? NotificationTradeHasBeenMadeTicks { get; set; }
    /// <summary>Paint the terminal's own executed-order overlay (fill chips + position-size chip) on the tape. Default on; absent = on.</summary>
    public bool? ShowExecutedOrders { get; set; }

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

// ============ Appearance (Settings -> Display) ============

public class AppearanceTheme
{
    public long Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Built-in themes are read-only: copy one and change the copy.</summary>
    public bool IsBuiltIn { get; set; }
    public bool IsActive { get; set; }
}

public class AppearanceGeneral
{
    public string Language { get; set; } = "";
    public string Font { get; set; } = "";
    public double OrderBookFontSize { get; set; }
    public bool HighlightPricesOnBaseLevelsFactor { get; set; }
    public int ActionBarWidth { get; set; }
    public int ClusterColumnWidth { get; set; }
    public string LimitOrderButtonPosition { get; set; } = "";
    public int ErrorDisplayDuration { get; set; }
    public string DesignType { get; set; } = "";
    public string OrderSizeChipPosition { get; set; } = "";
    public string SlTpChipPosition { get; set; } = "";
    public string PriceMarkerPosition { get; set; } = "";
    public string ClusterColorBlendMode { get; set; } = "";
}

/// <summary>Every field is optional: null = not sent = unchanged.</summary>
public class AppearanceGeneralUpdate
{
    /// <summary>ru, en, zh-Hans, hy-AM or uk-UA.</summary>
    [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
    public string? Language { get; set; }

    /// <summary>One of GET /api/appearance/font-families.</summary>
    [JsonProperty("font", NullValueHandling = NullValueHandling.Ignore)]
    public string? Font { get; set; }

    /// <summary>8-20, whole.</summary>
    [JsonProperty("orderBookFontSize", NullValueHandling = NullValueHandling.Ignore)]
    public double? OrderBookFontSize { get; set; }

    [JsonProperty("highlightPricesOnBaseLevelsFactor", NullValueHandling = NullValueHandling.Ignore)]
    public bool? HighlightPricesOnBaseLevelsFactor { get; set; }

    /// <summary>0-40.</summary>
    [JsonProperty("actionBarWidth", NullValueHandling = NullValueHandling.Ignore)]
    public int? ActionBarWidth { get; set; }

    /// <summary>0-200; a change sets the cluster width of every order book.</summary>
    [JsonProperty("clusterColumnWidth", NullValueHandling = NullValueHandling.Ignore)]
    public int? ClusterColumnWidth { get; set; }

    /// <summary>Left or Right.</summary>
    [JsonProperty("limitOrderButtonPosition", NullValueHandling = NullValueHandling.Ignore)]
    public string? LimitOrderButtonPosition { get; set; }

    /// <summary>1-7 seconds.</summary>
    [JsonProperty("errorDisplayDuration", NullValueHandling = NullValueHandling.Ignore)]
    public int? ErrorDisplayDuration { get; set; }

    /// <summary>Standard or Simplified.</summary>
    [JsonProperty("designType", NullValueHandling = NullValueHandling.Ignore)]
    public string? DesignType { get; set; }

    /// <summary>Left or Right: the order-size chips («Объём заявок»).</summary>
    [JsonProperty("orderSizeChipPosition", NullValueHandling = NullValueHandling.Ignore)]
    public string? OrderSizeChipPosition { get; set; }

    /// <summary>Left or Right: the SL / TP labels.</summary>
    [JsonProperty("slTpChipPosition", NullValueHandling = NullValueHandling.Ignore)]
    public string? SlTpChipPosition { get; set; }

    /// <summary>Left or Right: the MP / IP / LP price markers.</summary>
    [JsonProperty("priceMarkerPosition", NullValueHandling = NullValueHandling.Ignore)]
    public string? PriceMarkerPosition { get; set; }

    /// <summary>Classic or Soft: cluster colour blending.</summary>
    [JsonProperty("clusterColorBlendMode", NullValueHandling = NullValueHandling.Ignore)]
    public string? ClusterColorBlendMode { get; set; }
}

public class AppearanceState
{
    public long ActiveThemeId { get; set; }
    public List<AppearanceTheme> Themes { get; set; } = new();
    public AppearanceGeneral General { get; set; } = new();
}

public class FontFamiliesResponse
{
    public int Count { get; set; }
    public List<string> Fonts { get; set; } = new();
}

public class CreateThemeRequest
{
    /// <summary>null = «&lt;source&gt; (N)».</summary>
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    /// <summary>Theme to copy; null = the active theme.</summary>
    [JsonProperty("copyFromThemeId", NullValueHandling = NullValueHandling.Ignore)]
    public long? CopyFromThemeId { get; set; }
}

public class DeleteThemeResponse
{
    public bool Deleted { get; set; }
    public long ThemeId { get; set; }
}

public class AppearanceColor
{
    public string Key { get; set; } = "";

    /// <summary>application, timeAndSales, chart or infoPanel.</summary>
    public string Section { get; set; } = "";
    public string Label { get; set; } = "";

    /// <summary>#AARRGGBB, or Light / Dark for a choice row.</summary>
    public string? Value { get; set; }

    /// <summary>Allowed values of a choice row; null for a colour row.</summary>
    public List<string>? Options { get; set; }
}

public class ThemeColorsResponse
{
    public long ThemeId { get; set; }
    public List<AppearanceColor> Colors { get; set; } = new();
}

public class AppearanceTextStyle
{
    public string Element { get; set; } = "";

    /// <summary>orderBook, tapeClusters, windows or charts.</summary>
    public string Group { get; set; } = "";
    public string Label { get; set; } = "";
    public double? Size { get; set; }
    public bool? Bold { get; set; }
    public bool? Italic { get; set; }
    public string? Color { get; set; }

    /// <summary>false for the two chip-background rows: colour only.</summary>
    public bool HasStyle { get; set; }
}

/// <summary>Every field is optional: null = not sent = unchanged.</summary>
public class AppearanceTextStyleUpdate
{
    [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
    public double? Size { get; set; }

    [JsonProperty("bold", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Bold { get; set; }

    [JsonProperty("italic", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Italic { get; set; }

    [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
    public string? Color { get; set; }
}

public class ThemeTextStylesResponse
{
    public long ThemeId { get; set; }
    public List<AppearanceTextStyle> TextStyles { get; set; } = new();
}

// ============ Chart settings (Settings -> Chart) ============

/// <summary>The rows of Settings -> Chart (terminal-wide: one value for every chart, SciChart and TradingView),
/// plus <see cref="Allowed"/> and, after an update, <see cref="Changed"/>.</summary>
public class ChartSettings
{
    /// <summary>None, Histogram or Chart: the open-interest pane («Отображение открытого интереса»). The pane appears
    /// only on markets with open interest.</summary>
    public string OpenInterestDisplayMode { get; set; } = "";

    /// <summary>Timeframe a new chart opens on (S1 ... D1).</summary>
    public string DefaultTimeFrame { get; set; } = "";

    /// <summary>Timeframes offered in a chart's timeframe list.</summary>
    public List<string> TimeFrames { get; set; } = new();

    /// <summary>Volume pane.</summary>
    public bool ShowVolume { get; set; }

    /// <summary>Volume pane height: 15 (small), 25 (medium) or 30 (large).</summary>
    public double VolumeHeight { get; set; }

    /// <summary>Time left until the candle closes.</summary>
    public bool ShowTimeFrameCountdown { get; set; }

    /// <summary>Candle parameters (open, high, low, close) on hover.</summary>
    public bool ShowCandleParameters { get; set; }
    public bool ShowUserLevels { get; set; }
    public bool ShowSignalLevels { get; set; }

    /// <summary>Own trades on every chart.</summary>
    public bool ShowTradesOnAllCharts { get; set; }

    /// <summary>Own orders on every chart.</summary>
    public bool ShowOrdersOnAllCharts { get; set; }

    /// <summary>Percent from the current price at the crosshair.</summary>
    public bool ShowPercentFromCurrentPrice { get; set; }

    /// <summary>Drawing magnet.</summary>
    public bool MagnetEnabled { get; set; }

    /// <summary>Weak, Medium or Strong.</summary>
    public string MagnetMode { get; set; } = "";

    /// <summary>SeparateWindow or Docked: where a chart opened from an order book goes.</summary>
    public string OpenChartFromOrderBook { get; set; } = "";

    /// <summary>Off or On: moving and cancelling orders and setting SL / TP by dragging on the chart.</summary>
    public string ChartOrderManagement { get; set; } = "";

    /// <summary>Whether each chart order action asks first.</summary>
    public bool ConfirmChartOrderActions { get; set; }

    /// <summary>Automatic levels: fractal width, 1-10.</summary>
    public int AutoLevelFractalWidth { get; set; }

    /// <summary>Automatic levels: candles scanned, 100-5000.</summary>
    public int AutoLevelHistoryDepth { get; set; }

    /// <summary>Automatic levels: levels drawn per side, 1-100.</summary>
    public int AutoLevelRenderedCap { get; set; }

    /// <summary>UpdateChartSettingsAsync only: the fields this request changed (a field sent with its current value is
    /// not one of them); null on a read.</summary>
    public List<string>? Changed { get; set; }

    /// <summary>The names and limits each choice row takes.</summary>
    public ChartSettingsAllowed Allowed { get; set; } = new();
}

public class ChartSettingsRange
{
    public int Min { get; set; }
    public int Max { get; set; }
}

public class ChartSettingsAllowed
{
    public List<string> OpenInterestDisplayMode { get; set; } = new();
    public List<string> TimeFrames { get; set; } = new();
    public List<double> VolumeHeight { get; set; } = new();
    public List<string> MagnetMode { get; set; } = new();
    public List<string> OpenChartFromOrderBook { get; set; } = new();
    public List<string> ChartOrderManagement { get; set; } = new();
    public ChartSettingsRange AutoLevelFractalWidth { get; set; } = new();
    public ChartSettingsRange AutoLevelHistoryDepth { get; set; } = new();
    public ChartSettingsRange AutoLevelRenderedCap { get; set; } = new();
}

/// <summary>Every field is optional: null = not sent = unchanged.</summary>
public class ChartSettingsUpdate
{
    /// <summary>None, Histogram or Chart.</summary>
    [JsonProperty("openInterestDisplayMode", NullValueHandling = NullValueHandling.Ignore)]
    public string? OpenInterestDisplayMode { get; set; }

    /// <summary>S1 ... D1.</summary>
    [JsonProperty("defaultTimeFrame", NullValueHandling = NullValueHandling.Ignore)]
    public string? DefaultTimeFrame { get; set; }

    /// <summary>Replaces the whole list; needs at least one timeframe.</summary>
    [JsonProperty("timeFrames", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? TimeFrames { get; set; }

    [JsonProperty("showVolume", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ShowVolume { get; set; }

    /// <summary>15, 25 or 30.</summary>
    [JsonProperty("volumeHeight", NullValueHandling = NullValueHandling.Ignore)]
    public double? VolumeHeight { get; set; }

    [JsonProperty("showTimeFrameCountdown", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ShowTimeFrameCountdown { get; set; }

    [JsonProperty("showCandleParameters", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ShowCandleParameters { get; set; }

    [JsonProperty("showUserLevels", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ShowUserLevels { get; set; }

    [JsonProperty("showSignalLevels", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ShowSignalLevels { get; set; }

    [JsonProperty("showTradesOnAllCharts", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ShowTradesOnAllCharts { get; set; }

    [JsonProperty("showOrdersOnAllCharts", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ShowOrdersOnAllCharts { get; set; }

    [JsonProperty("showPercentFromCurrentPrice", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ShowPercentFromCurrentPrice { get; set; }

    [JsonProperty("magnetEnabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? MagnetEnabled { get; set; }

    /// <summary>Weak, Medium or Strong.</summary>
    [JsonProperty("magnetMode", NullValueHandling = NullValueHandling.Ignore)]
    public string? MagnetMode { get; set; }

    /// <summary>SeparateWindow or Docked.</summary>
    [JsonProperty("openChartFromOrderBook", NullValueHandling = NullValueHandling.Ignore)]
    public string? OpenChartFromOrderBook { get; set; }

    /// <summary>Off or On.</summary>
    [JsonProperty("chartOrderManagement", NullValueHandling = NullValueHandling.Ignore)]
    public string? ChartOrderManagement { get; set; }

    [JsonProperty("confirmChartOrderActions", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ConfirmChartOrderActions { get; set; }

    /// <summary>1-10.</summary>
    [JsonProperty("autoLevelFractalWidth", NullValueHandling = NullValueHandling.Ignore)]
    public int? AutoLevelFractalWidth { get; set; }

    /// <summary>100-5000.</summary>
    [JsonProperty("autoLevelHistoryDepth", NullValueHandling = NullValueHandling.Ignore)]
    public int? AutoLevelHistoryDepth { get; set; }

    /// <summary>1-100.</summary>
    [JsonProperty("autoLevelRenderedCap", NullValueHandling = NullValueHandling.Ignore)]
    public int? AutoLevelRenderedCap { get; set; }
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

/// <summary>A user (plain) level as sent on the user_level_* WebSocket family.</summary>
public class UserLevelDto
{
    public long Id { get; set; }
    public long ConnectionId { get; set; }
    public string Ticker { get; set; } = "";
    public decimal Price { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset? Date { get; set; }
    public string? Note { get; set; }
    public double? LineThickness { get; set; }
    public string? LineStyle { get; set; }
    public string? LineColor { get; set; }
    public double? TextSize { get; set; }
    public string? TextColor { get; set; }
    public string? TextAlignment { get; set; }
    public string? TextStyle { get; set; }
}

/// <summary>Sent after every user_level_subscribe (empty list when nothing matches the optional filter).</summary>
public class UserLevelsSnapshotData
{
    public List<UserLevelDto> UserLevels { get; set; } = new();
}

public class UserLevelRemovedData
{
    public List<long> Ids { get; set; } = new();
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
}

/// <summary>One exchange market of a large trades subscription — the pair plus its manual/auto BDS choice.
/// The density map has no manual BDS (MetaBroker MB-702): a bdsMode "manual" or a bdsValue sent to
/// density_map_subscribe is refused with an error frame naming the field (MET-1828).</summary>
public class LargeTradesExchangeMarket : AnalyticsExchangeMarket
{
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
    /// <summary>The window's «asset classes» filter: any of "crypto" | "stablecoins" | "commodities" | "stocks" |
    /// "indices". null, empty or all five = no filter; an unknown name is refused with an error frame naming the field.</summary>
    public List<string>? AssetClasses { get; set; }
}

public class LargeTradesSubscribeOptions
{
    /// <summary>Required, non-empty — an empty list is rejected.</summary>
    public List<LargeTradesExchangeMarket> ExchangeMarkets { get; set; } = new();
    /// <summary>Tape-merge window, 0–60000 ms; 0 = every raw print individually. Default 500.</summary>
    public int? AggregationMs { get; set; }
    /// <summary>Optional USD floor below which prints are not emitted — applied by the terminal on top of the BDS
    /// threshold (MET-1832).</summary>
    public decimal? MinAmountUsd { get; set; }
    /// <summary>Size-class coefficients over the effective BDS. Defaults: 3 / 2 / 1.</summary>
    public decimal? LargeCoefficient { get; set; }
    public decimal? MediumCoefficient { get; set; }
    public decimal? SmallCoefficient { get; set; }
    /// <summary>"USDT" | "USDC" | "OTHER". Default: ["USDT", "USDC", "OTHER"].</summary>
    public List<string>? IncludedQuoteAssets { get; set; }
    /// <summary>The window's «asset classes» filter: any of "crypto" | "stablecoins" | "commodities" | "stocks" |
    /// "indices". null, empty or all five = no filter; an unknown name is refused with an error frame naming the field.</summary>
    public List<string>? AssetClasses { get; set; }
}

public class LiquidationsSubscribeOptions
{
    /// <summary>Required, non-empty. Valid names: binance, bybit, okx, bitget, gate, htx, aster, lighter.</summary>
    public List<string> Exchanges { get; set; } = new();
    /// <summary>Minimum USD size of a single liquidation. Default 0.</summary>
    public decimal? MinNotionalUsd { get; set; }
    /// <summary>Minimum notional / 24h turnover × 10000; rows with unknown turnover are dropped when set. Default off.</summary>
    public decimal? MinImpactBps { get; set; }
    /// <summary>"all" | "crypto" | "tradfi". Default all. The class is the one the terminal's Liquidations window
    /// shows for the coin (MET-1829); a live row of a coin first seen in the session waits up to 2 s for it.</summary>
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
    /// <summary>"crypto" | "tradfi" — the class the terminal's Liquidations window shows for the coin (MET-1829).</summary>
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
