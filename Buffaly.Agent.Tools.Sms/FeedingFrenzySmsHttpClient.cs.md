# FeedingFrenzySmsHttpClient.cs Change History

## Initial Creation (2026-03-29)
- Added `IFeedingFrenzySmsClient` and `FeedingFrenzySmsHttpClient` for typed HTTP access to FeedingFrenzy JSONWS SMS endpoints.
- Added `FeedingFrenzySmsClientSettings` to keep provider URL and endpoint paths explicit and configurable.
- Design Decision: enforce fail-fast behavior for non-success HTTP responses and invalid JSON payloads to prevent hidden contract drift.

## Add API Key Bearer Auth and Raw Contract Mapping (2026-03-29)
- Extended client settings to require API key configuration and added fail-fast validation when missing.
- Updated HTTP execution path to attach `Authorization: Bearer <apiKey>` on every request.
- Routed send/read through explicit raw contract mapping (`SmsRequestContractMapper`) before posting and after response parsing.
- Design Decision: keep one explicit translation boundary for external endpoint contracts while preserving typed facade-level client methods.

## Preserve PascalCase JSON Contract Field Names (2026-03-29)
- Updated serializer options to disable naming-policy conversion so outbound JSON uses contract property names exactly as declared (for example `Method`, `ToPhone`, `SinceMessageID`).
- Reason: production endpoint contract is PascalCase-driven and should not receive camelCase request keys.

## Deserialize Read Responses From Raw Arrays (2026-03-29)
- Updated read-side deserialization to parse `List<FeedingFrenzyMessage>` directly from endpoint JSON instead of expecting a wrapped object.
- Added a focused custom DateTime JSON converter to robustly parse observed date string formats while keeping strong typed contracts.

## Route Reads Through Line-Specific Raw Contract Path (2026-03-29)
- Updated read-call path to use the authoritative `GetMessagesByPhoneRawByLine` request contract (including `LinePhone`) from the mapper.
- Removed client-side line parameter threading and local fallback filtering from the read mapping call chain.
- Design Decision: keep the provider endpoint as the single source of truth for line filtering and avoid duplicate filtering behavior in the client.
