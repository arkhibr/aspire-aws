using Amazon.SQS.Model;

namespace Scenarios.SNS.SQS.Fanout;

public class SnsSqsFanoutTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task Publish_ShouldDeliverMessageToBothQueues()
    {
        await fixture.SNS.PublishAsync(fixture.TopicArn, "broadcast event");

        foreach (var queueUrl in new[] { fixture.Queue1Url, fixture.Queue2Url })
        {
            var messages = await fixture.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 5
            });

            Assert.Single(messages.Messages);
            Assert.Contains("broadcast event", messages.Messages[0].Body);
        }
    }
}
