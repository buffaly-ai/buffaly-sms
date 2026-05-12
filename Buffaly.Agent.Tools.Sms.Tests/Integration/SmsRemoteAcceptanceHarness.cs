using System.Text.Json;
using BasicUtilities;
using Buffaly.Agent.Tools.Secrets;
using Microsoft.Extensions.Configuration;

namespace Buffaly.Agent.Tools.Sms.Tests.Integration;

public sealed class SmsRemoteAcceptanceHarness
{
	public SmsRemoteAcceptanceHarness(SmsRemoteAcceptanceSettings settings)
	{
		AcceptanceSettings = settings ?? throw new ArgumentNullException(nameof(settings));
		InitializeSettings();
	}

	public SmsRemoteAcceptanceSettings AcceptanceSettings { get; }

	public JsonElement SendExplicitLine(string toPhone, string linePhone, string messageBody)
	{
		string payload = SmsFacade.SendMessageOnLine(toPhone, messageBody, linePhone);
		return ParsePayload(payload);
	}

	public JsonElement ReadAllForConversation(string phoneNumber, string linePhone, int? sinceMessageId, int maxRows)
	{
		string payload = SmsFacade.GetMessagesOnLine(phoneNumber, linePhone, sinceMessageId, maxRows);
		return ParsePayload(payload);
	}

	public JsonElement ReadNewForConversation(string phoneNumber, string linePhone, int maxRows)
	{
		string payload = SmsFacade.GetNewMessagesOnLine(phoneNumber, linePhone, maxRows);
		return ParsePayload(payload);
	}

	public JsonElement ResetCursor(string phoneNumber, string linePhone)
	{
		string payload = SmsFacade.ResetNewMessageCursorOnLine(phoneNumber, linePhone);
		return ParsePayload(payload);
	}

	public JsonElement WaitForNewMessage(string phoneNumber, string linePhone, int timeoutSeconds, int pollIntervalSeconds)
	{
		DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
		JsonElement latest = default;
		while (DateTime.UtcNow <= deadline)
		{
			latest = ReadNewForConversation(phoneNumber, linePhone, 100);
			if (GetMessages(latest).Count > 0)
				return latest;

			Thread.Sleep(TimeSpan.FromSeconds(Math.Max(1, pollIntervalSeconds)));
		}

		return latest;
	}

	// Drain recent same-conversation activity until at least one idle read is observed.
	public JsonElement DrainNewUntilIdle(string phoneNumber, string linePhone, int maxRows = 100, int timeoutMilliseconds = 2500, int pollIntervalMilliseconds = 150, int requiredConsecutiveEmptyBatches = 1)
	{
		DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(250, timeoutMilliseconds));
		int consecutiveEmptyBatches = 0;
		JsonElement latest = default;
		while (DateTime.UtcNow <= deadline)
		{
			latest = ReadNewForConversation(phoneNumber, linePhone, maxRows);
			if (GetMessages(latest).Count > 0)
			{
				consecutiveEmptyBatches = 0;
				continue;
			}

			consecutiveEmptyBatches++;
			if (consecutiveEmptyBatches >= Math.Max(1, requiredConsecutiveEmptyBatches))
				return latest;

			Thread.Sleep(TimeSpan.FromMilliseconds(Math.Max(25, pollIntervalMilliseconds)));
		}

		return latest;
	}

	public static bool IsSuccess(JsonElement payload)
	{
		return payload.TryGetProperty("Success", out JsonElement success) && success.ValueKind == JsonValueKind.True;
	}

	public static int? GetMessageId(JsonElement payload)
	{
		return payload.TryGetProperty("MessageID", out JsonElement node) && node.ValueKind == JsonValueKind.Number
			? node.GetInt32()
			: null;
	}

	public static int? GetOutboundMessageId(JsonElement payload)
	{
		return GetMessageId(payload);
	}

	public static string GetOutboundActualFromPhone(JsonElement payload)
	{
		if (payload.TryGetProperty("FromPhone", out JsonElement fromPhone))
			return fromPhone.GetString() ?? string.Empty;

		if (payload.TryGetProperty("SentByPhone", out JsonElement sentByPhone))
			return sentByPhone.GetString() ?? string.Empty;

		return string.Empty;
	}

	public static string GetOutboundRequestedLinePhone(JsonElement payload)
	{
		return payload.TryGetProperty("LinePhone", out JsonElement linePhone)
			? linePhone.GetString() ?? string.Empty
			: string.Empty;
	}

	public static int GetCursorNext(JsonElement payload)
	{
		return payload.TryGetProperty("CursorNext", out JsonElement node) && node.ValueKind == JsonValueKind.Number
			? node.GetInt32()
			: 0;
	}

	public static List<JsonElement> GetMessages(JsonElement payload)
	{
		if (!payload.TryGetProperty("Messages", out JsonElement messages) || messages.ValueKind != JsonValueKind.Array)
			return new List<JsonElement>();

		List<JsonElement> list = new();
		foreach (JsonElement message in messages.EnumerateArray())
			list.Add(message.Clone());
		return list;
	}

	private void InitializeSettings()
	{
		WriteAppSettingsFile();
		ConfigureSessionsConnectionString();
		UserSecrets.ConfigureDatabaseBackedSecrets();
		if (!string.IsNullOrWhiteSpace(AcceptanceSettings.ApiKey))
			UserSecrets.SetSecret(AcceptanceSettings.ApiKeySettingName, AcceptanceSettings.ApiKey);
		Settings.SetAppSettings(AcceptanceSettings.BuildAppSettingsJsonObject());
	}

	private string WriteAppSettingsFile()
	{
		JsonObject root = new()
		{
			["AppSettings"] = AcceptanceSettings.BuildAppSettingsJsonObject()
		};
		string appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
		File.WriteAllText(appSettingsPath, JsonUtil.ToStringExt(root).ToString());
		return appSettingsPath;
	}

	private static void ConfigureSessionsConnectionString()
	{
		ConfigurationBuilder builder = new();
		builder.SetBasePath(AppContext.BaseDirectory);
		builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
		builder.AddEnvironmentVariables();
		IConfigurationRoot configuration = builder.Build();
		string? connectionString = configuration.GetConnectionString("buffaly_sessions.readwrite");
		if (string.IsNullOrWhiteSpace(connectionString))
			throw new InvalidOperationException("Expected buffaly_sessions.readwrite connection string for SMS acceptance DB-backed user secret resolution.");

		Buffaly.Sessions.DB.DataAccess.SetConnectionString(connectionString);
	}

	private static JsonElement ParsePayload(string payload)
	{
		using JsonDocument document = JsonDocument.Parse(payload);
		return document.RootElement.Clone();
	}
}
