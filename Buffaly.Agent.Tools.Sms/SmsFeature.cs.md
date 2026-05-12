# SmsFeature.cs Change History

## Initial Creation (2026-04-09)
- Added `SmsFeature` as the canonical settings owner for SMS facade configuration in `Buffaly.Agent.Tools.Sms`.
- Added canonical keys:
- `SmsFeature.LinePhone`
- `SmsFeature.MonitoredLines`
- `SmsFeature.ProviderBaseUrl`
- `SmsFeature.SendMessagePath`
- `SmsFeature.GetMessagesPath`
- Implemented one-cache lazy load shape with `Feature`, `CreateFromSettings()`, `SetRuntimeFeature(...)`, and `ClearRuntimeFeature()`.
- Centralized line/monitored-list normalization, provider URL parsing, and canonical send/get path normalization in this class.
- Design Decision: keep SMS line/provider configuration parsing and validation in one shared feature owner so facade call paths stop reading/normalizing settings directly.

## Correct Canonical SMS Path Keys And Remove API Key From AppSettings Ownership (2026-04-09)
- Removed `SmsFeature.ApiKey` and `ResolvedApiKey` so `SmsFeature` owns only AppSettings-backed SMS line/provider endpoint settings.
- Renamed canonical endpoint keys from `SmsFeature.SendPath`/`SmsFeature.ReadPath` to `SmsFeature.SendMessagePath`/`SmsFeature.GetMessagesPath`.
- Renamed resolved endpoint properties to `ResolvedSendMessagePath` and `ResolvedGetMessagesPath` to match the canonical key contract.
- Design Decision: keep provider credentials outside AppSettings-owned feature configuration and preserve path naming symmetry with facade operations.

## Restore Canonical SMS API Key Ownership For Settings Migration Completeness (2026-04-09)
- Added canonical `SmsFeature.ApiKey` and typed `ResolvedApiKey` loading from settings.
- Kept existing line/monitored/provider/send/read feature ownership unchanged.
- Design Decision: align with migration mapping that assigns both `Buffaly.Sms.*` and `FeedingFrenzy.ApiKey` ownership to `SmsFeature` so facade/runtime/test code can use one canonical feature contract.

## Switch SmsFeature Keys To Object-Node Paths (2026-04-09)
- Updated SMS feature key constants from dotted form (`SmsFeature.*`) to section-path form (`SmsFeature:*`).
- Design Decision: align SMS feature contracts with nested `AppSettings:SmsFeature` object-node configuration used by other migrated feature owners.

## Reuse Shared Text/Fallback Normalizers In SmsFeature Helpers (2026-04-09)
- Updated SMS feature helper methods to use shared `NormalizationUtil` text/fallback normalization (`NormalizeOptionalText`, `NormalizeRequiredText`, `NormalizeRequiredString`).
- Kept SMS-specific phone-digit and URI/path semantics unchanged while removing duplicate trim/blank handling logic.
- Design Decision: centralize generic string normalization in `Buffaly.Agent.Common` and keep only SMS-domain behavior local.

## Canonicalize SMS Key Contract Delimiter (2026-04-10)
- Updated SMS feature key constants from `SmsFeature:*` to canonical naked `SmsFeature.*` names.
- Design Decision: align SMS key naming with the canonical feature contract format already used by other migrated features.

## Align Feature Object Shape (2026-04-10)
- Removed setting-key constants and bound `SmsFeature` from its object node with property names matching the JSON fields.
- Design Decision: keep SMS phone/API/path cleanup in the feature and remove caller dependency on feature key constants.

## Remove Dead Non-Nullable Fallbacks From SmsFeature (2026-04-13)
- Removed the redundant runtime null guard from `SetRuntimeFeature(...)` and stopped re-coalescing the non-nullable `ApiKey` property during feature binding.
- Kept monitored-line and URI/path normalization behavior unchanged.
- Design Decision: align SMS feature binding with the repo-wide rule that non-nullable typed feature members should be trusted directly.

## Load SmsFeature From Database Feature Store (2026-04-21)
- Replaced `AppSettings["SmsFeature"]` hydration with `DatabaseFeatureStore.LoadRequiredFeature<SmsFeature>("Sms Feature")`.
- Kept phone, monitored-line, base-url, API key, and path normalization after database hydration.
- Design Decision: SMS integration settings now use the central Features table as the single runtime source.
