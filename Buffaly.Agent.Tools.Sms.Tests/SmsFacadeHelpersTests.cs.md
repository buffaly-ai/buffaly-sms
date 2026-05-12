# SmsFacadeHelpersTests.cs Change History

## Initial Creation (2026-03-29)
- Added focused unit tests for SMS phone normalization behavior, including dashed send formatting and digits-only read normalization.
- Added tests for cursor key semantics, facade payload mapping, and production raw request shaping (`Method` fields and expected contract members).
- Added a client-level test with a fake HTTP handler to verify `Authorization: Bearer <apiKey>` is applied and send request bodies include `Method=SendMessageRaw`.
- Design Decision: keep tests pure/unit-scoped and independent from the shared `buffaly.agent.tests` graph so this batch can compile and execute without unrelated restore blockers.

## Stabilize New HTTP Contract Tests (2026-03-29)
- Updated outbound direction test fixture to include country code so it aligns with digits-based line matching logic.
- Updated fake HTTP handler capture to persist request body text before content disposal, eliminating disposed-content assertion failures.

## Cover Read Raw Array Endpoint Shape (2026-03-29)
- Added unit test validating `GetMessagesAsync(...)` accepts production-style raw JSON array responses and still applies line filtering/mapping.
- Added assertion that read request bodies include `Method=GetMessagesByPhoneRaw` and preserved typed DateCreated parsing behavior.

## Align Tests With Provider Send/Read Behavior (2026-03-29)
- Updated send request tests to assert `FromPhone` is not serialized in outbound raw JSON payloads.
- Added read-mapping fallback test that verifies unfiltered rows are returned when requested line filtering has zero matches.

## Add Direction Signal Regression Coverage (2026-03-29)
- Added unit tests proving `IsReceived=true` maps to `Direction=inbound` and `IsReceived=false` maps to `Direction=outbound` even when `SentByPhone` does not match the requested line.
- Updated mixed-row direction test fixture to keep outbound classification aligned with `IsReceived=false`.
