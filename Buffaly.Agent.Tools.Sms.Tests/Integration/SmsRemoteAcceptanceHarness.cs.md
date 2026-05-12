# SmsRemoteAcceptanceHarness.cs Change History

## Added AppSettings File Bootstrap (2026-03-30)
- Updated `InitializeSettings()` to write `appsettings.json` in `AppContext.BaseDirectory` before calling `Settings.SetAppSettings(...)`.
- Design Decision: Keep the fix harness-only by generating a minimal `{ "AppSettings": ... }` payload from `AcceptanceSettings.BuildAppSettingsJsonObject()` so `UserSecrets` file lookup succeeds without changing runtime SMS code.

## Mirrored Web UserSecrets Format For MSTest (2026-03-30)
- Updated the appsettings bootstrap helper to include a top-level `UserSecrets` object with `FeedingFrenzy.ApiKey` sourced from `AcceptanceSettings.ApiKey`.
- Design Decision: After writing `appsettings.json`, call `UserSecrets.ConfigureAppSettingsPath(appSettingsPath)` so test secret resolution uses the same explicit file path pattern as web startup.

## Applied Harness UserSecrets Path Wiring (2026-03-30)
- Added explicit `Buffaly.Agent.Tools.Integrations.UserSecrets.ConfigureAppSettingsPath(appSettingsPath)` wiring in the harness after writing `appsettings.json`.
- Design Decision: Keep configuration shaping inside the test harness helper to mirror web startup secret resolution without touching SMS runtime code.

## Remove Stale Actions Namespace Import (2026-04-11)
- Removed the unused `Buffaly.Agent.Tools.Actions` import from the SMS acceptance harness after the capability split.
- Design Decision: keep the harness bound only to the SMS facade contract it actually exercises.

## Restore Explicit UserSecrets Import (2026-04-11)
- Re-added the `Buffaly.Agent.Tools.Actions` import because the harness still calls `UserSecrets.ConfigureAppSettingsPath(...)` during test settings bootstrap.
- Design Decision: keep the harness on the authoritative `UserSecrets` contract rather than introducing a local wrapper for test-only configuration.

## Reference Secrets Assembly Directly For Harness Bootstrap (2026-04-11)
- Kept the explicit `Buffaly.Agent.Tools.Actions` import in the harness and restored the test-project dependency on the Secrets assembly that owns `UserSecrets`.
- Design Decision: the acceptance harness should compile directly against the authoritative secrets contract instead of relying on transitive references from the SMS runtime project.

## Seed Acceptance Secrets Into Database-Backed User Secrets Feature (2026-04-21)
- Updated the harness bootstrap to seed the generated `UserSecrets` section into `User Secrets Feature` and then call `ConfigureDatabaseBackedSecrets()` instead of binding runtime secret resolution to the temporary file path.
- Design Decision: acceptance tests should still shape legacy migration input files when needed, but the exercised runtime path must match the new database-backed secret authority.

## Convert SMS Acceptance Harness To Explicit Database-Backed Secret Setup (2026-04-23)
- Removed the retired startup-style DB seeding call from `InitializeSettings()`.
- Updated the harness to preserve existing generated appsettings content, initialize the sessions DB connection from the generated file, switch to `ConfigureDatabaseBackedSecrets()`, and write the API key with `UserSecrets.SetSecret(...)`.
- Design Decision: SMS acceptance tests may generate local configuration inputs, but the exercised secret resolution path must be the same explicit database-backed runtime flow as production code.
