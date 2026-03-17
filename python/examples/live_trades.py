"""Stream live trades from MetaScalp."""

import asyncio
from metascalp import MetaScalpClient, MetaScalpSocket


async def main():
    # Find connection
    client = await MetaScalpClient.discover()
    data = await client.get_connections()
    conn = data["connections"][0]
    await client.close()
    print(f"Using connection [{conn['id']}] {conn['name']}")

    # Connect WebSocket
    socket = await MetaScalpSocket.discover()
    print(f"WebSocket connected on port {socket.port}")

    # Register trade handler
    @socket.on("trade_update")
    def on_trade(data):
        for trade in data["trades"]:
            side = "\033[32mBUY \033[0m" if trade["side"] == "Buy" else "\033[31mSELL\033[0m"
            print(f"  {side} {trade['price']} x {trade['size']} @ {trade['time']}")

    @socket.on("trade_subscribed")
    def on_subscribed(data):
        print(f"Subscribed to trades for {data['ticker']}")

    # Subscribe
    ticker = "BTCUSDT"
    socket.subscribe_trades(conn["id"], ticker)

    print("Listening for trades... (Ctrl+C to stop)")
    await socket.listen_forever()


asyncio.run(main())
