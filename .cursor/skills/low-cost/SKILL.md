---
name: low-cost
description: Prefer the lowest-cost Azure/infra choices for Commerce (Table Storage over Redis/Service Bus when enough, batch writes, short TTL, event-stream SSE/poll). Use when designing SSE streaming, event streams, queues, caches, stores, or any new Azure resource.
---

# Low cost (always)

Every design and implementation choice in this repo must bias to **lowest ongoing cost** unless the user explicitly accepts a costlier option.

## Defaults

| Prefer | Avoid (unless required) |
|--------|-------------------------|
| Azure Table Storage | Redis, Cosmos, Service Bus Premium |
| Coarse / batched writes | One row per token / high QPS chatter |
| Short TTL + delete/GC | Infinite retention |
| In-process SSE on one request | Extra hops, fan-out brokers, sticky session deps |
| Reuse existing tables/conn strings | New SKUs / new services |

## Event streams (SSE + Tables)

Generic progress buffer: `IEventStreamStore` / table `commerceeventstreams` — any feature that emits progress and the client waits.

Config lives in **appsettings** for Api, Worker, and Mcp (`EventStreams` section) — not Vite:

```json
"EventStreams": {
  "Transport": "Sse",
  "PollIntervalMs": 800,
  "Ttl": "24:00:00"
}
```

- `Transport`: **`Sse`** (default) or **`TableStorage`** (client short-polls Table-backed events).
- Web reads via `GET /t/{tenant}/event-streams/settings`.
- Durable buffer: Table Storage (`ConnectionStrings:AzureTables`). No Redis Streams / SignalR / Event Hubs / WebSockets unless the user asks.

## Write thrift (Tables)

- Prefer **one row per logical event** (`status`, `progress`, `output`, `done`, `error`, …), not per LLM token/chunk.
- Fine chunks (tokens): stream on SSE only; do **not** persist each token. Persist aggregated `output` / `done` / `error` (and optional `status`).
- Cap payload size; large blobs go to existing Blob stores, not Table entities.
- Partition by `tenantId:streamId`; RowKey sortable ascending for cheap `since` reads.
- TTL: `EventStreams:Ttl` (default 24h). `EventStreamGcWorker` deletes partitions after terminal + TTL.
- Terminal events (`done` / `error` / `cancelled`) stop the client; `completed: true` on list is the same signal.

## Decision check (before coding)

1. Does this add a new paid service? → Prefer no.
2. Does this multiply transactions per user message? → Batch / coalesce.
3. Can SSE stay on the request that started the work? → Prefer yes; Table backs TableStorage transport.
4. Client short-poll? → Only if `EventStreams:Transport=TableStorage` in appsettings.

## When documenting plans

Always call out **cost levers** (writes/operation, storage TTL, services avoided) in the plan.
