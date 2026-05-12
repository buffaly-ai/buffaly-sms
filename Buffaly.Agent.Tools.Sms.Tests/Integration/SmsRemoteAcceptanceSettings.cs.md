# SmsRemoteAcceptanceSettings.cs Change History

## Initial Creation (2026-04-09)
- Added canonical-feature key migration updates for SMS remote acceptance settings bootstrap.
- Replaced `FeedingFrenzy.ApiKey` and `Buffaly.Sms.*` runtime setting reads/writes (where feature owners exist) with `SmsFeature.ApiKey`, `SmsFeature.ProviderBaseUrl`, `SmsFeature.LinePhone`, and `SmsFeature.MonitoredLines`.
- Updated default in-memory config key population to canonical `SmsFeature` key names under `AppSettings`.
- Design Decision: keep acceptance harness behavior unchanged while making canonical SMS feature keys the authoritative settings contract for the migrated fields.

## Fix Static SmsFeature Key Access Compilation Errors (2026-04-10)
- Replaced stale `SmsFeature.<Property>` static key references with explicit canonical key strings (`SmsFeature:ProviderBaseUrl`, `SmsFeature:LinePhone`, `SmsFeature:MonitoredLines`, `SmsFeature:ApiKey`) across defaults, reads, and persisted JSON payload composition.
- Design Decision: keep acceptance settings behavior unchanged while removing dependency on removed per-property feature constants.
