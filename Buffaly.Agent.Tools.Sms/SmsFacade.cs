using System.Text.Json;
using BasicUtilities;

namespace Buffaly.Agent.Tools.Sms;

public static class SmsFacade
{
	private const string CursorStorePath = @"C:\logs\Buffaly\sms-facade-cursors.json";
	private static readonly object CursorStoreLock = new();
	private static readonly Lazy<IFeedingFrenzySmsClient> FeedingFrenzySmsClient = new(CreateSmsClient);

	// Sends an SMS using the configured default Buffaly line phone.
	public static string SendMessage(string toPhone, string messageBody)
	{
		return SendMessageOnLine(toPhone, messageBody, null);
	}

	// Sends an SMS on a specific monitored line while preserving the existing facade response shape.
	public static string SendMessageOnLine(string toPhone, string messageBody, string? linePhone)
	{
		try
		{
			string normalizedToPhone = SmsPhoneUtil.NormalizeSendPhone(toPhone);
			if (string.IsNullOrWhiteSpace(normalizedToPhone))
				throw new InvalidOperationException("toPhone is required.");

			if (string.IsNullOrWhiteSpace(messageBody))
				throw new InvalidOperationException("messageBody is required.");

			string resolvedLinePhone = ResolveLinePhoneOrThrow(linePhone);
			FeedingFrenzySendMessageResponse response = GetSmsClient().SendMessageAsync(
				new FeedingFrenzySendMessageRequest(normalizedToPhone, messageBody, resolvedLinePhone)).GetAwaiter().GetResult();
			JsonObject payload = SmsPayloadMapper.BuildSendPayload(response, resolvedLinePhone);
			return JsonUtil.ToStringExt(payload).ToString();
		}
		catch (Exception ex)
		{
			return BuildErrorResult(ex);
		}
	}

	// Reads SMS history for a phone number from the configured default line.
	public static string GetMessages(string phoneNumber, int? sinceMessageId = null, int maxRows = 100)
	{
		return GetMessagesOnLine(phoneNumber, null, sinceMessageId, maxRows);
	}

	// Reads SMS history for a phone number from a specific line while preserving facade response contracts.
	public static string GetMessagesOnLine(string phoneNumber, string? linePhone, int? sinceMessageId = null, int maxRows = 100)
	{
		try
		{
			string normalizedPhone = SmsPhoneUtil.NormalizePhoneDigits(phoneNumber);
			if (string.IsNullOrWhiteSpace(normalizedPhone))
				throw new InvalidOperationException("phoneNumber is required.");

			string resolvedLinePhone = ResolveLinePhoneOrThrow(linePhone);
			JsonObject payload = BuildMessagesPayload(normalizedPhone, resolvedLinePhone, sinceMessageId, maxRows);
			return JsonUtil.ToStringExt(payload).ToString();
		}
		catch (Exception ex)
		{
			return BuildErrorResult(ex);
		}
	}

	// Reads only new SMS messages since the stored cursor for the configured default line.
	public static string GetNewMessages(string phoneNumber, int maxRows = 100)
	{
		return GetNewMessagesOnLine(phoneNumber, null, maxRows);
	}

	// Reads only new SMS messages since the stored cursor for a specific monitored line.
	public static string GetNewMessagesOnLine(string phoneNumber, string? linePhone, int maxRows = 100)
	{
		try
		{
			string normalizedPhone = SmsPhoneUtil.NormalizePhoneDigits(phoneNumber);
			if (string.IsNullOrWhiteSpace(normalizedPhone))
				throw new InvalidOperationException("phoneNumber is required.");

			string resolvedLinePhone = ResolveLinePhoneOrThrow(linePhone);
			string cursorKey = SmsCursorUtil.BuildCursorKey(normalizedPhone, resolvedLinePhone);
			int lastCursor = ReadCursor(cursorKey);
			JsonObject payload = BuildMessagesPayload(normalizedPhone, resolvedLinePhone, lastCursor, maxRows);
			int nextCursor = payload.GetIntOrDefault("CursorNext", lastCursor);
			if (nextCursor > lastCursor)
				WriteCursor(cursorKey, nextCursor);
			payload["CursorPrevious"] = lastCursor;
			return JsonUtil.ToStringExt(payload).ToString();
		}
		catch (Exception ex)
		{
			return BuildErrorResult(ex);
		}
	}

	// Resets the new-message cursor for the configured default line.
	public static string ResetNewMessageCursor(string phoneNumber)
	{
		return ResetNewMessageCursorOnLine(phoneNumber, null);
	}

	// Resets the new-message cursor for a specific monitored line.
	public static string ResetNewMessageCursorOnLine(string phoneNumber, string? linePhone)
	{
		try
		{
			string normalizedPhone = SmsPhoneUtil.NormalizePhoneDigits(phoneNumber);
			if (string.IsNullOrWhiteSpace(normalizedPhone))
				throw new InvalidOperationException("phoneNumber is required.");

			string resolvedLinePhone = ResolveLinePhoneOrThrow(linePhone);
			string cursorKey = SmsCursorUtil.BuildCursorKey(normalizedPhone, resolvedLinePhone);
			DeleteCursor(cursorKey);
			JsonObject payload = new()
			{
				["Success"] = true,
				["PhoneNumber"] = normalizedPhone,
				["LinePhone"] = resolvedLinePhone,
				["CursorKey"] = cursorKey
			};
			return JsonUtil.ToStringExt(payload).ToString();
		}
		catch (Exception ex)
		{
			return BuildErrorResult(ex);
		}
	}

	// Returns the active SMS configuration for default and monitored lines.
	public static string GetSmsConfiguration()
	{
		try
		{
			string defaultLine = ResolveLinePhoneOrThrow(null);
			List<string> monitored = GetConfiguredLinePhones().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			JsonArray lines = new();
			foreach (string line in monitored)
				lines.Add(line);

			JsonObject payload = new()
			{
				["Success"] = true,
				["DefaultLinePhone"] = defaultLine,
				["MonitoredLinePhones"] = lines,
				["LineSettingKey"] = "SmsFeature.LinePhone",
				["MonitoredLinesSettingKey"] = "SmsFeature.MonitoredLines"
			};
			return JsonUtil.ToStringExt(payload).ToString();
		}
		catch (Exception ex)
		{
			return BuildErrorResult(ex);
		}
	}

	// Calls the HTTP client and shapes read responses to the existing facade payload contract.
	private static JsonObject BuildMessagesPayload(string normalizedPhone, string linePhone, int? sinceMessageId, int maxRows)
	{
		int boundedMaxRows = Math.Clamp(maxRows, 1, 500);
		FeedingFrenzyGetMessagesResponse response = GetSmsClient().GetMessagesAsync(
			new FeedingFrenzyGetMessagesRequest(normalizedPhone, linePhone, sinceMessageId, boundedMaxRows)).GetAwaiter().GetResult();
		return SmsPayloadMapper.BuildMessagesPayload(normalizedPhone, linePhone, sinceMessageId, boundedMaxRows, response.Messages);
	}

	// Resolves and validates the requested line phone against configured monitored values.
	private static string ResolveLinePhoneOrThrow(string? linePhoneOverride)
	{
		SmsFeature smsFeature = SmsFeature.Feature;
		string rawLinePhone = string.IsNullOrWhiteSpace(linePhoneOverride)
			? smsFeature.LinePhone
			: linePhoneOverride.Trim();
		string normalized = SmsPhoneUtil.NormalizePhoneDigits(rawLinePhone);
		if (string.IsNullOrWhiteSpace(normalized))
			throw new InvalidOperationException("Missing required app setting: SmsFeature.LinePhone.");

		List<string> configured = GetConfiguredLinePhones();
		if (!configured.Any(x => StringUtil.EqualNoCase(x, normalized)))
			throw new InvalidOperationException("Requested line is not in configured monitored set. line=" + normalized + ", setting=SmsFeature.MonitoredLines.");
		return normalized;
	}

	// Builds the allowed monitored line-phone list from configured settings.
	private static List<string> GetConfiguredLinePhones()
	{
		return SmsFeature.Feature.GetMonitoredLinePhones().ToList();
	}

	// Creates the HTTP client from SMS feature app settings including the canonical SMS API key.
	private static IFeedingFrenzySmsClient CreateSmsClient()
	{
		SmsFeature smsFeature = SmsFeature.Feature;
		string apiKey = smsFeature.ApiKey;
		if (string.IsNullOrWhiteSpace(apiKey))
			throw new InvalidOperationException("Missing required app setting: SmsFeature.ApiKey.");
		FeedingFrenzySmsClientSettings settings = new(smsFeature.GetProviderBaseUri(), apiKey, smsFeature.SendMessagePath, smsFeature.GetMessagesPath);
		HttpClient httpClient = new()
		{
			BaseAddress = smsFeature.GetProviderBaseUri(),
			Timeout = TimeSpan.FromSeconds(30)
		};
		return new FeedingFrenzySmsHttpClient(httpClient, settings);
	}

	// Returns the lazily initialized FeedingFrenzy SMS HTTP client.
	private static IFeedingFrenzySmsClient GetSmsClient() => FeedingFrenzySmsClient.Value;

	// Reads the persisted cursor for a phone/line key.
	private static int ReadCursor(string cursorKey)
	{
		lock (CursorStoreLock)
		{
			Dictionary<string, int> map = ReadCursorMap();
			return map.TryGetValue(cursorKey, out int cursor) ? cursor : 0;
		}
	}

	// Writes the persisted cursor for a phone/line key.
	private static void WriteCursor(string cursorKey, int cursorValue)
	{
		lock (CursorStoreLock)
		{
			Dictionary<string, int> map = ReadCursorMap();
			map[cursorKey] = cursorValue;
			WriteCursorMap(map);
		}
	}

	// Deletes the persisted cursor for a phone/line key.
	private static void DeleteCursor(string cursorKey)
	{
		lock (CursorStoreLock)
		{
			Dictionary<string, int> map = ReadCursorMap();
			if (!map.Remove(cursorKey))
				return;

			WriteCursorMap(map);
		}
	}

	// Reads the on-disk cursor map.
	private static Dictionary<string, int> ReadCursorMap()
	{
		if (!File.Exists(CursorStorePath))
			return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		string payload = File.ReadAllText(CursorStorePath);
		if (string.IsNullOrWhiteSpace(payload))
			return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		Dictionary<string, int>? map = JsonSerializer.Deserialize<Dictionary<string, int>>(payload);
		return map ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	}

	// Writes the on-disk cursor map.
	private static void WriteCursorMap(Dictionary<string, int> map)
	{
		string? directory = Path.GetDirectoryName(CursorStorePath);
		if (!string.IsNullOrWhiteSpace(directory))
			Directory.CreateDirectory(directory);

		string payload = JsonSerializer.Serialize(map);
		File.WriteAllText(CursorStorePath, payload);
	}

	// Builds the standardized facade error payload.
	private static string BuildErrorResult(Exception ex)
	{
		JsonObject payload = new()
		{
			["Success"] = false,
			["Error"] = ex.Message,
			["ErrorType"] = ex.GetType().FullName ?? ex.GetType().Name
		};
		return JsonUtil.ToStringExt(payload).ToString();
	}
}
