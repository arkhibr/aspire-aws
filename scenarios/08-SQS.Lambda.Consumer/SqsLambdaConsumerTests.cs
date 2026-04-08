using Amazon.DynamoDBv2.Model;
using Shared;

namespace Scenarios.SQS.LambdaConsumer;

public class SqsLambdaConsumerTests(Fixture fixture) : IClassFixture<Fixture>
{
    [SkipOnMacOsArm64LocalStackLambdaFact]
    public async Task SendMessage_ShouldTriggerLambda_AndPersistToDynamoDb()
    {
        await fixture.SQS.SendMessageAsync(
            fixture.QueueUrl,
            """{"event":"order-placed","orderId":"123"}""");

        await PollingHelper.WaitUntilAsync(async () =>
        {
            var scan = await fixture.DynamoDB.ScanAsync(new ScanRequest
            {
                TableName = Fixture.TableName
            });

            return scan.Items.Any(item =>
                item.TryGetValue("body", out var body) &&
                body.S.Contains("order-placed", StringComparison.Ordinal));
        }, timeout: TimeSpan.FromSeconds(30));
    }
}
