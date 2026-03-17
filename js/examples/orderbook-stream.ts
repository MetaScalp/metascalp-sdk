import { MetaScalpClient, MetaScalpSocket, OrderBookSnapshotData, OrderBookUpdateData } from '../src';

// Simple in-memory order book
const asks = new Map<number, number>();
const bids = new Map<number, number>();

function applySnapshot(data: OrderBookSnapshotData) {
  asks.clear();
  bids.clear();
  for (const o of data.asks) asks.set(o.price, o.size);
  for (const o of data.bids) bids.set(o.price, o.size);
}

function applyUpdate(data: OrderBookUpdateData) {
  for (const o of data.updates) {
    const map = (o.type === 'Ask' || o.type === 'BestAsk') ? asks : bids;
    if (o.size <= 0) {
      map.delete(o.price);
    } else {
      map.set(o.price, o.size);
    }
  }
}

function printTopOfBook(n = 5) {
  const topAsks = [...asks.entries()].sort((a, b) => a[0] - b[0]).slice(0, n).reverse();
  const topBids = [...bids.entries()].sort((a, b) => b[0] - a[0]).slice(0, n);

  console.clear();
  console.log('--- ORDER BOOK ---');
  console.log('  SIZE       PRICE (ASK)');
  for (const [price, size] of topAsks) {
    console.log(`  ${size.toFixed(2).padStart(10)}  \x1b[31m${price}\x1b[0m`);
  }
  if (topAsks.length > 0 && topBids.length > 0) {
    const spread = topAsks[topAsks.length - 1][0] - topBids[0][0];
    console.log(`  ---- spread: ${spread.toFixed(4)} ----`);
  }
  console.log('  SIZE       PRICE (BID)');
  for (const [price, size] of topBids) {
    console.log(`  ${size.toFixed(2).padStart(10)}  \x1b[32m${price}\x1b[0m`);
  }
}

async function main() {
  const client = await MetaScalpClient.discover();
  const { connections } = await client.getConnections();
  const conn = connections[0];

  const socket = await MetaScalpSocket.discover();

  const ticker = 'BTCUSDT';
  socket.subscribeOrderBook(conn.id, ticker);
  await socket.waitFor('orderbook_subscribed');
  console.log(`Subscribed to order book for ${ticker}`);

  socket.on('orderbook_snapshot', (data) => {
    applySnapshot(data);
    printTopOfBook();
  });

  socket.on('orderbook_update', (data) => {
    applyUpdate(data);
    printTopOfBook();
  });

  console.log('Streaming order book... (Ctrl+C to stop)');
  process.on('SIGINT', () => {
    socket.unsubscribeOrderBook(conn.id, ticker);
    socket.disconnect();
    process.exit(0);
  });
}

main().catch(console.error);
