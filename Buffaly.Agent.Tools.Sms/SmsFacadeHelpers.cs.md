# SmsFacadeHelpers.cs Change History

## Initial Creation (2026-03-29)
- Added reusable helper classes for phone normalization, cursor-key generation, and facade payload mapping.
- Added deterministic mapping helpers for send/read payload shapes so unit tests can validate behavior without network calls.
- Design Decision: extract pure helper logic from `SmsFacade` to keep facade orchestration thin and testable.

## Add Send Phone Formatting and External Contract Mappers (2026-03-29)
- Added `NormalizeSendPhone(...)` for known-good dashed destination formatting on send operations.
- Added `SmsRequestContractMapper` helper to shape raw send/read provider request bodies and map raw responses to internal contracts.
- Added explicit line filtering in read response mapping so `SmsFacade` line semantics remain stable after switching to `GetMessagesByPhoneRaw`.
- Design Decision: keep external contract translation isolated in one shared helper path for deterministic unit testing.

## Accept Raw Read Arrays At Mapping Boundary (2026-03-29)
- Updated read raw mapper signature to accept a typed message collection directly rather than a wrapped raw response object.
- Design Decision: preserve existing line filtering and facade payload behavior while matching production endpoint array output.

## Send Contract And Read Fallback Alignment (2026-03-29)
- Updated send raw request mapper to omit `FromPhone` in outbound provider request payloads.
- Updated read raw mapping to fall back to unfiltered conversation rows when strict requested-line filtering yields zero matches.
- Design Decision: keep conversations visible when provider uses different actual line numbers while preserving strict filtering when matches exist.

## Prioritize IsReceived For Direction Classification (2026-03-29)
- Updated read payload direction mapping to use `IsReceived` as the primary signal (`true` => inbound, `false` => outbound) before any phone heuristics.
- Design Decision: avoid misclassifying provider rows when actual `SentByPhone` differs from the requested line under test.

## Use Line-Specific Raw Read Contract Without Local Filtering (2026-03-29)
- Updated raw read request mapping to call `GetMessagesByPhoneRawByLine` and include `LinePhone` on the typed request contract.
- Removed local line-filter/fallback logic in raw read response mapping and now pass provider-filtered rows through directly.
- Design Decision: keep a single authoritative filtering source at the provider method boundary to prevent contract drift and hidden normalization.
