# SmsRemoteAcceptanceTests.cs Change History

## Initial Creation (2026-04-09)
- Updated remote acceptance inconclusive diagnostics to reference canonical `SmsFeature.ApiKey` instead of legacy `FeedingFrenzy.ApiKey`.
- Design Decision: keep manual acceptance workflow and assertions unchanged while reflecting canonical SMS API-key ownership in operator-facing test guidance.

## Fix ApiKey Guidance After Key-Constant Removal (2026-04-10)
- Replaced `SmsFeature.ApiKey` static key references in inconclusive test guidance with canonical `SmsFeature:ApiKey` string labels.
- Design Decision: preserve manual test gating semantics while keeping diagnostics compile-safe after feature key constant removal.
