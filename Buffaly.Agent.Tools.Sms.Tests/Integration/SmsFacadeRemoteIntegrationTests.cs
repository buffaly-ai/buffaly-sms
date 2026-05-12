using System.Text.Json;
using BasicUtilities;
using Buffaly.Agent.Tools.Sms;
using Microsoft.Extensions.Configuration;

namespace Buffaly.Agent.Tools.Sms.Tests.Integration;

[TestClass]
public sealed class SmsFacadeRemoteIntegrationTests
{
	private const string RunRemoteSmsTestsEnvVar = "BUFFALY_SMS_RUN_REMOTE_INTEGRATION";

	// Verifies integration harness defaults and settings wiring without any network calls.
	[TestMethod]
	[TestCategory("Integration")]
	public void SmsIntegrationHarness_UsesExpectedDefaults_AndAppliesSettings()
	{
		SmsRemoteIntegrationHarness harness = SmsRemoteIntegrationHarness.CreateDefault(apiKeyOverride: "unit-test-api-key");

		Assert.AreEqual("https://ff.medek.ai", harness.ProviderBaseUrl);
		Assert.AreEqual("16895880847", harness.PrimaryLinePhone);
		Assert.AreEqual("16895880847,16896001779,18063018828", harness.MonitoredLinesCsv);
		Assert.AreEqual("4804140506", harness.PrimaryReceivingPhone);
		Assert.AreEqual("unit-test-api-key", harness.ApiKey);
	}

	// Manual acceptance workflow: requires two outbound sends and two inbound human/device replies.
	[TestMethod]
	[TestCategory("Integration")]
	public async Task SmsFacade_RemoteAcceptance_TwoOutboundTwoInbound_ThenNoNewMessages()
	{
		if (!string.Equals(Environment.GetEnvironmentVariable(RunRemoteSmsTestsEnvVar), "1", StringComparison.Ordinal))
			Assert.Inconclusive("Set BUFFALY_SMS_RUN_REMOTE_INTEGRATION=1 for explicit remote execution.");

		SmsRemoteIntegrationHarness harness = SmsRemoteIntegrationHarness.CreateDefault();
		if (string.IsNullOrWhiteSpace(harness.ApiKey))
			Assert.Inconclusive("SmsFeature:ApiKey is required for remote integration acceptance tests.");

		// Acceptance requirement: exactly 2 outbound + 2 inbound successful messages in this workflow.
		harness.ResetCursorOnPrimaryLine();

		JsonElement sendA = harness.SendOnPrimaryLine("Buffaly acceptance outbound A " + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
		Assert.IsTrue(sendA.GetProperty("Success").GetBoolean(), "Outbound A send must succeed.");
		Assert.AreEqual("16895880847", sendA.GetProperty("LinePhone").GetString(), "Requested explicit test line must be 16895880847.");
		int? messageIdA = harness.GetOutboundMessageId(sendA);
		Assert.IsTrue(messageIdA.HasValue && messageIdA.Value > 0, "Outbound A must include MessageID.");
		string actualFromA = harness.GetOutboundActualFromPhone(sendA);
		Assert.IsFalse(string.IsNullOrWhiteSpace(actualFromA), "Outbound A must include a non-empty actual provider send-from value.");

		bool inboundA = await harness.WaitForNextInboundAsync(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(10));
		Assert.IsTrue(inboundA, "Inbound A reply must be observed.");

		JsonElement sendB = harness.SendOnPrimaryLine("Buffaly acceptance outbound B " + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
		Assert.IsTrue(sendB.GetProperty("Success").GetBoolean(), "Outbound B send must succeed.");
		Assert.AreEqual("16895880847", sendB.GetProperty("LinePhone").GetString(), "Requested explicit test line must be 16895880847.");
		int? messageIdB = harness.GetOutboundMessageId(sendB);
		Assert.IsTrue(messageIdB.HasValue && messageIdB.Value > 0, "Outbound B must include MessageID.");
		string actualFromB = harness.GetOutboundActualFromPhone(sendB);
		Assert.IsFalse(string.IsNullOrWhiteSpace(actualFromB), "Outbound B must include a non-empty actual provider send-from value.");

		bool inboundB = await harness.WaitForNextInboundAsync(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(10));
		Assert.IsTrue(inboundB, "Inbound B reply must be observed.");

		JsonElement finalBatch = harness.GetNewOnPrimaryLine();
		Assert.AreEqual(0, harness.GetMessagesCount(finalBatch), "Final GetNew must return zero rows after cursor advancement.");
	}
}

internal sealed class SmsRemoteIntegrationHarness
{
	private const string AppSettingsSectionName = "AppSettings";
	private const string DefaultProviderBaseUrl = "https://ff.medek.ai";
	private const string DefaultPrimaryLine = "16895880847";
	private const string DefaultMonitoredLines = "16895880847,16896001779,18063018828";
	private const string DefaultPrimaryReceivingPhone = "4804140506";
	private const string SmsProviderBaseUrlSettingKey = "SmsFeature:ProviderBaseUrl";
	private const string SmsLinePhoneSettingKey = "SmsFeature:LinePhone";
	private const string SmsMonitoredLinesSettingKey = "SmsFeature:MonitoredLines";
	private const string SmsApiKeySettingKey = "SmsFeature:ApiKey";

	private SmsRemoteIntegrationHarness(IConfigurationRoot configurationRoot)
	{
		ConfigurationRoot = configurationRoot;
		IConfigurationSection appSettings = configurationRoot.GetSection(AppSettingsSectionName);
		Settings.SetAppSettings(appSettings);

		ProviderBaseUrl = GetRequiredValue(appSettings, SmsProviderBaseUrlSettingKey);
		PrimaryLinePhone = GetRequiredValue(appSettings, SmsLinePhoneSettingKey);
		MonitoredLinesCsv = GetRequiredValue(appSettings, SmsMonitoredLinesSettingKey);
		PrimaryReceivingPhone = GetRequiredValue(appSettings, "Buffaly.Sms.PrimaryReceivingPhone");
		ApiKey = appSettings[SmsApiKeySettingKey] ?? string.Empty;
	}

	public IConfigurationRoot ConfigurationRoot { get; }
	public string ProviderBaseUrl { get; }
	public string PrimaryLinePhone { get; }
	public string MonitoredLinesCsv { get; }
	public string PrimaryReceivingPhone { get; }
	public string ApiKey { get; }

	// Creates a harness with defaults, optional local appsettings overrides, and environment variable overrides.
	public static SmsRemoteIntegrationHarness CreateDefault(string? apiKeyOverride = null)
	{
		Dictionary<string, string?> defaults = new(StringComparer.OrdinalIgnoreCase)
		{
			["AppSettings:" + SmsProviderBaseUrlSettingKey] = DefaultProviderBaseUrl,
			["AppSettings:" + SmsLinePhoneSettingKey] = DefaultPrimaryLine,
			["AppSettings:" + SmsMonitoredLinesSettingKey] = DefaultMonitoredLines,
			["AppSettings:Buffaly.Sms.PrimaryReceivingPhone"] = DefaultPrimaryReceivingPhone,
			["AppSettings:" + SmsApiKeySettingKey] = ResolveApiKey(apiKeyOverride)
		};

		ConfigurationBuilder builder = new();
		builder.SetBasePath(AppContext.BaseDirectory);
		builder.AddInMemoryCollection(defaults);
		builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
		builder.AddEnvironmentVariables();
		return new SmsRemoteIntegrationHarness(builder.Build());
	}

	// Sends a message through SmsFacade using the explicit primary line.
	public JsonElement SendOnPrimaryLine(string messageBody)
	{
		string payload = SmsFacade.SendMessageOnLine(PrimaryReceivingPhone, messageBody, PrimaryLinePhone);
		return ParsePayload(payload);
	}

	// Reads only new messages through SmsFacade on the explicit primary line.
	public JsonElement GetNewOnPrimaryLine(int maxRows = 50)
	{
		string payload = SmsFacade.GetNewMessagesOnLine(PrimaryReceivingPhone, PrimaryLinePhone, maxRows);
		return ParsePayload(payload);
	}

	// Resets the persisted SmsFacade cursor on the explicit primary line.
	public void ResetCursorOnPrimaryLine()
	{
		_ = SmsFacade.ResetNewMessageCursorOnLine(PrimaryReceivingPhone, PrimaryLinePhone);
	}

	// Polls SmsFacade GetNew until at least one new inbound message is observed or timeout elapses.
	public async Task<bool> WaitForNextInboundAsync(TimeSpan timeout, TimeSpan pollInterval)
	{
		DateTime deadline = DateTime.UtcNow + timeout;
		while (DateTime.UtcNow <= deadline)
		{
			JsonElement batch = GetNewOnPrimaryLine();
			if (GetNewestInboundRows(batch).Count >= 1)
				return true;

			await Task.Delay(pollInterval).ConfigureAwait(false);
		}

		return false;
	}

	// Returns the message row count from a SmsFacade read payload.
	public int GetMessagesCount(JsonElement payload)
	{
		return payload.TryGetProperty("Messages", out JsonElement messages) && messages.ValueKind == JsonValueKind.Array
			? messages.GetArrayLength()
			: 0;
	}

	// Extracts outbound MessageID from a send payload.
	public int? GetOutboundMessageId(JsonElement sendPayload)
	{
		return sendPayload.TryGetProperty("MessageID", out JsonElement id) && id.ValueKind == JsonValueKind.Number
			? id.GetInt32()
			: null;
	}

	// Extracts the actual provider send-from phone value from a send payload.
	public string GetOutboundActualFromPhone(JsonElement sendPayload)
	{
		if (sendPayload.TryGetProperty("FromPhone", out JsonElement fromPhone))
			return fromPhone.GetString() ?? string.Empty;

		if (sendPayload.TryGetProperty("SentByPhone", out JsonElement sentByPhone))
			return sentByPhone.GetString() ?? string.Empty;

		return string.Empty;
	}

	// Returns inbound rows from a SmsFacade GetNew/GetMessages payload.
	public List<JsonElement> GetNewestInboundRows(JsonElement payload)
	{
		if (!payload.TryGetProperty("Messages", out JsonElement messages) || messages.ValueKind != JsonValueKind.Array)
			return new List<JsonElement>();

		List<JsonElement> inbound = new();
		foreach (JsonElement message in messages.EnumerateArray())
		{
			string direction = message.TryGetProperty("Direction", out JsonElement directionNode)
				? directionNode.GetString() ?? string.Empty
				: string.Empty;
			if (string.Equals(direction, "inbound", StringComparison.OrdinalIgnoreCase))
				inbound.Add(message.Clone());
		}
		return inbound;
	}

	// Parses SmsFacade JSON string payload into a detached JsonElement root.
	private static JsonElement ParsePayload(string payload)
	{
		using JsonDocument document = JsonDocument.Parse(payload);
		return document.RootElement.Clone();
	}

	// Resolves API key from explicit override first, then environment variables.
	private static string ResolveApiKey(string? apiKeyOverride)
	{
		if (!string.IsNullOrWhiteSpace(apiKeyOverride))
			return apiKeyOverride;

		return Environment.GetEnvironmentVariable("SmsFeature__ApiKey")
			?? Environment.GetEnvironmentVariable("SmsFeature.ApiKey")
			?? Environment.GetEnvironmentVariable("SMSFEATURE_API_KEY")
			?? string.Empty;
	}

	// Reads required app settings values and fails fast when missing.
	private static string GetRequiredValue(IConfigurationSection appSettings, string key)
	{
		string? value = appSettings[key];
		if (string.IsNullOrWhiteSpace(value))
			throw new InvalidOperationException("Missing required integration harness setting: " + key + ".");

		return value.Trim();
	}
}
