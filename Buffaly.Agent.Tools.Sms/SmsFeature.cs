using BasicUtilities;
using Buffaly.Agent.Common;

namespace Buffaly.Agent.Tools.Sms;

// Provide canonical SMS facade settings for line routing and provider endpoint paths.
public sealed class SmsFeature
{
	private const string DefaultProviderBaseUrl = "https://ff.medek.ai";
	private const string DefaultSendMessagePath = "/api/feedingfrenzy.admin.business/messages/send-message-raw";
	private const string DefaultGetMessagesPath = "/api/feedingfrenzy.admin.business/messages/get-messages-by-phone-raw";
	private static SmsFeature? _feature;

	// Return one cached SMS feature snapshot, loading from settings on first use.
	public static SmsFeature Feature => _feature ??= CreateFromSettings();

	// Override the cached SMS feature snapshot for runtime/test scenarios.
	public static void SetRuntimeFeature(SmsFeature runtimeFeature)
	{
		_feature = runtimeFeature;
	}

	// Clear the cached/runtime override so the next read rebinds from settings.
	public static void ClearRuntimeFeature()
	{
		_feature = null;
	}

	// Build one canonical SMS feature snapshot from the SmsFeature object node.
	public static SmsFeature CreateFromSettings()
	{
		SmsFeature smsFeature = DatabaseFeatureStore.LoadRequiredFeature<SmsFeature>("Sms Feature");

		smsFeature.LinePhone = NormalizeRequiredPhone(smsFeature.LinePhone, nameof(LinePhone));
		smsFeature.MonitoredLines = NormalizeMonitoredLines(smsFeature.MonitoredLines, smsFeature.LinePhone);
		smsFeature.ProviderBaseUrl = NormalizeAbsoluteUriOrThrow(smsFeature.ProviderBaseUrl, nameof(ProviderBaseUrl)).ToString().TrimEnd('/');
		smsFeature.ApiKey = smsFeature.ApiKey.Trim();
		smsFeature.SendMessagePath = NormalizeRelativePathOrDefault(smsFeature.SendMessagePath, DefaultSendMessagePath);
		smsFeature.GetMessagesPath = NormalizeRelativePathOrDefault(smsFeature.GetMessagesPath, DefaultGetMessagesPath);

		return smsFeature;
	}

	public string LinePhone { get; set; } = string.Empty;
	public string MonitoredLines { get; set; } = string.Empty;
	public string ProviderBaseUrl { get; set; } = DefaultProviderBaseUrl;
	public string ApiKey { get; set; } = string.Empty;
	public string SendMessagePath { get; set; } = DefaultSendMessagePath;
	public string GetMessagesPath { get; set; } = DefaultGetMessagesPath;

	// Return normalized monitored line numbers as a distinct list.
	public IReadOnlyList<string> GetMonitoredLinePhones()
	{
		return MonitoredLines.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	// Return the provider base URL as an absolute URI for HTTP clients.
	public Uri GetProviderBaseUri()
	{
		return new Uri(ProviderBaseUrl, UriKind.Absolute);
	}

	// Normalize one required phone value from settings.
	private static string NormalizeRequiredPhone(string? value, string settingKey)
	{
		string normalized = SmsPhoneUtil.NormalizePhoneDigits(NormalizationUtil.NormalizeOptionalText(value));
		if (string.IsNullOrWhiteSpace(normalized))
			throw new InvalidOperationException("Missing required app setting: SmsFeature." + settingKey + ".");
		return normalized;
	}

	// Normalize one monitored lines CSV setting to a distinct canonical CSV including the default line.
	private static string NormalizeMonitoredLines(string? monitoredLines, string defaultLinePhone)
	{
		HashSet<string> normalizedSet = new(StringComparer.OrdinalIgnoreCase)
		{
			defaultLinePhone
		};

		string raw = (monitoredLines ?? string.Empty).Trim();
		foreach (string token in raw.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string normalized = SmsPhoneUtil.NormalizePhoneDigits(token);
			if (!string.IsNullOrWhiteSpace(normalized))
				normalizedSet.Add(normalized);
		}

		return string.Join(',', normalizedSet);
	}

	// Normalize one required absolute URI setting.
	private static Uri NormalizeAbsoluteUriOrThrow(string? value, string settingKey)
	{
		string normalized = NormalizationUtil.NormalizeRequiredText(value, "SmsFeature." + settingKey);
		if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? parsed))
			throw new InvalidOperationException("Invalid SMS provider base URL setting: SmsFeature." + settingKey + ".");
		return parsed;
	}

	// Normalize one relative API path setting with deterministic default.
	private static string NormalizeRelativePathOrDefault(string? value, string defaultValue)
	{
		string normalized = NormalizationUtil.NormalizeRequiredString(value, defaultValue);
		if (!normalized.StartsWith("/", StringComparison.Ordinal))
			normalized = "/" + normalized;
		return normalized;
	}
}
