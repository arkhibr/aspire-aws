using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Model;
using Shared;
using System.Text.Json;

namespace Scenarios.DynamoDB.Lambda;

public class DynamoDbLambdaTests(Fixture fixture) : IClassFixture<Fixture>
{
    [SkipOnMacOsArm64LocalStackLambdaFact]
    public async Task InvokeLambda_ShouldWriteToDynamoDb()
    {
        var payload = JsonSerializer.Serialize(new { id = "evt-001", type = "click" });

        await fixture.Lambda.InvokeAsync(new InvokeRequest
        {
            FunctionName = Fixture.FunctionName,
            Payload = payload
        });

        await PollingHelper.WaitUntilAsync(async () =>
        {
            var response = await fixture.DynamoDB.GetItemAsync(
                Fixture.ResultTable,
                new Dictionary<string, AttributeValue> { ["id"] = new() { S = "evt-001" } });

            return response.Item.ContainsKey("id");
        });

        var item = await fixture.DynamoDB.GetItemAsync(
            Fixture.ResultTable,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "evt-001" } });

        Assert.Equal("evt-001", item.Item["id"].S);
    }
}
