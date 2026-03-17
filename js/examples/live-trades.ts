import { MetaScalpClient, MetaScalpSocket } from '../src';

async function main() {
  // Discover MetaScalp
  const client = await MetaScalpClient.discover();
  const { connections } = await client.getConnections();
  const conn = connections[0];
  console.log(`Using connection [${conn.id}] ${conn.name}`);

  // Connect WebSocket
  const socket = await MetaScalpSocket.discover();
  console.log(`WebSocket connected on port ${socket.port}`);

  // Subscribe to trades
  const ticker = 'BTCUSDT';
  socket.subscribeTrades(conn.id, ticker);
  await socket.waitFor('trade_subscribed');
  console.log(`Subscribed to trades for ${ticker}`);

  // Listen for trade updates
  socket.on('trade_update', (data) => {
    for (const trade of data.trades) {
      const side = trade.side === 'Buy' ? '\x1b[32mBUY \x1b[0m' : '\x1b[31mSELL\x1b[0m';
      console.log(`${side} ${trade.price} x ${trade.size} @ ${trade.time}`);
    }
  });

  // Listen for errors
  socket.on('error', (data) => {
    console.error('Socket error:', data.error);
  });

  // Keep running
  console.log('Listening for trades... (Ctrl+C to stop)');
  process.on('SIGINT', () => {
    socket.unsubscribeTrades(conn.id, ticker);
    socket.disconnect();
    process.exit(0);
  });
}

main().catch(console.error);
