using System.Text.Json;
using Buffaly.Agent.Tools.Sms;

namespace Buffaly.Agent.Tools.Sms.Tests.Integration;

[TestClass]
public sealed class SmsRemoteAcceptanceTests
{
	[TestMethod]
	[TestCategory("Integration")]
	public void SmsRemoteAcceptanceConfigurationDefaults_AreExpected()
	{
		SmsRemoteAcceptanceSettings settings = SmsRemoteAcceptanceSettings.Load();

		Assert.AreEqual("16895880847", settings.PrimaryLinePhone);
		Assert.AreEqual("16896001779", settings.SecondaryLinePhone);
		StringAssert.Contains(settings.MonitoredLines, "16896001779");
		StringAssert.Contains(settings.MonitoredLines, "18063018828");
		Assert.AreEqual("https://ff.medek.ai", settings.BaseUrl);
		Assert.IsTrue(settings.UseControlledPair);
	}

	[TestMethod]
	[TestCategory("Integration")]
	public void SmsRemoteAcceptance_SendAndReceive_TwoMessagesEachDirection()
	{
		SmsRemoteAcceptanceSettings settings = SmsRemoteAcceptanceSettings.Load();
		if (!settings.EnableManualRemoteTests)
			Assert.Inconclusive("Enable Buffaly.Sms.Tests.EnableManualRemote to run remote acceptance workflow.");
		if (string.IsNullOrWhiteSpace(settings.ApiKey))
			Assert.Inconclusive("SmsFeature:ApiKey must be configured for remote acceptance workflow.");
		if (!settings.UseControlledPair)
			Assert.Inconclusive("Buffaly.Sms.Tests.UseControlledPair must be enabled for autonomous controlled-pair acceptance workflow.");

		SmsRemoteAcceptanceHarness harness = new(settings);

		JsonElement resetA = harness.ResetCursor(settings.SecondaryLinePhone, settings.PrimaryLinePhone);
		Assert.IsTrue(SmsRemoteAcceptanceHarness.IsSuccess(resetA), "Cursor reset for A->B scope must succeed.");
		JsonElement resetB = harness.ResetCursor(settings.PrimaryLinePhone, settings.SecondaryLinePhone);
		Assert.IsTrue(SmsRemoteAcceptanceHarness.IsSuccess(resetB), "Cursor reset for B->A scope must succeed.");

		JsonElement sendA1 = harness.SendExplicitLine(settings.SecondaryLinePhone, settings.PrimaryLinePhone, "Buffaly controlled acceptance A1 " + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
		Assert.IsTrue(SmsRemoteAcceptanceHarness.IsSuccess(sendA1), "Outbound A1 send must succeed. Payload=" + sendA1.GetRawText());
		Assert.AreEqual(settings.PrimaryLinePhone, SmsRemoteAcceptanceHarness.GetOutboundRequestedLinePhone(sendA1));
		Assert.IsTrue(SmsRemoteAcceptanceHarness.GetOutboundMessageId(sendA1).GetValueOrDefault() > 0, "Outbound A1 must return a MessageID.");
		Assert.IsFalse(string.IsNullOrWhiteSpace(SmsRemoteAcceptanceHarness.GetOutboundActualFromPhone(sendA1)), "Outbound A1 must include actual provider from value.");
		JsonElement inboundToB1 = harness.WaitForNewMessage(settings.PrimaryLinePhone, settings.SecondaryLinePhone, settings.ReplyWaitSeconds, settings.PollIntervalSeconds);
		Assert.IsTrue(SmsRemoteAcceptanceHarness.GetMessages(inboundToB1).Count >= 1, "B must receive A1 through typed GetNew path.");

		JsonElement sendB1 = harness.SendExplicitLine(settings.PrimaryLinePhone, settings.SecondaryLinePhone, "Buffaly controlled acceptance B1 " + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
		Assert.IsTrue(SmsRemoteAcceptanceHarness.IsSuccess(sendB1), "Outbound B1 send must succeed.");
		Assert.AreEqual(settings.SecondaryLinePhone, SmsRemoteAcceptanceHarness.GetOutboundRequestedLinePhone(sendB1));
		Assert.IsTrue(SmsRemoteAcceptanceHarness.GetOutboundMessageId(sendB1).GetValueOrDefault() > 0, "Outbound B1 must return a MessageID.");
		Assert.IsFalse(string.IsNullOrWhiteSpace(SmsRemoteAcceptanceHarness.GetOutboundActualFromPhone(sendB1)), "Outbound B1 must include actual provider from value.");
		JsonElement inboundToA1 = harness.WaitForNewMessage(settings.SecondaryLinePhone, settings.PrimaryLinePhone, settings.ReplyWaitSeconds, settings.PollIntervalSeconds);
		Assert.IsTrue(SmsRemoteAcceptanceHarness.GetMessages(inboundToA1).Count >= 1, "A must receive B1 through typed GetNew path.");

		JsonElement sendA2 = harness.SendExplicitLine(settings.SecondaryLinePhone, settings.PrimaryLinePhone, "Buffaly controlled acceptance A2 " + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
		Assert.IsTrue(SmsRemoteAcceptanceHarness.IsSuccess(sendA2), "Outbound A2 send must succeed.");
		Assert.AreEqual(settings.PrimaryLinePhone, SmsRemoteAcceptanceHarness.GetOutboundRequestedLinePhone(sendA2));
		Assert.IsTrue(SmsRemoteAcceptanceHarness.GetOutboundMessageId(sendA2).GetValueOrDefault() > 0, "Outbound A2 must return a MessageID.");
		JsonElement inboundToB2 = harness.WaitForNewMessage(settings.PrimaryLinePhone, settings.SecondaryLinePhone, settings.ReplyWaitSeconds, settings.PollIntervalSeconds);
		Assert.IsTrue(SmsRemoteAcceptanceHarness.GetMessages(inboundToB2).Count >= 1, "B must receive A2 through typed GetNew path.");

		JsonElement sendB2 = harness.SendExplicitLine(settings.PrimaryLinePhone, settings.SecondaryLinePhone, "Buffaly controlled acceptance B2 " + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
		Assert.IsTrue(SmsRemoteAcceptanceHarness.IsSuccess(sendB2), "Outbound B2 send must succeed.");
		Assert.AreEqual(settings.SecondaryLinePhone, SmsRemoteAcceptanceHarness.GetOutboundRequestedLinePhone(sendB2));
		Assert.IsTrue(SmsRemoteAcceptanceHarness.GetOutboundMessageId(sendB2).GetValueOrDefault() > 0, "Outbound B2 must return a MessageID.");
		JsonElement inboundToA2 = harness.WaitForNewMessage(settings.SecondaryLinePhone, settings.PrimaryLinePhone, settings.ReplyWaitSeconds, settings.PollIntervalSeconds);
		Assert.IsTrue(SmsRemoteAcceptanceHarness.GetMessages(inboundToA2).Count >= 1, "A must receive B2 through typed GetNew path.");

		harness.DrainNewUntilIdle(settings.SecondaryLinePhone, settings.PrimaryLinePhone, 100, 2500, 150, 2);
		harness.DrainNewUntilIdle(settings.PrimaryLinePhone, settings.SecondaryLinePhone, 100, 2500, 150, 2);

		JsonElement finalBatchA = harness.DrainNewUntilIdle(settings.SecondaryLinePhone, settings.PrimaryLinePhone, 100, 2500, 150, 1);
		Assert.AreEqual(0, SmsRemoteAcceptanceHarness.GetMessages(finalBatchA).Count, "Final GetNew for A scope must return zero rows after cursor advancement.");
		JsonElement finalBatchB = harness.DrainNewUntilIdle(settings.PrimaryLinePhone, settings.SecondaryLinePhone, 100, 2500, 150, 1);
		Assert.AreEqual(0, SmsRemoteAcceptanceHarness.GetMessages(finalBatchB).Count, "Final GetNew for B scope must return zero rows after cursor advancement.");
	}

	[TestMethod]
	[TestCategory("Integration")]
	public void SmsRemoteAcceptance_ReadConversationSnapshot_Manual()
	{
		SmsRemoteAcceptanceSettings settings = SmsRemoteAcceptanceSettings.Load();
		if (!settings.EnableManualRemoteTests)
			Assert.Inconclusive("Enable Buffaly.Sms.Tests.EnableManualRemote to run manual remote snapshot check.");
		if (string.IsNullOrWhiteSpace(settings.ApiKey))
			Assert.Inconclusive("SmsFeature:ApiKey must be configured for remote snapshot check.");

		SmsRemoteAcceptanceHarness harness = new(settings);
		JsonElement snapshot = harness.ReadAllForConversation(settings.SecondaryLinePhone, settings.PrimaryLinePhone, null, 100);

		Assert.IsTrue(SmsRemoteAcceptanceHarness.IsSuccess(snapshot), "Snapshot read must return Success=true.");
		Assert.IsTrue(snapshot.TryGetProperty("Messages", out JsonElement messages), "Snapshot payload must contain Messages.");
		Assert.AreEqual(JsonValueKind.Array, messages.ValueKind, "Snapshot Messages must be an array.");
		Assert.IsTrue(snapshot.TryGetProperty("CursorNext", out _), "Snapshot payload must include CursorNext.");
	}
}

