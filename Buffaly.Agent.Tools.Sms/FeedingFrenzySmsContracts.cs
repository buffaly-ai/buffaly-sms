namespace Buffaly.Agent.Tools.Sms;

public sealed record FeedingFrenzySendMessageRequest(
	string ToPhone,
	string MessageBody,
	string FromPhone,
	string? MetadataJson = null);

public sealed class FeedingFrenzySendMessageResponse
{
	public bool WasSent { get; set; }
	public int MessageID { get; set; }
	public string ToPhone { get; set; } = string.Empty;
	public string SentByPhone { get; set; } = string.Empty;
	public DateTime CreatedUtc { get; set; }
}

public sealed record FeedingFrenzyGetMessagesRequest(
	string PhoneNumber,
	string LinePhone,
	int? SinceMessageId,
	int MaxRows);

public sealed class FeedingFrenzyMessage
{
	public int MessageID { get; set; }
	public string MessageText { get; set; } = string.Empty;
	public string SentByPhone { get; set; } = string.Empty;
	public string ReceivedByPhone { get; set; } = string.Empty;
	public DateTime DateCreated { get; set; }
	public bool IsDelivered { get; set; }
	public bool IsReceived { get; set; }
	public bool IsDismissed { get; set; }
}

public sealed class FeedingFrenzyGetMessagesResponse
{
	public List<FeedingFrenzyMessage> Messages { get; set; } = new();
}

public sealed class FeedingFrenzySendMessageRawRequest
{
	public string Method { get; set; } = "SendMessageRaw";
	public string ToPhone { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
	public string? MetadataJson { get; set; }
	public string? FromPhone { get; set; }
}

public sealed class FeedingFrenzySendMessageRawResponse
{
	public bool WasSent { get; set; }
	public int MessageID { get; set; }
	public string ToPhone { get; set; } = string.Empty;
	public string SentByPhone { get; set; } = string.Empty;
	public DateTime CreatedUtc { get; set; }
}

public sealed class FeedingFrenzyGetMessagesRawRequest
{
	public string Method { get; set; } = "GetMessagesByPhoneRawByLine";
	public string Phone { get; set; } = string.Empty;
	public string LinePhone { get; set; } = string.Empty;
	public int? SinceMessageID { get; set; }
	public int MaxRows { get; set; }
}

