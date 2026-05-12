# SmsFacade.cs Change History

## Initial Creation (2026-03-25)
- Created `SmsFacade` in `Buffaly.Agent.Tools.Sms` as a FeedingFrenzy-backed SMS facade for Buffaly runtime usage.
- Added facade methods:
- `SendMessage(toPhone, messageBody)`
- `GetMessages(phoneNumber, sinceMessageId, maxRows)`
- `GetNewMessages(phoneNumber, maxRows)`
- `ResetNewMessageCursor(phoneNumber)`
- Implemented opaque line-phone configuration via required app setting `Buffaly.Sms.LinePhone`.
- Implemented cursor persistence at `C:\logs\Buffaly\sms-facade-cursors.json` so ProtoScript callers do not manage message cursors.
- Design Decision: keep all Twilio interaction delegated through FeedingFrenzy `Messages` APIs and expose one deterministic JSON response contract for ProtoScript pass-through skills.
## Align Facade With Available Shipped FeedingFrenzy DLL Surface (2026-03-25)
- Replaced planned `FeedingFrenzy.CallCenter.Messages.SendMessageRaw/GetMessagesByPhoneRawByLine` calls with available shipped APIs from local `lib`:
- send path now uses `FeedingFrenzy.TwilioAPI.TwilioHelper.SendSMSViaTwilio(...)` plus `MessagesRepository.InsertMessage(...)` persistence.
- read path now uses `MessagesRepository.GetByPhone(...)` with explicit line-phone and cursor filtering inside the facade.
- Reason: the local `FeedingFrenzy.CallCenter.dll` in this repo does not expose the newer raw-message helper methods.

## Switch To Admin.Business Messages Surface After DLL Refresh (2026-03-25)
- Updated facade send/read implementation to use `FeedingFrenzy.Admin.Business.Messages` methods directly:
- `SendMessageRaw(...)`
- `GetMessagesByPhoneRaw(...)`
- Updated row contracts to `FeedingFrenzy.Data.MessagesRow/MessagesDataTable` and retained line-phone filtering and cursor persistence in the facade.
- Design Decision: keep one canonical FeedingFrenzy-owned API surface for SMS operations and avoid direct Twilio API calls in Buffaly.

## Seed Missing Twilio Feature Fields From AppSettings For SMS Sends (2026-03-25)
- Added `EnsureTwilioFeatureConfigured(linePhone)` and invoked it from `SendMessage(...)` before `Messages.SendMessageRaw(...)`.
- New bootstrap logic fills missing `TwilioFeature` fields from `AppSettings` keys when feature storage is incomplete:
- `Twilio.AccountSID`
- `Twilio.AuthToken`
- `Twilio.MessagingServiceSid` (optional; enables `UseMessagingService`)
- Also seeds `FromNumber` from configured Buffaly line phone when missing.
- Added explicit fail-fast error when `AccountSID`/`AuthToken` are still missing after bootstrap so diagnostics are actionable.
- Design Decision: keep SMS sending delegated to FeedingFrenzy `Messages`/`TwilioHelper` while making Buffaly-side setup resilient to partially configured Twilio feature rows in local environments.

## Introduce Typed FeedingFrenzy JSONWS HTTP Client Layer (2026-03-29)
- Replaced direct static `FeedingFrenzy.Admin.Business` and `FeedingFrenzy.CallCenter.Data` calls in `SmsFacade` with a lazily initialized typed HTTP client (`IFeedingFrenzySmsClient`).
- Added configurable provider settings for JSONWS host/paths with default base URL `https://ff.medek.ai`.
- Kept all existing public `SmsFacade` method names/signatures and preserved output payload shape keys for ProtoScript compatibility.
- Extracted phone normalization, cursor key generation, and payload mapping into dedicated helper classes for unit-test coverage without network dependencies.
- Design Decision: move provider access behind one typed client boundary and keep cursor persistence behavior unchanged in this first migration batch.

## Align JSONWS Defaults, Auth, and Request Shaping With Proven Production Workflow (2026-03-29)
- Updated default endpoint paths to production-proven values:
- send: `/api/feedingfrenzy.admin.business/messages/send-message-raw`
- read: `/api/feedingfrenzy.admin.business/messages/get-messages-by-phone-raw`
- Added required API key configuration from app setting `FeedingFrenzy.ApiKey` during client creation.
- Updated send phone normalization usage to dashed destination formatting while preserving digits-only normalization for read and cursor flows.
- Design Decision: keep facade public signatures unchanged while enforcing explicit provider/auth configuration and production-aligned request contracts.

## Resolve SMS API Key From Canonical UserSecrets Helper (2026-03-29)
- Updated `CreateSmsClient()` to resolve `FeedingFrenzy.ApiKey` via `UserSecrets.GetSecret("FeedingFrenzy.ApiKey").Trim()` instead of app settings lookup.
- Added explicit fail-fast error for missing/blank user secret while keeping base URL, line phone, monitored lines, send path, and read path behavior unchanged.
- Design Decision: align SMS auth key resolution with the existing repo-wide user-secrets pattern and keep one explicit source for the credential.

## Rewire SMS Facade Settings To SmsFeature (2026-04-09)
- Replaced direct settings parsing in `SmsFacade` with `SmsFeature.Feature` properties for default line, monitored lines, provider base URL, API key, send path, and read path.
- Removed facade-local monitored-lines and provider/client setting normalization logic for the migrated settings.
- Updated configuration metadata payload keys to canonical feature keys (`SmsFeature.LinePhone`, `SmsFeature.MonitoredLines`).
- Design Decision: keep all migrated SMS AppSettings parsing/defaulting/validation in one `SmsFeature` owner and keep facade methods focused on operational behavior.

## Restore FeedingFrenzy API Key Secret Boundary And Canonical SMS Path Usage (2026-04-09)
- Updated CreateSmsClient() to resolve FeedingFrenzy.ApiKey only from UserSecrets.GetSecret(...) inside SmsFacade and fail fast when missing.
- Updated the client settings wiring to consume SmsFeature.ResolvedSendMessagePath and SmsFeature.ResolvedGetMessagesPath.
- Design Decision: keep credentials resolved at the facade boundary while SmsFeature remains the owner of AppSettings-backed line and endpoint path settings only.

## Switch SmsFacade API Key Source To Canonical SmsFeature (2026-04-09)
- Updated `CreateSmsClient()` to read API credentials from `SmsFeature.Feature.ResolvedApiKey`.
- Removed direct `UserSecrets.GetSecret("FeedingFrenzy.ApiKey")` dependency from facade client creation and updated fail-fast diagnostics to `SmsFeature.ApiKey`.
- Design Decision: make `SmsFeature` the authoritative settings source for SMS provider credentials per migration mapping and keep facade behavior unchanged otherwise.



## Remove SmsFeature Constant Usage (2026-04-10)
- Replaced removed `SmsFeature` setting-key constants in diagnostics/config payload labels with explicit feature-field names.
- Design Decision: keep facade reads on `SmsFeature.Feature` while allowing the feature class to avoid per-property key constants.

## Remove Stale Actions Namespace Import (2026-04-11)
- Removed the unused `Buffaly.Agent.Tools.Actions` import after tool assembly splits moved SMS facade dependencies to direct capability references.
- Design Decision: fail builds on real missing dependencies instead of carrying stale imports from the pre-split assembly layout.
