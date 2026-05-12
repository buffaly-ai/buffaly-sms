using BasicUtilities;

namespace Buffaly.Agent.Tools.Sms;

public static class SmsPhoneUtil
{
	// Normalizes phone values by keeping only decimal digits.
	public static string NormalizePhoneDigits(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return string.Empty;

		char[] digits = raw.Where(char.IsDigit).ToArray();
		return new string(digits);
	}

	// Normalizes destination phone values for send operations using known-good dashed formatting.
	public static string NormalizeSendPhone(string? raw)
	{
		string digits = NormalizePhoneDigits(raw);
		if (digits.Length == 10)
			return digits.Substring(0, 3) + "-" + digits.Substring(3, 3) + "-" + digits.Substring(6, 4);

		if (digits.Length == 11 && digits.StartsWith("1", StringComparison.Ordinal))
			return digits.Substring(0, 1) + "-" + digits.Substring(1, 3) + "-" + digits.Substring(4, 3) + "-" + digits.Substring(7, 4);

		return digits;
	}
}

public static class SmsCursorUtil
{
	// Builds the persisted cursor key with the line phone first to preserve existing behavior.
	public static string BuildCursorKey(string phoneNumber, string linePhone) => linePhone + "|" + phoneNumber;
}

public static class SmsPayloadMapper
{
	// Builds the send response payload expected by existing SmsFacade consumers.
	public static JsonObject BuildSendPayload(FeedingFrenzySendMessageResponse response, string resolvedLinePhone)
	{
		return new JsonObject
		{
			["Success"] = response.WasSent,
			["MessageID"] = response.MessageID,
			["ToPhone"] = response.ToPhone,
			["FromPhone"] = response.SentByPhone,
			["LinePhone"] = resolvedLinePhone,
			["CreatedUtc"] = response.CreatedUtc.ToString("O")
		};
	}

	// Builds the read response payload expected by existing SmsFacade consumers.
	public static JsonObject BuildMessagesPayload(string normalizedPhone, string linePhone, int? sinceMessageId, int maxRows, IReadOnlyCollection<FeedingFrenzyMessage> rows)
	{
		IEnumerable<FeedingFrenzyMessage> orderedRows = rows.OrderBy(row => row.MessageID).Take(maxRows);
		JsonArray messages = new();
		int cursorNext = sinceMessageId ?? 0;
		foreach (FeedingFrenzyMessage row in orderedRows)
		{
			if (row.MessageID > cursorNext)
				cursorNext = row.MessageID;
			string direction = ClassifyDirection(row, linePhone);

			messages.Add(new JsonObject
			{
				["MessageID"] = row.MessageID,
				["MessageText"] = row.MessageText,
				["SentByPhone"] = row.SentByPhone,
				["ReceivedByPhone"] = row.ReceivedByPhone,
				["DateCreated"] = row.DateCreated.ToString("O"),
				["IsDelivered"] = row.IsDelivered,
				["IsReceived"] = row.IsReceived,
				["IsDismissed"] = row.IsDismissed,
				["Direction"] = direction
			});
		}

		return new JsonObject
		{
			["Success"] = true,
			["PhoneNumber"] = normalizedPhone,
			["LinePhone"] = linePhone,
			["SinceMessageID"] = sinceMessageId.HasValue ? sinceMessageId.Value : null,
			["MaxRows"] = maxRows,
			["CursorNext"] = cursorNext,
			["Messages"] = messages
		};
	}

	// Classifies message direction using IsReceived signal first, then phone fallback heuristics when needed.
	private static string ClassifyDirection(FeedingFrenzyMessage row, string linePhone)
	{
		if (row.IsReceived)
			return "inbound";

		if (!row.IsReceived)
			return "outbound";

		return StringUtil.EqualNoCase(SmsPhoneUtil.NormalizePhoneDigits(row.SentByPhone), linePhone) ? "outbound" : "inbound";
	}
}

public static class SmsRequestContractMapper
{
	// Builds the authoritative raw send request body expected by the FeedingFrenzy production endpoint.
	public static FeedingFrenzySendMessageRawRequest BuildSendRawRequest(FeedingFrenzySendMessageRequest request)
	{
		return new FeedingFrenzySendMessageRawRequest
		{
			Method = "SendMessageRaw",
			ToPhone = request.ToPhone,
			Message = request.MessageBody,
			MetadataJson = request.MetadataJson,
			FromPhone = request.FromPhone
		};
	}

	// Maps the raw send response back to the facade-oriented send response contract.
	public static FeedingFrenzySendMessageResponse MapSendRawResponse(FeedingFrenzySendMessageRawResponse response)
	{
		return new FeedingFrenzySendMessageResponse
		{
			WasSent = response.WasSent,
			MessageID = response.MessageID,
			ToPhone = response.ToPhone,
			SentByPhone = response.SentByPhone,
			CreatedUtc = response.CreatedUtc
		};
	}

	// Builds the authoritative raw read request body expected by the FeedingFrenzy production endpoint.
	public static FeedingFrenzyGetMessagesRawRequest BuildReadRawRequest(FeedingFrenzyGetMessagesRequest request)
	{
		return new FeedingFrenzyGetMessagesRawRequest
		{
			Method = "GetMessagesByPhoneRawByLine",
			Phone = request.PhoneNumber,
			LinePhone = request.LinePhone,
			SinceMessageID = request.SinceMessageId,
			MaxRows = request.MaxRows
		};
	}

	// Maps raw read responses directly because line filtering is authoritative at the provider boundary.
	public static FeedingFrenzyGetMessagesResponse MapReadRawResponse(IReadOnlyCollection<FeedingFrenzyMessage> response)
	{
		return new FeedingFrenzyGetMessagesResponse
		{
			Messages = response.ToList()
		};
	}

}

