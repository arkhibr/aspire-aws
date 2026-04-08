using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;

namespace Scenarios.SNS.Basic;

public class SnsBasicTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task CreateTopic_ShouldReturnArn()
    {
        var response = await fixture.SNS.CreateTopicAsync("test-topic");

        Assert.Contains("test-topic", response.TopicArn);
    }

    [Fact]
    public async Task Publish_WithSqsSubscription_ShouldDeliverMessage()
    {
        var topicArn = (await fixture.SNS.CreateTopicAsync("notify-topic")).TopicArn;
        var queueUrl = (await fixture.SQS.CreateQueueAsync("notify-queue")).QueueUrl;
        var queueArn = (await fixture.SQS.GetQueueAttributesAsync(queueUrl, ["QueueArn"]))
            .Attributes["QueueArn"];

        await fixture.SQS.SetQueueAttributesAsync(new SetQueueAttributesRequest
        {
            QueueUrl = queueUrl,
            Attributes = new Dictionary<string, string>
            {
                ["Policy"] = $$"""
                {
                  "Version": "2012-10-17",
                  "Statement": [
                    {
                      "Sid": "AllowSnsPublish",
                      "Effect": "Allow",
                      "Principal": { "Service": "sns.amazonaws.com" },
                      "Action": "sqs:SendMessage",
                      "Resource": "{{queueArn}}",
                      "Condition": {
                        "ArnEquals": { "aws:SourceArn": "{{topicArn}}" }
                      }
                    }
                  ]
                }
                """
            }
        });

        await fixture.SNS.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "sqs",
            Endpoint = queueArn
        });

        await fixture.SNS.PublishAsync(topicArn, "hello from SNS");

        var messages = await fixture.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 5
        });

        Assert.Single(messages.Messages);
        Assert.Contains("hello from SNS", messages.Messages[0].Body);
    }

    [Fact]
    public async Task ListTopics_ShouldIncludeCreatedTopic()
    {
        var topicArn = (await fixture.SNS.CreateTopicAsync("list-topic")).TopicArn;

        var response = await fixture.SNS.ListTopicsAsync();

        Assert.Contains(response.Topics, topic => topic.TopicArn == topicArn);
    }
}
