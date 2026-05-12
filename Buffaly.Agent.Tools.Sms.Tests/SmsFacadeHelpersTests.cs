using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BasicUtilities;
using Buffaly.Agent.Tools.Sms;

namespace Buffaly.Agent.Tools.Sms.Tests;

[TestClass]
public sealed class SmsFacadeHelpersTests
{
	// Verifies send normalization uses dashed output for 10-digit destinations.
	[TestMethod]
	[TestCategory("Unit")]
	public void NormalizeSendPhone_FormatsTenDigitsWithDashes()
	{
		string formatted = SmsPhoneUtil.NormalizeSendPhone("(555) 123-4567");

		Assert.AreEqual("555-123-4567", formatted);
	}

	// Verifies read normalization strips non-digit characters.
	[TestMethod]
	[TestCategory("Unit")]
	public void NormalizePhoneDigits_StripsNonDigits()
	{
		string normalized = SmsPhoneUtil.NormalizePhoneDigits(" +1 (555) 123-4567 ext.89 ");

		Assert.AreEqual("1555123456789", normalized);
	}

	// Verifies cursor keys preserve line|phone ordering used by persisted cursor behavior.
	[TestMethod]
	[TestCategory("Unit")]
	public void BuildCursorKey_PreservesExistingOrdering()
	{
		string cursorKey = SmsCursorUtil.BuildCursorKey("15551230000", "15557650000");

		Assert.AreEqual("15557650000|15551230000", cursorKey);
	}

	// Verifies send payload mapping keeps facade contract property names and values.
	[TestMethod]
	[TestCategory("Unit")]
	public void BuildSendPayload_UsesFacadeContractShape()
	{
		FeedingFrenzySendMessageResponse response = new()
		{
			WasSent = true,
			MessageID = 42,
			ToPhone = "15551230000",
			SentByPhone = "15557650000",
			CreatedUtc = new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc)
		};

		JsonObject payload = SmsPayloadMapper.BuildSendPayload(response, "15557650000");
		JsonElement root = ParsePayload(payload);

		Assert.IsTrue(root.GetProperty("Success").GetBoolean());
		Assert.AreEqual(42, root.GetProperty("MessageID").GetInt32());
		Assert.AreEqual("15551230000", root.GetProperty("ToPhone").GetString());
		Assert.AreEqual("15557650000", root.GetProperty("FromPhone").GetString());
		Assert.AreEqual("15557650000", root.GetProperty("LinePhone").GetString());
		Assert.AreEqual("2026-03-29T12:00:00.0000000Z", root.GetProperty("CreatedUtc").GetString());
	}

	// Verifies read payload mapping keeps cursor and direction behavior.
	[TestMethod]
	[TestCategory("Unit")]
	public void BuildMessagesPayload_MapsCursorAndDirection()
	{
		List<FeedingFrenzyMessage> rows =
		[
			new FeedingFrenzyMessage
			{
				MessageID = 8,
				MessageText = "inbound",
				SentByPhone = "15550001111",
				ReceivedByPhone = "15557650000",
				DateCreated = new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc),
				IsDelivered = true,
				IsReceived = true,
				IsDismissed = false
			},
			new FeedingFrenzyMessage
			{
				MessageID = 9,
				MessageText = "outbound",
				SentByPhone = "1 (555) 765-0000",
				ReceivedByPhone = "15550001111",
				DateCreated = new DateTime(2026, 3, 29, 11, 0, 0, DateTimeKind.Utc),
				IsDelivered = true,
				IsReceived = false,
				IsDismissed = false
			}
		];

		JsonObject payload = SmsPayloadMapper.BuildMessagesPayload("15550001111", "15557650000", 7, 100, rows);
		JsonElement root = ParsePayload(payload);
		JsonElement messages = root.GetProperty("Messages");

		Assert.IsTrue(root.GetProperty("Success").GetBoolean());
		Assert.AreEqual(9, root.GetProperty("CursorNext").GetInt32());
		Assert.AreEqual(2, messages.GetArrayLength());
		Assert.AreEqual("inbound", messages[0].GetProperty("Direction").GetString());
		Assert.AreEqual("outbound", messages[1].GetProperty("Direction").GetString());
	}

	// Verifies IsReceived=true always maps to inbound even when SentByPhone does not match requested line.
	[TestMethod]
	[TestCategory("Unit")]
	public void BuildMessagesPayload_UsesIsReceivedTrue_AsInbound_WhenSentByPhoneDiffersFromLine()
	{
		List<FeedingFrenzyMessage> rows =
		[
			new FeedingFrenzyMessage
			{
				MessageID = 21,
				MessageText = "provider-mismatch-inbound",
				SentByPhone = "6896001779",
				ReceivedByPhone = "14804140506",
				DateCreated = new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc),
				IsDelivered = true,
				IsReceived = true,
				IsDismissed = false
			}
		];

		JsonObject payload = SmsPayloadMapper.BuildMessagesPayload("14804140506", "16895880847", 20, 100, rows);
		JsonElement root = ParsePayload(payload);
		JsonElement message = root.GetProperty("Messages")[0];

		Assert.AreEqual("inbound", message.GetProperty("Direction").GetString());
	}

	// Verifies IsReceived=false always maps to outbound even when SentByPhone does not match requested line.
	[TestMethod]
	[TestCategory("Unit")]
	public void BuildMessagesPayload_UsesIsReceivedFalse_AsOutbound_WhenSentByPhoneDiffersFromLine()
	{
		List<FeedingFrenzyMessage> rows =
		[
			new FeedingFrenzyMessage
			{
				MessageID = 22,
				MessageText = "provider-mismatch-outbound",
				SentByPhone = "6896001779",
				ReceivedByPhone = "14804140506",
				DateCreated = new DateTime(2026, 3, 29, 12, 5, 0, DateTimeKind.Utc),
				IsDelivered = true,
				IsReceived = false,
				IsDismissed = false
			}
		];

		JsonObject payload = SmsPayloadMapper.BuildMessagesPayload("14804140506", "16895880847", 21, 100, rows);
		JsonElement root = ParsePayload(payload);
		JsonElement message = root.GetProperty("Messages")[0];

		Assert.AreEqual("outbound", message.GetProperty("Direction").GetString());
	}

	// Verifies send request shaping includes the expected Method and contract fields.
	[TestMethod]
	[TestCategory("Unit")]
	public void BuildSendRawRequest_UsesProductionContractFields()
	{
		FeedingFrenzySendMessageRawRequest request = SmsRequestContractMapper.BuildSendRawRequest(
			new FeedingFrenzySendMessageRequest("555-123-4567", "hello", "15557650000", "{\"TraceId\":\"abc\"}"));

		Assert.AreEqual("SendMessageRaw", request.Method);
		Assert.AreEqual("555-123-4567", request.ToPhone);
		Assert.AreEqual("hello", request.Message);
		Assert.AreEqual("{\"TraceId\":\"abc\"}", request.MetadataJson);
	}

	// Verifies read request shaping includes the expected Method and contract fields.
	[TestMethod]
	[TestCategory("Unit")]
	public void BuildReadRawRequest_UsesProductionContractFields()
	{
		FeedingFrenzyGetMessagesRawRequest request = SmsRequestContractMapper.BuildReadRawRequest(
			new FeedingFrenzyGetMessagesRequest("15550001111", "15557650000", 9, 50));

		Assert.AreEqual("GetMessagesByPhoneRaw", request.Method);
		Assert.AreEqual("15550001111", request.Phone);
		Assert.AreEqual(9, request.SinceMessageID);
		Assert.AreEqual(50, request.MaxRows);
	}

	// Verifies client requests include Authorization bearer token and Method contract fields.
	[TestMethod]
	[TestCategory("Unit")]
	public async Task SendMessageAsync_AddsBearerAuthAndMethodField()
	{
		CapturingHttpMessageHandler handler = new(_ =>
		{
			const string body = "{\"WasSent\":true,\"MessageID\":77,\"ToPhone\":\"555-123-4567\",\"SentByPhone\":\"15557650000\",\"CreatedUtc\":\"2026-03-29T12:00:00Z\"}";
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json")
			};
		});
		HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://ff.medek.ai") };
		FeedingFrenzySmsHttpClient client = new(httpClient, new FeedingFrenzySmsClientSettings(new Uri("https://ff.medek.ai"), "api-key-123", "/send", "/read"));

		_ = await client.SendMessageAsync(new FeedingFrenzySendMessageRequest("555-123-4567", "hello", "15557650000"));

		Assert.IsNotNull(handler.LastRequest);
		Assert.AreEqual("Bearer", handler.LastRequest!.Headers.Authorization?.Scheme);
		Assert.AreEqual("api-key-123", handler.LastRequest.Headers.Authorization?.Parameter);
		StringAssert.Contains(handler.LastRequestBody, "\"Method\":\"SendMessageRaw\"");
		Assert.IsFalse(handler.LastRequestBody.Contains("\"FromPhone\"", StringComparison.Ordinal), "Send payload must not include FromPhone.");
	}

	// Verifies client read path accepts raw array responses and maps/filter messages by line phone.
	[TestMethod]
	[TestCategory("Unit")]
	public async Task GetMessagesAsync_AcceptsRawArrayResponse_AndMapsMessages()
	{
		CapturingHttpMessageHandler handler = new(_ =>
		{
			const string body = "[" +
				"{\"MessageID\":18,\"MessageText\":\"inbound\",\"SentByPhone\":\"15550001111\",\"ReceivedByPhone\":\"15557650000\",\"DateCreated\":\"2026-03-29T10:00:00\",\"IsDelivered\":true,\"IsReceived\":true,\"IsDismissed\":false}," +
				"{\"MessageID\":19,\"MessageText\":\"outbound\",\"SentByPhone\":\"1 (555) 765-0000\",\"ReceivedByPhone\":\"15550001111\",\"DateCreated\":\"2026-03-29T11:00:00Z\",\"IsDelivered\":true,\"IsReceived\":true,\"IsDismissed\":false}," +
				"{\"MessageID\":20,\"MessageText\":\"other-line\",\"SentByPhone\":\"15559990000\",\"ReceivedByPhone\":\"15550001111\",\"DateCreated\":\"2026-03-29T12:00:00Z\",\"IsDelivered\":true,\"IsReceived\":true,\"IsDismissed\":false}" +
				"]";
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json")
			};
		});
		HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://ff.medek.ai") };
		FeedingFrenzySmsHttpClient client = new(httpClient, new FeedingFrenzySmsClientSettings(new Uri("https://ff.medek.ai"), "api-key-123", "/send", "/read"));

		FeedingFrenzyGetMessagesResponse response = await client.GetMessagesAsync(new FeedingFrenzyGetMessagesRequest("15550001111", "15557650000", 10, 50));

		Assert.AreEqual(2, response.Messages.Count);
		Assert.AreEqual(18, response.Messages[0].MessageID);
		Assert.AreEqual(19, response.Messages[1].MessageID);
		Assert.AreEqual(2026, response.Messages[0].DateCreated.Year);
		StringAssert.Contains(handler.LastRequestBody, "\"Method\":\"GetMessagesByPhoneRaw\"");
	}

	// Verifies read mapping falls back to unfiltered rows when provider phones do not match requested line.
	[TestMethod]
	[TestCategory("Unit")]
	public void MapReadRawResponse_FallsBackToUnfilteredRows_WhenLineDoesNotMatch()
	{
		List<FeedingFrenzyMessage> rows =
		[
			new FeedingFrenzyMessage
			{
				MessageID = 1,
				SentByPhone = "6896001779",
				ReceivedByPhone = "14804140506",
				MessageText = "m1",
				DateCreated = new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc)
			},
			new FeedingFrenzyMessage
			{
				MessageID = 2,
				SentByPhone = "14804140506",
				ReceivedByPhone = "6896001779",
				MessageText = "m2",
				DateCreated = new DateTime(2026, 3, 29, 2, 0, 0, DateTimeKind.Utc)
			}
		];

		FeedingFrenzyGetMessagesResponse response = SmsRequestContractMapper.MapReadRawResponse(rows);

		Assert.AreEqual(2, response.Messages.Count);
		Assert.AreEqual(1, response.Messages[0].MessageID);
		Assert.AreEqual(2, response.Messages[1].MessageID);
	}

	private static JsonElement ParsePayload(JsonObject payload)
	{
		using JsonDocument document = JsonDocument.Parse(JsonUtil.ToStringExt(payload).ToString());
		return document.RootElement.Clone();
	}

	private sealed class CapturingHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

		public CapturingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
		{
			_responseFactory = responseFactory;
		}

		public HttpRequestMessage? LastRequest { get; private set; }
		public string LastRequestBody { get; private set; } = string.Empty;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			LastRequest = request;
			LastRequestBody = request.Content == null ? string.Empty : request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
			return Task.FromResult(_responseFactory(request));
		}
	}
}
