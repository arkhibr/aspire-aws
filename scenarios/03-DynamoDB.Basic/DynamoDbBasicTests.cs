using Amazon.DynamoDBv2.Model;

namespace Scenarios.DynamoDB.Basic;

public class DynamoDbBasicTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task PutAndGetItem_ShouldRoundTrip()
    {
        await fixture.DynamoDB.PutItemAsync(Fixture.TableName, new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = "item-1" },
            ["name"] = new AttributeValue { S = "Alice" }
        });

        var response = await fixture.DynamoDB.GetItemAsync(
            Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "item-1" } });

        Assert.Equal("Alice", response.Item["name"].S);
    }

    [Fact]
    public async Task Scan_ShouldReturnAllItems()
    {
        await fixture.DynamoDB.PutItemAsync(
            Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "scan-1" } });
        await fixture.DynamoDB.PutItemAsync(
            Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "scan-2" } });

        var response = await fixture.DynamoDB.ScanAsync(new ScanRequest
        {
            TableName = Fixture.TableName
        });

        Assert.True(response.Count >= 2);
    }

    [Fact]
    public async Task DeleteItem_ShouldRemoveRecord()
    {
        await fixture.DynamoDB.PutItemAsync(
            Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "to-delete" } });

        await fixture.DynamoDB.DeleteItemAsync(
            Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "to-delete" } });

        var response = await fixture.DynamoDB.GetItemAsync(
            Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "to-delete" } });

        Assert.Empty(response.Item);
    }

    [Fact]
    public async Task Query_ShouldReturnMatchingItems()
    {
        await fixture.DynamoDB.PutItemAsync(Fixture.TableName, new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = "query-target" },
            ["status"] = new AttributeValue { S = "active" }
        });

        var response = await fixture.DynamoDB.QueryAsync(new QueryRequest
        {
            TableName = Fixture.TableName,
            KeyConditionExpression = "id = :id",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":id"] = new() { S = "query-target" }
            }
        });

        Assert.Single(response.Items);
        Assert.Equal("active", response.Items[0]["status"].S);
    }
}
