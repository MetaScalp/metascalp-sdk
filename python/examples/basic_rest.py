"""Basic REST API usage example."""

import asyncio
from metascalp import MetaScalpClient


async def main():
    # Discover running MetaScalp instance
    client = await MetaScalpClient.discover()
    print(f"Connected to MetaScalp on port {client.port}")

    # List connections
    data = await client.get_connections()
    connections = data["connections"]
    print(f"Found {len(connections)} connection(s)")

    for conn in connections:
        print(f"  [{conn['id']}] {conn['name']} — {conn['exchange']} {conn['market']}")

        # Get balance
        balance_data = await client.get_balance(conn["id"])
        for b in balance_data["balances"]:
            if b["total"] > 0:
                print(f"    {b['coin']}: total={b['total']}, free={b['free']}, locked={b['locked']}")

    await client.close()


asyncio.run(main())
