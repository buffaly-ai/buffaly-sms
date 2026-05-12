# SmsFacadeRemoteIntegrationTests.cs Change History

## Initial Creation (2026-03-29)
- Added `SmsRemoteIntegrationHarness` with `ConfigurationBuilder` + `Settings.SetAppSettings(config.GetSection("AppSettings"))` wiring for SMS remote integration scenarios.
- Added explicit default values:
- base URL: `https://ff.medek.ai`
- primary line: `16895880847`
- monitored lines: `16895880847,16896001779,18063018828`
- primary receiving phone: `4804140506`
- API key: `FeedingFrenzy.ApiKey` (with environment override fallback support).
- Added one non-network integration smoke test that validates default harness configuration wiring.
- Added a gated manual acceptance workflow test encoding two outbound sends, two inbound replies, and final zero-row `GetNew` cursor behavior through `SmsFacade` public methods only.
- Design Decision: keep real remote coverage compileable but opt-in/manual so default test runs remain deterministic and CI-safe.

## Migrate Remote Integration Harness To Canonical SmsFeature Keys (2026-04-09)
- Replaced `Buffaly.Sms.ProviderBaseUrl`, `Buffaly.Sms.LinePhone`, and `Buffaly.Sms.MonitoredLines` references with `SmsFeature` constants.
- Replaced remote API key setting key from `FeedingFrenzy.ApiKey` to `SmsFeature.ApiKey` and updated inconclusive diagnostics accordingly.
- Updated API-key environment fallback variables to canonical `SmsFeature` names.
- Design Decision: keep manual integration behavior the same while aligning harness configuration to canonical feature-owned setting keys.

## Fix SmsFeature Key Access After Constant Removal (2026-04-10)
- Replaced stale `SmsFeature.<Property>` static key references with explicit canonical AppSettings key strings used by the integration harness (`SmsFeature:ProviderBaseUrl`, `SmsFeature:LinePhone`, `SmsFeature:MonitoredLines`, `SmsFeature:ApiKey`).
- Design Decision: keep harness configuration object-node aligned without depending on removed per-property feature constants.
