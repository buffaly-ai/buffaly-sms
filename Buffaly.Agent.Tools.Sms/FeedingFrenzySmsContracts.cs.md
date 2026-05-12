# FeedingFrenzySmsContracts.cs Change History

## Initial Creation (2026-03-29)
- Added authoritative typed request/response contracts for SMS send/read calls to FeedingFrenzy JSONWS.
- Added one shared `FeedingFrenzyMessage` contract used by facade mapping and tests.
- Design Decision: keep one typed contract surface for host-to-provider flow and avoid ad-hoc JSON handling in core SMS paths.

## Add Production Raw JSONWS Contract DTOs (2026-03-29)
- Added raw send/read request DTOs with explicit `Method` fields (`SendMessageRaw`, `GetMessagesByPhoneRaw`) and production contract member names.
- Added raw send/read response DTOs to support one explicit external-boundary mapping path back into internal facade contracts.
- Added optional `MetadataJson` support on send requests for provider contract completeness.

## Align Read Raw Contract To Array Response Shape (2026-03-29)
- Removed wrapped read raw response DTO so the HTTP boundary can deserialize directly from the production raw JSON array.
- Design Decision: keep the external read contract aligned to observed endpoint output and map to facade response DTOs after deserialization.

## Remove FromPhone From Raw Send Request Contract (2026-03-29)
- Removed `FromPhone` from `FeedingFrenzySendMessageRawRequest` so outbound JSON matches verified provider behavior for `send-message-raw`.
- Design Decision: keep explicit requested line evidence in facade payload only and avoid failing provider ownership checks at send boundary.

## Align Raw Read Request With Line-Specific Provider Method (2026-03-29)
- Updated `FeedingFrenzyGetMessagesRawRequest.Method` to `GetMessagesByPhoneRawByLine`.
- Added required `LinePhone` to the authoritative read request contract so provider-side line filtering is explicit and typed.
