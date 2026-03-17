import { MetaScalpClient } from '../src';

async function main() {
  // Discover running MetaScalp instance
  const client = await MetaScalpClient.discover();
  console.log(`Connected to MetaScalp on port ${client.port}`);

  // List connections
  const { connections } = await client.getConnections();
  console.log(`Found ${connections.length} connection(s)`);

  for (const conn of connections) {
    console.log(`  [${conn.id}] ${conn.name} — ${conn.exchange} ${conn.market} (state: ${conn.state})`);

    // Get tickers
    const { tickers } = await client.getTickers(conn.id);
    console.log(`  ${tickers.length} tickers available`);

    // Get balance
    const { balances } = await client.getBalance(conn.id);
    for (const b of balances) {
      if (b.total > 0) {
        console.log(`  Balance: ${b.coin} — total: ${b.total}, free: ${b.free}, locked: ${b.locked}`);
      }
    }
  }
}

main().catch(console.error);
