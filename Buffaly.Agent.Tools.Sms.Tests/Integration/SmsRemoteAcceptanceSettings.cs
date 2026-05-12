using BasicUtilities;
using Buffaly.Agent.Tools.Sms;
using Microsoft.Extensions.Configuration;

namespace Buffaly.Agent.Tools.Sms.Tests.Integration;

public sealed class SmsRemoteAcceptanceSettings
{
	private const string SmsProviderBaseUrlSettingKey = "SmsFeature:ProviderBaseUrl";
	private const string SmsLinePhoneSettingKey = "SmsFeature:LinePhone";
	private const string SmsMonitoredLinesSettingKey = "SmsFeature:MonitoredLines";
	private const string SmsApiKeySettingKey = "SmsFeature:ApiKey";

	public string BaseUrl { get; init; } = "https://ff.medek.ai";
	public string PrimaryLinePhone { get; init; } = "16895880847";
	public string SecondaryLinePhone { get; init; } = "16896001779";
	public string MonitoredLines { get; init; } = "16895880847,16896001779,18063018828";
	public string ApiKeySettingName { get; init; } = SmsApiKeySettingKey;
	public string EnableManualRemoteTestsSetting { get; init; } = "Buffaly.Sms.Tests.EnableManualRemote";
	public string ControlledModeSetting { get; init; } = "Buffaly.Sms.Tests.UseControlledPair";
	public int ReplyWaitSeconds { get; init; } = 180;
	public int PollIntervalSeconds { get; init; } = 10;
	public string ApiKey { get; init; } = string.Empty;
	public bool EnableManualRemoteTests { get; init; }
	public bool UseControlledPair { get; init; }

	public static SmsRemoteAcceptanceSettings Load()
	{
		Dictionary<string, string?> defaults = new(StringComparer.OrdinalIgnoreCase)
		{
			["AppSettings:" + SmsProviderBaseUrlSettingKey] = "https://ff.medek.ai",
			["AppSettings:" + SmsLinePhoneSettingKey] = "16895880847",
			["AppSettings:Buffaly.Sms.SecondaryLinePhone"] = "16896001779",
			["AppSettings:" + SmsMonitoredLinesSettingKey] = "16895880847,16896001779,18063018828",
			["AppSettings:" + SmsApiKeySettingKey] = string.Empty,
			["AppSettings:Buffaly.Sms.Tests.EnableManualRemote"] = "false",
			["AppSettings:Buffaly.Sms.Tests.UseControlledPair"] = "true",
			["AppSettings:Buffaly.Sms.Tests.ReplyWaitSeconds"] = "180",
			["AppSettings:Buffaly.Sms.Tests.PollIntervalSeconds"] = "10"
		};

		ConfigurationBuilder builder = new();
		builder.SetBasePath(AppContext.BaseDirectory);
		builder.AddInMemoryCollection(defaults);
		builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
		builder.AddEnvironmentVariables();
		IConfigurationRoot root = builder.Build();
		IConfigurationSection appSettings = root.GetSection("AppSettings");

		string apiKey = appSettings[SmsApiKeySettingKey]
			?? root["SmsFeature__ApiKey"]
			?? root["SMSFEATURE_API_KEY"]
			?? string.Empty;

		return new SmsRemoteAcceptanceSettings
		{
			BaseUrl = (appSettings[SmsProviderBaseUrlSettingKey] ?? "https://ff.medek.ai").Trim(),
			PrimaryLinePhone = (appSettings[SmsLinePhoneSettingKey] ?? "16895880847").Trim(),
			SecondaryLinePhone = (appSettings["Buffaly.Sms.SecondaryLinePhone"] ?? "16896001779").Trim(),
			MonitoredLines = (appSettings[SmsMonitoredLinesSettingKey] ?? "16895880847,16896001779,18063018828").Trim(),
			ApiKey = apiKey.Trim(),
			EnableManualRemoteTests = ParseBoolean(appSettings["Buffaly.Sms.Tests.EnableManualRemote"] ?? "false"),
			UseControlledPair = ParseBoolean(appSettings["Buffaly.Sms.Tests.UseControlledPair"] ?? "true"),
			ReplyWaitSeconds = ParsePositiveInt(appSettings["Buffaly.Sms.Tests.ReplyWaitSeconds"], 180),
			PollIntervalSeconds = ParsePositiveInt(appSettings["Buffaly.Sms.Tests.PollIntervalSeconds"], 10)
		};
	}

	public JsonObject BuildAppSettingsJsonObject()
	{
		return new JsonObject
		{
			[SmsProviderBaseUrlSettingKey] = BaseUrl,
			[SmsLinePhoneSettingKey] = PrimaryLinePhone,
			["Buffaly.Sms.SecondaryLinePhone"] = SecondaryLinePhone,
			[SmsMonitoredLinesSettingKey] = MonitoredLines,
			[ApiKeySettingName] = ApiKey,
			[EnableManualRemoteTestsSetting] = EnableManualRemoteTests,
			[ControlledModeSetting] = UseControlledPair,
			["Buffaly.Sms.Tests.ReplyWaitSeconds"] = ReplyWaitSeconds,
			["Buffaly.Sms.Tests.PollIntervalSeconds"] = PollIntervalSeconds
		};
	}

	private static int ParsePositiveInt(string? raw, int fallback)
	{
		return int.TryParse(raw, out int value) && value > 0 ? value : fallback;
	}

	private static bool ParseBoolean(string raw)
	{
		return bool.TryParse(raw, out bool value) && value;
	}
}
