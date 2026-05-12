using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Buffaly.Agent.Tools.Sms;

public interface IFeedingFrenzySmsClient
{
	Task<FeedingFrenzySendMessageResponse> SendMessageAsync(FeedingFrenzySendMessageRequest request, CancellationToken cancellationToken = default);
	Task<FeedingFrenzyGetMessagesResponse> GetMessagesAsync(FeedingFrenzyGetMessagesRequest request, CancellationToken cancellationToken = default);
}

public sealed record FeedingFrenzySmsClientSettings(Uri ProviderBaseUri, string ApiKey, string SendMessagePath, string GetMessagesPath);

public sealed class FeedingFrenzySmsHttpClient : IFeedingFrenzySmsClient
{
	private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
	private static JsonSerializerOptions CreateJsonOptions()
	{
		JsonSerializerOptions options = new()
		{
			PropertyNamingPolicy = null
		};
		options.Converters.Add(new FeedingFrenzyDateTimeJsonConverter());
		return options;
	}
	private readonly HttpClient _httpClient;
	private readonly FeedingFrenzySmsClientSettings _settings;

	public FeedingFrenzySmsHttpClient(HttpClient httpClient, FeedingFrenzySmsClientSettings settings)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_settings = settings ?? throw new ArgumentNullException(nameof(settings));

		if (string.IsNullOrWhiteSpace(_settings.SendMessagePath))
			throw new InvalidOperationException("SMS send path is required.");

		if (string.IsNullOrWhiteSpace(_settings.GetMessagesPath))
			throw new InvalidOperationException("SMS read path is required.");

		if (string.IsNullOrWhiteSpace(_settings.ApiKey))
			throw new InvalidOperationException("FeedingFrenzy API key is required.");
	}

	// Sends an SMS through FeedingFrenzy JSONWS using the authoritative typed contract.
	public Task<FeedingFrenzySendMessageResponse> SendMessageAsync(FeedingFrenzySendMessageRequest request, CancellationToken cancellationToken = default)
	{
		FeedingFrenzySendMessageRawRequest rawRequest = SmsRequestContractMapper.BuildSendRawRequest(request);
		return SendMessageInternalAsync(rawRequest, cancellationToken);
	}

	// Reads SMS messages through FeedingFrenzy JSONWS using the authoritative typed contract.
	public Task<FeedingFrenzyGetMessagesResponse> GetMessagesAsync(FeedingFrenzyGetMessagesRequest request, CancellationToken cancellationToken = default)
	{
		FeedingFrenzyGetMessagesRawRequest rawRequest = SmsRequestContractMapper.BuildReadRawRequest(request);
		return GetMessagesInternalAsync(rawRequest, cancellationToken);
	}

	// Sends typed raw payloads and maps them back to facade-level contracts.
	private async Task<FeedingFrenzySendMessageResponse> SendMessageInternalAsync(FeedingFrenzySendMessageRawRequest request, CancellationToken cancellationToken)
	{
		FeedingFrenzySendMessageRawResponse rawResponse = await PostJsonAsync<FeedingFrenzySendMessageRawRequest, FeedingFrenzySendMessageRawResponse>(_settings.SendMessagePath, request, cancellationToken).ConfigureAwait(false);
		return SmsRequestContractMapper.MapSendRawResponse(rawResponse);
	}

	// Sends typed raw payloads and maps them back to facade-level contracts.
	private async Task<FeedingFrenzyGetMessagesResponse> GetMessagesInternalAsync(FeedingFrenzyGetMessagesRawRequest request, CancellationToken cancellationToken)
	{
		List<FeedingFrenzyMessage> rawResponse = await PostJsonAsync<FeedingFrenzyGetMessagesRawRequest, List<FeedingFrenzyMessage>>(_settings.GetMessagesPath, request, cancellationToken).ConfigureAwait(false);
		return SmsRequestContractMapper.MapReadRawResponse(rawResponse);
	}

	// Posts a typed request and fails fast when the remote endpoint does not return a valid typed response.
	private async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken)
	{
		using HttpRequestMessage httpRequest = new(HttpMethod.Post, path)
		{
			Content = JsonContent.Create(request, options: JsonOptions)
		};
		httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

		using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
		string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException("FeedingFrenzy SMS request failed. Path=" + path + ", StatusCode=" + (int)response.StatusCode + ", Body=" + responseBody);

		TResponse? typedResponse = JsonSerializer.Deserialize<TResponse>(responseBody, JsonOptions);
		if (typedResponse == null)
			throw new InvalidOperationException("FeedingFrenzy SMS response was empty or invalid JSON. Path=" + path + ".");

		return typedResponse;
	}

	private sealed class FeedingFrenzyDateTimeJsonConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
	{
		public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
				throw new JsonException("Expected string token for DateTime value.");

			string? raw = reader.GetString();
			if (string.IsNullOrWhiteSpace(raw))
				return default;

			if (DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime roundTrip))
				return roundTrip;

			if (DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime utc))
				return utc;

			throw new JsonException("Invalid DateTime value: " + raw);
		}

		public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString("O"));
		}
	}
}
