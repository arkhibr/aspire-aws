using Amazon.S3.Model;
using Amazon.SQS.Model;
using Shared;

namespace Scenarios.S3.SQS.Notification;

public class S3SqsNotificationTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task PutObject_ShouldSendNotificationToSqs()
    {
        await fixture.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Fixture.BucketName,
            Key = "upload.csv",
            ContentBody = "col1,col2"
        });

        await PollingHelper.WaitUntilAsync(async () =>
        {
            var response = await fixture.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = fixture.QueueUrl,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 2
            });

            return response.Messages.Any(message =>
                message.Body.Contains("upload.csv", StringComparison.Ordinal));
        }, timeout: TimeSpan.FromSeconds(20));
    }
}
