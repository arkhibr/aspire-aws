using Amazon.SQS.Model;

namespace Scenarios.SQS.Basic;

public class SqsBasicTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task CreateQueue_ShouldSucceed()
    {
        var response = await fixture.SQS.CreateQueueAsync("test-queue-create");

        Assert.NotEmpty(response.QueueUrl);
    }

    [Fact]
    public async Task SendAndReceiveMessage_ShouldRoundTrip()
    {
        var queueUrl = (await fixture.SQS.CreateQueueAsync("test-queue-rw")).QueueUrl;

        await fixture.SQS.SendMessageAsync(queueUrl, "hello sqs");

        var response = await fixture.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 5
        });

        Assert.Single(response.Messages);
        Assert.Equal("hello sqs", response.Messages[0].Body);
    }

    [Fact]
    public async Task DeleteMessage_ShouldRemoveFromQueue()
    {
        var queueUrl = (await fixture.SQS.CreateQueueAsync("test-queue-delete")).QueueUrl;

        await fixture.SQS.SendMessageAsync(queueUrl, "to-delete");

        var receive = await fixture.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1
        });

        await fixture.SQS.DeleteMessageAsync(queueUrl, receive.Messages[0].ReceiptHandle);

        var afterDelete = await fixture.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 1
        });

        Assert.Empty(afterDelete.Messages);
    }

    [Fact]
    public async Task DeadLetterQueue_ShouldBeConfigurable()
    {
        var dlqUrl = (await fixture.SQS.CreateQueueAsync("test-dlq")).QueueUrl;
        var dlqAttributes = await fixture.SQS.GetQueueAttributesAsync(dlqUrl, ["QueueArn"]);
        var dlqArn = dlqAttributes.Attributes["QueueArn"];

        var queueUrl = (await fixture.SQS.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = "test-queue-dlq",
            Attributes = new Dictionary<string, string>
            {
                ["RedrivePolicy"] = $$"""{"deadLetterTargetArn":"{{dlqArn}}","maxReceiveCount":"1"}"""
            }
        })).QueueUrl;

        var attributes = await fixture.SQS.GetQueueAttributesAsync(queueUrl, ["RedrivePolicy"]);

        Assert.Contains("deadLetterTargetArn", attributes.Attributes["RedrivePolicy"]);
    }
}
