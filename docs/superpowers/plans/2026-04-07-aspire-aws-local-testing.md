# aspire-aws Local Testing Environment — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a catalog of 15 progressive AWS testing scenarios using .NET Aspire + LocalStack — from single-service basics to full multi-service pipelines — so engineers can test AWS integrations locally with `dotnet test`.

**Architecture:** Aspire AppHost manages a LocalStack container on port 4566. All scenario projects are independent xUnit test projects that extend a shared `LocalStackFixture`. Python Lambda functions live in `src/lambdas/`, are zipped and deployed to LocalStack by `LambdaDeployer` at test startup. Tests invoke Lambda indirectly via AWS event triggers (S3, SQS, EventBridge), never directly.

**Tech Stack:** .NET 9, .NET Aspire 9.x, xUnit 2.x, LocalStack 3.8 (Community), AWS SDK for .NET v3, Python 3.12 (Lambda handlers), boto3

**Current environment limitation:** on macOS ARM64, Lambda-backed scenarios `07`, `08`, `11`, `12`, `13`, and `14` are skipped because LocalStack 3.8 does not provide a reliable Lambda invoke path in this environment.

---

## File Map

```
aspire-aws.sln
src/
  AppHost/
    AppHost.csproj
    Program.cs
  Shared/
    Shared.csproj
    LocalStackFixture.cs      # IAsyncLifetime: starts Aspire AppHost, waits for LocalStack
    AwsClientFactory.cs       # Static factory methods per AWS service
    LambdaDeployer.cs         # Zips Python code and deploys to LocalStack Lambda
    PollingHelper.cs          # WaitUntilAsync for async Lambda side-effects
  lambdas/
    s3_processor/handler.py
    sqs_consumer/handler.py
    dynamodb_writer/handler.py
    fanout_processor/handler.py
    eventbridge_handler/handler.py
    stepfunctions_task/handler.py
scenarios/
  01-S3.Basic/
    01-S3.Basic.csproj
    Fixture.cs
    S3BasicTests.cs
  02-SQS.Basic/
    02-SQS.Basic.csproj
    Fixture.cs
    SqsBasicTests.cs
  03-DynamoDB.Basic/
    03-DynamoDB.Basic.csproj
    Fixture.cs
    DynamoDbBasicTests.cs
  04-SNS.Basic/
    04-SNS.Basic.csproj
    Fixture.cs
    SnsBasicTests.cs
  05-SSM.Basic/
    05-SSM.Basic.csproj
    Fixture.cs
    SsmBasicTests.cs
  06-SecretsManager.Basic/
    06-SecretsManager.Basic.csproj
    Fixture.cs
    SecretsManagerBasicTests.cs
  07-S3.Lambda.Trigger/
    07-S3.Lambda.Trigger.csproj
    Fixture.cs
    S3LambdaTriggerTests.cs
  08-SQS.Lambda.Consumer/
    08-SQS.Lambda.Consumer.csproj
    Fixture.cs
    SqsLambdaConsumerTests.cs
  09-SNS.SQS.Fanout/
    09-SNS.SQS.Fanout.csproj
    Fixture.cs
    SnsSqsFanoutTests.cs
  10-S3.SQS.Notification/
    10-S3.SQS.Notification.csproj
    Fixture.cs
    S3SqsNotificationTests.cs
  11-DynamoDB.Lambda/
    11-DynamoDB.Lambda.csproj
    Fixture.cs
    DynamoDbLambdaTests.cs
  12-Pipeline.S3.SQS.Lambda.DynamoDB/
    12-Pipeline.S3.SQS.Lambda.DynamoDB.csproj
    Fixture.cs
    FileProcessingPipelineTests.cs
  13-Pipeline.SNS.SQS.Lambda.S3/
    13-Pipeline.SNS.SQS.Lambda.S3.csproj
    Fixture.cs
    EventFanoutPipelineTests.cs
  14-EventBridge.Lambda/
    14-EventBridge.Lambda.csproj
    Fixture.cs
    EventBridgeLambdaTests.cs
  15-StepFunctions.Orchestration/
    15-StepFunctions.Orchestration.csproj
    Fixture.cs
    StepFunctionsTests.cs
```

---

## Task 1: Solution Scaffold

**Files:** Creates `aspire-aws.sln`, `src/AppHost/`, `src/Shared/`

- [ ] **Step 1: Create solution and projects**

```bash
cd /Volumes/Marco-Dev/dev/aspire-aws
dotnet new sln -n aspire-aws
dotnet new aspire-apphost -n AppHost -o src/AppHost
dotnet new classlib -n Shared -o src/Shared --framework net9.0
dotnet sln add src/AppHost/AppHost.csproj
dotnet sln add src/Shared/Shared.csproj
```

- [ ] **Step 2: Delete the default Class1.cs from Shared**

```bash
rm src/Shared/Class1.cs
```

- [ ] **Step 3: Add Shared reference to AppHost (for Projects.AppHost discovery) and add packages to Shared**

```bash
cd src/Shared
dotnet add reference ../AppHost/AppHost.csproj
dotnet add package Aspire.Hosting.Testing
dotnet add package AWSSDK.S3
dotnet add package AWSSDK.SQS
dotnet add package AWSSDK.SimpleNotificationService
dotnet add package AWSSDK.DynamoDBv2
dotnet add package AWSSDK.Lambda
dotnet add package AWSSDK.SimpleSystemsManagement
dotnet add package AWSSDK.SecretsManager
dotnet add package AWSSDK.EventBridge
dotnet add package AWSSDK.StepFunctions
dotnet add package xunit
cd ../..
```

- [ ] **Step 4: Verify solution builds**

```bash
dotnet build aspire-aws.sln
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "chore: scaffold solution with AppHost and Shared projects"
```

---

## Task 2: AppHost — LocalStack Container

**Files:** Modify `src/AppHost/Program.cs`

- [ ] **Step 1: Replace AppHost Program.cs**

```csharp
// src/AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

builder
    .AddContainer("localstack", "localstack/localstack", "3.8")
    .WithEnvironment("SERVICES", "s3,sqs,sns,dynamodb,lambda,ssm,secretsmanager,events,stepfunctions")
    .WithEnvironment("LAMBDA_EXECUTOR", "docker")
    .WithEnvironment("DOCKER_HOST", "unix:///var/run/docker.sock")
    .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock")
    .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "gateway");

builder.Build().Run();
```

> **Note:** port 4566 is fixed. Run one scenario project at a time to avoid port conflicts.
> On CI without Docker-in-Docker, set `LAMBDA_EXECUTOR=local` instead.

- [ ] **Step 2: Verify AppHost builds**

```bash
dotnet build src/AppHost/AppHost.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/AppHost/Program.cs
git commit -m "feat: configure LocalStack container in AppHost"
```

---

## Task 3: Shared — LocalStackFixture and PollingHelper

**Files:** Create `src/Shared/LocalStackFixture.cs`, `src/Shared/PollingHelper.cs`

- [ ] **Step 1: Create LocalStackFixture.cs**

```csharp
// src/Shared/LocalStackFixture.cs
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Shared;

public class LocalStackFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    public const string Endpoint = "http://localhost:4566";

    public virtual async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();
        await WaitForLocalStackAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }

    private static async Task WaitForLocalStackAsync()
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(120);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var resp = await http.GetAsync($"{Endpoint}/_localstack/health");
                if (resp.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(1000);
        }
        throw new TimeoutException("LocalStack did not become healthy within 120s.");
    }
}
```

- [ ] **Step 2: Create PollingHelper.cs**

```csharp
// src/Shared/PollingHelper.cs
namespace Shared;

public static class PollingHelper
{
    /// <summary>
    /// Polls the condition every 500ms until it returns true or the timeout elapses.
    /// Use this instead of Task.Delay when waiting for async Lambda side-effects.
    /// </summary>
    public static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? interval = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        interval ??= TimeSpan.FromMilliseconds(500);
        var deadline = DateTime.UtcNow + timeout.Value;

        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return;
            await Task.Delay(interval.Value);
        }

        throw new TimeoutException(
            $"Condition was not met within {timeout.Value.TotalSeconds}s.");
    }
}
```

- [ ] **Step 3: Build Shared**

```bash
dotnet build src/Shared/Shared.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/Shared/LocalStackFixture.cs src/Shared/PollingHelper.cs
git commit -m "feat: add LocalStackFixture and PollingHelper to Shared"
```

---

## Task 4: Shared — AwsClientFactory

**Files:** Create `src/Shared/AwsClientFactory.cs`

- [ ] **Step 1: Create AwsClientFactory.cs**

```csharp
// src/Shared/AwsClientFactory.cs
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.Lambda;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecretsManager;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.SQS;
using Amazon.StepFunctions;

namespace Shared;

/// <summary>
/// Creates AWS SDK clients pointing to LocalStack.
/// Always uses fake credentials (LocalStack ignores real ones).
/// </summary>
public static class AwsClientFactory
{
    private static BasicAWSCredentials Creds => new("test", "test");

    // ForcePathStyle=true is required for S3 on LocalStack
    public static AmazonS3Client S3(string endpoint = LocalStackFixture.Endpoint) =>
        new(Creds, new AmazonS3Config { ServiceURL = endpoint, ForcePathStyle = true });

    public static AmazonSQSClient SQS(string endpoint = LocalStackFixture.Endpoint) =>
        new(Creds, new AmazonSQSConfig { ServiceURL = endpoint });

    public static AmazonSimpleNotificationServiceClient SNS(string endpoint = LocalStackFixture.Endpoint) =>
        new(Creds, new AmazonSimpleNotificationServiceConfig { ServiceURL = endpoint });

    public static AmazonDynamoDBClient DynamoDB(string endpoint = LocalStackFixture.Endpoint) =>
        new(Creds, new AmazonDynamoDBConfig { ServiceURL = endpoint });

    public static AmazonLambdaClient Lambda(string endpoint = LocalStackFixture.Endpoint) =>
        new(Creds, new AmazonLambdaConfig { ServiceURL = endpoint });

    public static AmazonSimpleSystemsManagementClient SSM(string endpoint = LocalStackFixture.Endpoint) =>
        new(Creds, new AmazonSimpleSystemsManagementConfig { ServiceURL = endpoint });

    public static AmazonSecretsManagerClient SecretsManager(string endpoint = LocalStackFixture.Endpoint) =>
        new(Creds, new AmazonSecretsManagerConfig { ServiceURL = endpoint });

    public static AmazonEventBridgeClient EventBridge(string endpoint = LocalStackFixture.Endpoint) =>
        new(Creds, new AmazonEventBridgeConfig { ServiceURL = endpoint });

    public static AmazonStepFunctionsClient StepFunctions(string endpoint = LocalStackFixture.Endpoint) =>
        new(Creds, new AmazonStepFunctionsConfig { ServiceURL = endpoint });
}
```

- [ ] **Step 2: Build**

```bash
dotnet build src/Shared/Shared.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/Shared/AwsClientFactory.cs
git commit -m "feat: add AwsClientFactory to Shared"
```

---

## Task 5: Shared — LambdaDeployer

**Files:** Create `src/Shared/LambdaDeployer.cs`

- [ ] **Step 1: Create LambdaDeployer.cs**

```csharp
// src/Shared/LambdaDeployer.cs
using System.IO.Compression;
using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace Shared;

/// <summary>
/// Zips a Python Lambda handler directory and deploys it to LocalStack.
/// Call DeployAsync in your scenario Fixture.InitializeAsync before running tests.
/// </summary>
public class LambdaDeployer(AmazonLambdaClient client)
{
    private const string FakeRole = "arn:aws:iam::000000000000:role/local-role";

    public async Task DeployAsync(string functionName, string lambdaFolderName)
    {
        var sourcePath = ResolveLambdaPath(lambdaFolderName);
        var zipBytes = CreateZip(sourcePath);

        await client.CreateFunctionAsync(new CreateFunctionRequest
        {
            FunctionName = functionName,
            Runtime = Runtime.Python312,
            Handler = "handler.lambda_handler",
            Role = FakeRole,
            Code = new FunctionCode { ZipFile = new MemoryStream(zipBytes) },
            Environment = new Amazon.Lambda.Model.Environment
            {
                Variables = new Dictionary<string, string>
                {
                    ["AWS_ENDPOINT_URL"] = LocalStackFixture.Endpoint
                }
            }
        });

        await WaitUntilActiveAsync(functionName);
    }

    /// <summary>
    /// Walks up from the test output directory to find the solution root,
    /// then resolves src/lambdas/<folderName>.
    /// </summary>
    private static string ResolveLambdaPath(string folderName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;

        if (dir is null)
            throw new DirectoryNotFoundException("Could not locate solution root.");

        var path = Path.Combine(dir.FullName, "src", "lambdas", folderName);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Lambda folder not found: {path}");

        return path;
    }

    private static byte[] CreateZip(string sourcePath)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.GetFiles(sourcePath, "*.py", SearchOption.AllDirectories))
            {
                var entryName = Path.GetRelativePath(sourcePath, file).Replace('\\', '/');
                archive.CreateEntryFromFile(file, entryName);
            }
        }
        return ms.ToArray();
    }

    private async Task WaitUntilActiveAsync(string functionName)
    {
        for (int i = 0; i < 30; i++)
        {
            try
            {
                var resp = await client.GetFunctionAsync(
                    new GetFunctionRequest { FunctionName = functionName });
                if (resp.Configuration.State == State.Active) return;
            }
            catch { }
            await Task.Delay(1000);
        }
        throw new TimeoutException(
            $"Lambda '{functionName}' did not become Active within 30s.");
    }
}
```

- [ ] **Step 2: Build**

```bash
dotnet build src/Shared/Shared.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/Shared/LambdaDeployer.cs
git commit -m "feat: add LambdaDeployer to Shared"
```

---

## Task 6: Scenario 01 — S3 Basic

**Files:** Create `scenarios/01-S3.Basic/`

- [ ] **Step 1: Create project and add to solution**

```bash
dotnet new xunit -n "01-S3.Basic" -o scenarios/01-S3.Basic --framework net9.0
dotnet sln add scenarios/01-S3.Basic/01-S3.Basic.csproj
cd scenarios/01-S3.Basic
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.S3
cd ../..
```

- [ ] **Step 2: Delete the default UnitTest1.cs**

```bash
rm scenarios/01-S3.Basic/UnitTest1.cs
```

- [ ] **Step 3: Write Fixture.cs**

```csharp
// scenarios/01-S3.Basic/Fixture.cs
using Amazon.S3;
using Shared;

namespace Scenarios.S3.Basic;

public class Fixture : LocalStackFixture
{
    public AmazonS3Client S3 { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        S3 = AwsClientFactory.S3();
    }

    public override async Task DisposeAsync()
    {
        S3.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 4: Write S3BasicTests.cs**

```csharp
// scenarios/01-S3.Basic/S3BasicTests.cs
using Amazon.S3;
using Amazon.S3.Model;
using Shared;

namespace Scenarios.S3.Basic;

public class S3BasicTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task CreateBucket_ShouldSucceed()
    {
        await fixture.S3.PutBucketAsync("test-bucket-create");
        var buckets = await fixture.S3.ListBucketsAsync();
        Assert.Contains(buckets.Buckets, b => b.BucketName == "test-bucket-create");
    }

    [Fact]
    public async Task PutAndGetObject_ShouldRoundTrip()
    {
        await fixture.S3.PutBucketAsync("test-bucket-rw");
        await fixture.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = "test-bucket-rw",
            Key = "hello.txt",
            ContentBody = "hello world"
        });

        var resp = await fixture.S3.GetObjectAsync("test-bucket-rw", "hello.txt");
        using var reader = new StreamReader(resp.ResponseStream);
        var content = await reader.ReadToEndAsync();

        Assert.Equal("hello world", content);
    }

    [Fact]
    public async Task ListObjects_ShouldReturnUploadedKeys()
    {
        await fixture.S3.PutBucketAsync("test-bucket-list");
        await fixture.S3.PutObjectAsync(new PutObjectRequest
            { BucketName = "test-bucket-list", Key = "a.txt", ContentBody = "a" });
        await fixture.S3.PutObjectAsync(new PutObjectRequest
            { BucketName = "test-bucket-list", Key = "b.txt", ContentBody = "b" });

        var resp = await fixture.S3.ListObjectsV2Async(
            new ListObjectsV2Request { BucketName = "test-bucket-list" });

        Assert.Equal(2, resp.S3Objects.Count);
    }

    [Fact]
    public async Task GetPresignedUrl_ShouldReturnAccessibleUrl()
    {
        await fixture.S3.PutBucketAsync("test-bucket-presign");
        await fixture.S3.PutObjectAsync(new PutObjectRequest
            { BucketName = "test-bucket-presign", Key = "file.txt", ContentBody = "data" });

        var url = fixture.S3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = "test-bucket-presign",
            Key = "file.txt",
            Expires = DateTime.UtcNow.AddMinutes(5)
        });

        using var http = new HttpClient();
        var resp = await http.GetAsync(url);
        Assert.True(resp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task DeleteObject_ShouldRemoveKey()
    {
        await fixture.S3.PutBucketAsync("test-bucket-delete");
        await fixture.S3.PutObjectAsync(new PutObjectRequest
            { BucketName = "test-bucket-delete", Key = "todelete.txt", ContentBody = "x" });

        await fixture.S3.DeleteObjectAsync("test-bucket-delete", "todelete.txt");

        var resp = await fixture.S3.ListObjectsV2Async(
            new ListObjectsV2Request { BucketName = "test-bucket-delete" });
        Assert.Empty(resp.S3Objects);
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test scenarios/01-S3.Basic/ -v normal
```

Expected: `5 passed`.

- [ ] **Step 6: Commit**

```bash
git add scenarios/01-S3.Basic/
git commit -m "feat: add scenario 01 - S3 Basic"
```

---

## Task 7: Scenario 02 — SQS Basic

**Files:** Create `scenarios/02-SQS.Basic/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "02-SQS.Basic" -o scenarios/02-SQS.Basic --framework net9.0
dotnet sln add scenarios/02-SQS.Basic/02-SQS.Basic.csproj
cd scenarios/02-SQS.Basic
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.SQS
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/02-SQS.Basic/Fixture.cs
using Amazon.SQS;
using Shared;

namespace Scenarios.SQS.Basic;

public class Fixture : LocalStackFixture
{
    public AmazonSQSClient SQS { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        SQS = AwsClientFactory.SQS();
    }

    public override async Task DisposeAsync()
    {
        SQS.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write SqsBasicTests.cs**

```csharp
// scenarios/02-SQS.Basic/SqsBasicTests.cs
using Amazon.SQS.Model;
using Shared;

namespace Scenarios.SQS.Basic;

public class SqsBasicTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task CreateQueue_ShouldSucceed()
    {
        var resp = await fixture.SQS.CreateQueueAsync("test-queue-create");
        Assert.NotEmpty(resp.QueueUrl);
    }

    [Fact]
    public async Task SendAndReceiveMessage_ShouldRoundTrip()
    {
        var queueUrl = (await fixture.SQS.CreateQueueAsync("test-queue-rw")).QueueUrl;
        await fixture.SQS.SendMessageAsync(queueUrl, "hello sqs");

        var resp = await fixture.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 5
        });

        Assert.Single(resp.Messages);
        Assert.Equal("hello sqs", resp.Messages[0].Body);
    }

    [Fact]
    public async Task DeleteMessage_ShouldRemoveFromQueue()
    {
        var queueUrl = (await fixture.SQS.CreateQueueAsync("test-queue-delete")).QueueUrl;
        await fixture.SQS.SendMessageAsync(queueUrl, "to-delete");

        var receive = await fixture.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
            { QueueUrl = queueUrl, MaxNumberOfMessages = 1 });

        await fixture.SQS.DeleteMessageAsync(queueUrl, receive.Messages[0].ReceiptHandle);

        var after = await fixture.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
            { QueueUrl = queueUrl, MaxNumberOfMessages = 1, WaitTimeSeconds = 1 });
        Assert.Empty(after.Messages);
    }

    [Fact]
    public async Task DeadLetterQueue_ShouldBeConfigurable()
    {
        var dlqUrl = (await fixture.SQS.CreateQueueAsync("test-dlq")).QueueUrl;
        var dlqAttrs = await fixture.SQS.GetQueueAttributesAsync(
            dlqUrl, new List<string> { "QueueArn" });
        var dlqArn = dlqAttrs.Attributes["QueueArn"];

        var queueUrl = (await fixture.SQS.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = "test-queue-dlq",
            Attributes = new Dictionary<string, string>
            {
                ["RedrivePolicy"] = $$"""{"deadLetterTargetArn":"{{dlqArn}}","maxReceiveCount":"1"}"""
            }
        })).QueueUrl;

        var attrs = await fixture.SQS.GetQueueAttributesAsync(
            queueUrl, new List<string> { "RedrivePolicy" });
        Assert.Contains("deadLetterTargetArn", attrs.Attributes["RedrivePolicy"]);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test scenarios/02-SQS.Basic/ -v normal
```

Expected: `4 passed`.

- [ ] **Step 5: Commit**

```bash
git add scenarios/02-SQS.Basic/
git commit -m "feat: add scenario 02 - SQS Basic"
```

---

## Task 8: Scenario 03 — DynamoDB Basic

**Files:** Create `scenarios/03-DynamoDB.Basic/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "03-DynamoDB.Basic" -o scenarios/03-DynamoDB.Basic --framework net9.0
dotnet sln add scenarios/03-DynamoDB.Basic/03-DynamoDB.Basic.csproj
cd scenarios/03-DynamoDB.Basic
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.DynamoDBv2
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/03-DynamoDB.Basic/Fixture.cs
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Shared;

namespace Scenarios.DynamoDB.Basic;

public class Fixture : LocalStackFixture
{
    public AmazonDynamoDBClient DynamoDB { get; private set; } = null!;
    public const string TableName = "items";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        DynamoDB = AwsClientFactory.DynamoDB();

        await DynamoDB.CreateTableAsync(new CreateTableRequest
        {
            TableName = TableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new() { AttributeName = "id", AttributeType = ScalarAttributeType.S }
            },
            KeySchema = new List<KeySchemaElement>
            {
                new() { AttributeName = "id", KeyType = KeyType.HASH }
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }

    public override async Task DisposeAsync()
    {
        DynamoDB.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write DynamoDbBasicTests.cs**

```csharp
// scenarios/03-DynamoDB.Basic/DynamoDbBasicTests.cs
using Amazon.DynamoDBv2.Model;
using Shared;

namespace Scenarios.DynamoDB.Basic;

public class DynamoDbBasicTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task PutAndGetItem_ShouldRoundTrip()
    {
        await fixture.DynamoDB.PutItemAsync(Fixture.TableName,
            new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = "item-1" },
                ["name"] = new() { S = "Alice" }
            });

        var resp = await fixture.DynamoDB.GetItemAsync(Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "item-1" } });

        Assert.Equal("Alice", resp.Item["name"].S);
    }

    [Fact]
    public async Task Scan_ShouldReturnAllItems()
    {
        await fixture.DynamoDB.PutItemAsync(Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "scan-1" } });
        await fixture.DynamoDB.PutItemAsync(Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "scan-2" } });

        var resp = await fixture.DynamoDB.ScanAsync(
            new ScanRequest { TableName = Fixture.TableName });

        Assert.True(resp.Count >= 2);
    }

    [Fact]
    public async Task DeleteItem_ShouldRemoveRecord()
    {
        await fixture.DynamoDB.PutItemAsync(Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "to-delete" } });

        await fixture.DynamoDB.DeleteItemAsync(Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "to-delete" } });

        var resp = await fixture.DynamoDB.GetItemAsync(Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "to-delete" } });

        Assert.Empty(resp.Item);
    }

    [Fact]
    public async Task Query_ShouldReturnMatchingItems()
    {
        await fixture.DynamoDB.PutItemAsync(Fixture.TableName,
            new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = "query-target" },
                ["status"] = new() { S = "active" }
            });

        var resp = await fixture.DynamoDB.QueryAsync(new QueryRequest
        {
            TableName = Fixture.TableName,
            KeyConditionExpression = "id = :id",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":id"] = new() { S = "query-target" }
            }
        });

        Assert.Single(resp.Items);
        Assert.Equal("active", resp.Items[0]["status"].S);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test scenarios/03-DynamoDB.Basic/ -v normal
```

Expected: `4 passed`.

- [ ] **Step 5: Commit**

```bash
git add scenarios/03-DynamoDB.Basic/
git commit -m "feat: add scenario 03 - DynamoDB Basic"
```

---

## Task 9: Scenario 04 — SNS Basic

**Files:** Create `scenarios/04-SNS.Basic/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "04-SNS.Basic" -o scenarios/04-SNS.Basic --framework net9.0
dotnet sln add scenarios/04-SNS.Basic/04-SNS.Basic.csproj
cd scenarios/04-SNS.Basic
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.SimpleNotificationService
dotnet add package AWSSDK.SQS
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/04-SNS.Basic/Fixture.cs
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Shared;

namespace Scenarios.SNS.Basic;

public class Fixture : LocalStackFixture
{
    public AmazonSimpleNotificationServiceClient SNS { get; private set; } = null!;
    public AmazonSQSClient SQS { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        SNS = AwsClientFactory.SNS();
        SQS = AwsClientFactory.SQS();
    }

    public override async Task DisposeAsync()
    {
        SNS.Dispose();
        SQS.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write SnsBasicTests.cs**

```csharp
// scenarios/04-SNS.Basic/SnsBasicTests.cs
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using Shared;

namespace Scenarios.SNS.Basic;

public class SnsBasicTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task CreateTopic_ShouldReturnArn()
    {
        var resp = await fixture.SNS.CreateTopicAsync("test-topic");
        Assert.Contains("test-topic", resp.TopicArn);
    }

    [Fact]
    public async Task Publish_WithSqsSubscription_ShouldDeliverMessage()
    {
        var topicArn = (await fixture.SNS.CreateTopicAsync("notify-topic")).TopicArn;
        var queueUrl = (await fixture.SQS.CreateQueueAsync("notify-queue")).QueueUrl;
        var queueArn = (await fixture.SQS.GetQueueAttributesAsync(
            queueUrl, new List<string> { "QueueArn" })).Attributes["QueueArn"];

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
        var resp = await fixture.SNS.ListTopicsAsync();
        Assert.Contains(resp.Topics, t => t.TopicArn == topicArn);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test scenarios/04-SNS.Basic/ -v normal
```

Expected: `3 passed`.

- [ ] **Step 5: Commit**

```bash
git add scenarios/04-SNS.Basic/
git commit -m "feat: add scenario 04 - SNS Basic"
```

---

## Task 10: Scenario 05 — SSM Basic

**Files:** Create `scenarios/05-SSM.Basic/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "05-SSM.Basic" -o scenarios/05-SSM.Basic --framework net9.0
dotnet sln add scenarios/05-SSM.Basic/05-SSM.Basic.csproj
cd scenarios/05-SSM.Basic
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.SimpleSystemsManagement
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/05-SSM.Basic/Fixture.cs
using Amazon.SimpleSystemsManagement;
using Shared;

namespace Scenarios.SSM.Basic;

public class Fixture : LocalStackFixture
{
    public AmazonSimpleSystemsManagementClient SSM { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        SSM = AwsClientFactory.SSM();
    }

    public override async Task DisposeAsync()
    {
        SSM.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write SsmBasicTests.cs**

```csharp
// scenarios/05-SSM.Basic/SsmBasicTests.cs
using Amazon.SimpleSystemsManagement.Model;
using Shared;

namespace Scenarios.SSM.Basic;

public class SsmBasicTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task PutAndGetParameter_ShouldRoundTrip()
    {
        await fixture.SSM.PutParameterAsync(new PutParameterRequest
        {
            Name = "/app/config/db-host",
            Value = "localhost",
            Type = ParameterType.String
        });

        var resp = await fixture.SSM.GetParameterAsync(
            new GetParameterRequest { Name = "/app/config/db-host" });

        Assert.Equal("localhost", resp.Parameter.Value);
    }

    [Fact]
    public async Task PutSecureStringParameter_ShouldBeRetrievable()
    {
        await fixture.SSM.PutParameterAsync(new PutParameterRequest
        {
            Name = "/app/secrets/api-key",
            Value = "super-secret",
            Type = ParameterType.SecureString
        });

        var resp = await fixture.SSM.GetParameterAsync(new GetParameterRequest
        {
            Name = "/app/secrets/api-key",
            WithDecryption = true
        });

        Assert.Equal("super-secret", resp.Parameter.Value);
    }

    [Fact]
    public async Task GetParametersByPath_ShouldReturnAllUnderPrefix()
    {
        await fixture.SSM.PutParameterAsync(new PutParameterRequest
            { Name = "/myapp/env/host", Value = "host-val", Type = ParameterType.String });
        await fixture.SSM.PutParameterAsync(new PutParameterRequest
            { Name = "/myapp/env/port", Value = "5432", Type = ParameterType.String });

        var resp = await fixture.SSM.GetParametersByPathAsync(
            new GetParametersByPathRequest { Path = "/myapp/env", Recursive = true });

        Assert.Equal(2, resp.Parameters.Count);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test scenarios/05-SSM.Basic/ -v normal
```

Expected: `3 passed`.

- [ ] **Step 5: Commit**

```bash
git add scenarios/05-SSM.Basic/
git commit -m "feat: add scenario 05 - SSM Basic"
```

---

## Task 11: Scenario 06 — Secrets Manager Basic

**Files:** Create `scenarios/06-SecretsManager.Basic/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "06-SecretsManager.Basic" -o scenarios/06-SecretsManager.Basic --framework net9.0
dotnet sln add scenarios/06-SecretsManager.Basic/06-SecretsManager.Basic.csproj
cd scenarios/06-SecretsManager.Basic
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.SecretsManager
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/06-SecretsManager.Basic/Fixture.cs
using Amazon.SecretsManager;
using Shared;

namespace Scenarios.SecretsManager.Basic;

public class Fixture : LocalStackFixture
{
    public AmazonSecretsManagerClient SM { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        SM = AwsClientFactory.SecretsManager();
    }

    public override async Task DisposeAsync()
    {
        SM.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write SecretsManagerBasicTests.cs**

```csharp
// scenarios/06-SecretsManager.Basic/SecretsManagerBasicTests.cs
using Amazon.SecretsManager.Model;
using Shared;

namespace Scenarios.SecretsManager.Basic;

public class SecretsManagerBasicTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task CreateAndGetSecret_ShouldRoundTrip()
    {
        await fixture.SM.CreateSecretAsync(new CreateSecretRequest
        {
            Name = "myapp/db-password",
            SecretString = "p@ssw0rd"
        });

        var resp = await fixture.SM.GetSecretValueAsync(
            new GetSecretValueRequest { SecretId = "myapp/db-password" });

        Assert.Equal("p@ssw0rd", resp.SecretString);
    }

    [Fact]
    public async Task UpdateSecret_ShouldOverwriteValue()
    {
        await fixture.SM.CreateSecretAsync(new CreateSecretRequest
        {
            Name = "myapp/api-key",
            SecretString = "old-key"
        });

        await fixture.SM.PutSecretValueAsync(new PutSecretValueRequest
        {
            SecretId = "myapp/api-key",
            SecretString = "new-key"
        });

        var resp = await fixture.SM.GetSecretValueAsync(
            new GetSecretValueRequest { SecretId = "myapp/api-key" });

        Assert.Equal("new-key", resp.SecretString);
    }

    [Fact]
    public async Task DeleteSecret_ShouldMakeItInaccessible()
    {
        await fixture.SM.CreateSecretAsync(new CreateSecretRequest
        {
            Name = "myapp/to-delete",
            SecretString = "value"
        });

        await fixture.SM.DeleteSecretAsync(new DeleteSecretRequest
        {
            SecretId = "myapp/to-delete",
            ForceDeleteWithoutRecovery = true
        });

        await Assert.ThrowsAsync<ResourceNotFoundException>(async () =>
            await fixture.SM.GetSecretValueAsync(
                new GetSecretValueRequest { SecretId = "myapp/to-delete" }));
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test scenarios/06-SecretsManager.Basic/ -v normal
```

Expected: `3 passed`.

- [ ] **Step 5: Commit**

```bash
git add scenarios/06-SecretsManager.Basic/
git commit -m "feat: add scenario 06 - Secrets Manager Basic"
```

---

## Task 12: Python Lambda Handlers

**Files:** Create all handler files in `src/lambdas/`

- [ ] **Step 1: Create s3_processor**

```python
# src/lambdas/s3_processor/handler.py
import boto3
import json
import os

def lambda_handler(event, context):
    endpoint = os.environ.get("AWS_ENDPOINT_URL")
    dynamodb = boto3.resource("dynamodb", endpoint_url=endpoint)
    table = dynamodb.Table(os.environ["DYNAMODB_TABLE"])

    for record in event.get("Records", []):
        bucket = record["s3"]["bucket"]["name"]
        key = record["s3"]["object"]["key"]
        table.put_item(Item={"key": key, "bucket": bucket, "status": "processed"})

    return {"statusCode": 200}
```

- [ ] **Step 2: Create sqs_consumer**

```python
# src/lambdas/sqs_consumer/handler.py
import boto3
import json
import os

def lambda_handler(event, context):
    endpoint = os.environ.get("AWS_ENDPOINT_URL")
    dynamodb = boto3.resource("dynamodb", endpoint_url=endpoint)
    table = dynamodb.Table(os.environ["DYNAMODB_TABLE"])

    for record in event.get("Records", []):
        body = json.loads(record["body"])
        table.put_item(Item={"id": record["messageId"], "body": json.dumps(body)})

    return {"statusCode": 200}
```

- [ ] **Step 3: Create dynamodb_writer**

```python
# src/lambdas/dynamodb_writer/handler.py
import boto3
import json
import os

def lambda_handler(event, context):
    endpoint = os.environ.get("AWS_ENDPOINT_URL")
    dynamodb = boto3.resource("dynamodb", endpoint_url=endpoint)
    table = dynamodb.Table(os.environ["DYNAMODB_TABLE"])

    payload = event if isinstance(event, dict) else json.loads(event)
    table.put_item(Item={"id": payload["id"], "data": json.dumps(payload)})

    return {"statusCode": 200, "id": payload["id"]}
```

- [ ] **Step 4: Create fanout_processor**

```python
# src/lambdas/fanout_processor/handler.py
import boto3
import json
import os

def lambda_handler(event, context):
    endpoint = os.environ.get("AWS_ENDPOINT_URL")
    s3 = boto3.client("s3", endpoint_url=endpoint)
    bucket = os.environ["S3_BUCKET"]

    for record in event.get("Records", []):
        # SNS wraps message in "body" when delivered via SQS subscription
        outer = json.loads(record["body"])
        message = outer.get("Message", record["body"])
        key = f"results/{record['messageId']}.json"
        s3.put_object(Bucket=bucket, Key=key, Body=message)

    return {"statusCode": 200}
```

- [ ] **Step 5: Create eventbridge_handler**

```python
# src/lambdas/eventbridge_handler/handler.py
import boto3
import json
import os

def lambda_handler(event, context):
    endpoint = os.environ.get("AWS_ENDPOINT_URL")
    dynamodb = boto3.resource("dynamodb", endpoint_url=endpoint)
    table = dynamodb.Table(os.environ["DYNAMODB_TABLE"])

    table.put_item(Item={
        "id": context.aws_request_id,
        "source": event.get("source", "unknown"),
        "detail_type": event.get("detail-type", "unknown"),
        "detail": json.dumps(event.get("detail", {}))
    })

    return {"statusCode": 200}
```

- [ ] **Step 6: Create stepfunctions_task**

```python
# src/lambdas/stepfunctions_task/handler.py
import json

def lambda_handler(event, context):
    """
    Generic task Lambda for Step Functions.
    Receives the step input, appends a 'processed' flag, returns it.
    """
    result = dict(event)
    result["processed"] = True
    result["step"] = event.get("step", "unknown")
    return result
```

- [ ] **Step 7: Commit**

```bash
git add src/lambdas/
git commit -m "feat: add Python Lambda handlers for scenarios 07-15"
```

---

## Task 13: Scenario 07 — S3 Lambda Trigger

**Files:** Create `scenarios/07-S3.Lambda.Trigger/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "07-S3.Lambda.Trigger" -o scenarios/07-S3.Lambda.Trigger --framework net9.0
dotnet sln add scenarios/07-S3.Lambda.Trigger/07-S3.Lambda.Trigger.csproj
cd scenarios/07-S3.Lambda.Trigger
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.S3
dotnet add package AWSSDK.DynamoDBv2
dotnet add package AWSSDK.Lambda
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/07-S3.Lambda.Trigger/Fixture.cs
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Shared;

namespace Scenarios.S3.LambdaTrigger;

public class Fixture : LocalStackFixture
{
    public AmazonS3Client S3 { get; private set; } = null!;
    public AmazonDynamoDBClient DynamoDB { get; private set; } = null!;
    public const string BucketName = "uploads";
    public const string TableName = "processed-files";
    public const string FunctionName = "s3-processor";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        S3 = AwsClientFactory.S3();
        DynamoDB = AwsClientFactory.DynamoDB();
        var lambda = AwsClientFactory.Lambda();

        // Deploy Lambda with DynamoDB table name injected
        var deployer = new LambdaDeployer(lambda);
        await deployer.DeployAsync(FunctionName, "s3_processor",
            new Dictionary<string, string> { ["DYNAMODB_TABLE"] = TableName });

        // Get Lambda ARN
        var funcConfig = await lambda.GetFunctionAsync(
            new GetFunctionRequest { FunctionName = FunctionName });
        var lambdaArn = funcConfig.Configuration.FunctionArn;

        // Create S3 bucket
        await S3.PutBucketAsync(BucketName);

        // Configure S3 → Lambda notification
        await S3.PutBucketNotificationAsync(new PutBucketNotificationRequest
        {
            BucketName = BucketName,
            LambdaFunctionConfigurations = new List<LambdaFunctionConfiguration>
            {
                new()
                {
                    Id = "trigger-on-upload",
                    FunctionArn = lambdaArn,
                    Events = new List<EventType> { EventType.ObjectCreatedPut }
                }
            }
        });

        // Create DynamoDB table for results
        await DynamoDB.CreateTableAsync(new CreateTableRequest
        {
            TableName = TableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new() { AttributeName = "key", AttributeType = ScalarAttributeType.S }
            },
            KeySchema = new List<KeySchemaElement>
            {
                new() { AttributeName = "key", KeyType = KeyType.HASH }
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }

    public override async Task DisposeAsync()
    {
        S3.Dispose();
        DynamoDB.Dispose();
        await base.DisposeAsync();
    }
}
```

> **Note:** `LambdaDeployer.DeployAsync` needs an overload accepting env vars. Update `src/Shared/LambdaDeployer.cs` to accept `Dictionary<string, string>? extraEnv = null` and merge with `AWS_ENDPOINT_URL`.

- [ ] **Step 3: Update LambdaDeployer to accept extra env vars**

Edit `src/Shared/LambdaDeployer.cs`, change `DeployAsync` signature and body:

```csharp
public async Task DeployAsync(
    string functionName,
    string lambdaFolderName,
    Dictionary<string, string>? extraEnv = null)
{
    var sourcePath = ResolveLambdaPath(lambdaFolderName);
    var zipBytes = CreateZip(sourcePath);

    var envVars = new Dictionary<string, string>
    {
        ["AWS_ENDPOINT_URL"] = LocalStackFixture.Endpoint
    };
    if (extraEnv is not null)
        foreach (var kv in extraEnv) envVars[kv.Key] = kv.Value;

    await client.CreateFunctionAsync(new CreateFunctionRequest
    {
        FunctionName = functionName,
        Runtime = Runtime.Python312,
        Handler = "handler.lambda_handler",
        Role = FakeRole,
        Code = new FunctionCode { ZipFile = new MemoryStream(zipBytes) },
        Environment = new Amazon.Lambda.Model.Environment { Variables = envVars }
    });

    await WaitUntilActiveAsync(functionName);
}
```

- [ ] **Step 4: Write S3LambdaTriggerTests.cs**

```csharp
// scenarios/07-S3.Lambda.Trigger/S3LambdaTriggerTests.cs
using Amazon.DynamoDBv2.Model;
using Amazon.S3.Model;
using Shared;

namespace Scenarios.S3.LambdaTrigger;

public class S3LambdaTriggerTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task PutObject_ShouldTriggerLambda_AndPersistToDynamoDB()
    {
        await fixture.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Fixture.BucketName,
            Key = "report.pdf",
            ContentBody = "pdf-content"
        });

        await PollingHelper.WaitUntilAsync(async () =>
        {
            var resp = await fixture.DynamoDB.GetItemAsync(
                Fixture.TableName,
                new Dictionary<string, AttributeValue> { ["key"] = new() { S = "report.pdf" } });
            return resp.Item.ContainsKey("key");
        }, timeout: TimeSpan.FromSeconds(30));

        var item = await fixture.DynamoDB.GetItemAsync(
            Fixture.TableName,
            new Dictionary<string, AttributeValue> { ["key"] = new() { S = "report.pdf" } });

        Assert.Equal("uploads", item.Item["bucket"].S);
        Assert.Equal("processed", item.Item["status"].S);
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test scenarios/07-S3.Lambda.Trigger/ -v normal
```

Expected: `1 passed`.

- [ ] **Step 6: Commit**

```bash
git add scenarios/07-S3.Lambda.Trigger/ src/Shared/LambdaDeployer.cs
git commit -m "feat: add scenario 07 - S3 Lambda Trigger; update LambdaDeployer for extra env vars"
```

---

## Task 14: Scenario 08 — SQS Lambda Consumer

**Files:** Create `scenarios/08-SQS.Lambda.Consumer/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "08-SQS.Lambda.Consumer" -o scenarios/08-SQS.Lambda.Consumer --framework net9.0
dotnet sln add scenarios/08-SQS.Lambda.Consumer/08-SQS.Lambda.Consumer.csproj
cd scenarios/08-SQS.Lambda.Consumer
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.SQS
dotnet add package AWSSDK.DynamoDBv2
dotnet add package AWSSDK.Lambda
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/08-SQS.Lambda.Consumer/Fixture.cs
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.SQS;
using Shared;

namespace Scenarios.SQS.LambdaConsumer;

public class Fixture : LocalStackFixture
{
    public AmazonSQSClient SQS { get; private set; } = null!;
    public AmazonDynamoDBClient DynamoDB { get; private set; } = null!;
    public string QueueUrl { get; private set; } = null!;
    public string QueueArn { get; private set; } = null!;
    public const string TableName = "consumed-messages";
    public const string FunctionName = "sqs-consumer";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        SQS = AwsClientFactory.SQS();
        DynamoDB = AwsClientFactory.DynamoDB();
        var lambda = AwsClientFactory.Lambda();

        await new LambdaDeployer(lambda).DeployAsync(
            FunctionName, "sqs_consumer",
            new Dictionary<string, string> { ["DYNAMODB_TABLE"] = TableName });

        QueueUrl = (await SQS.CreateQueueAsync("consumer-queue")).QueueUrl;
        QueueArn = (await SQS.GetQueueAttributesAsync(
            QueueUrl, new List<string> { "QueueArn" })).Attributes["QueueArn"];

        var funcConfig = await lambda.GetFunctionAsync(
            new GetFunctionRequest { FunctionName = FunctionName });

        // Create event source mapping: SQS → Lambda
        await lambda.CreateEventSourceMappingAsync(new CreateEventSourceMappingRequest
        {
            FunctionName = FunctionName,
            EventSourceArn = QueueArn,
            BatchSize = 1,
            Enabled = true
        });

        await DynamoDB.CreateTableAsync(new CreateTableRequest
        {
            TableName = TableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new() { AttributeName = "id", AttributeType = ScalarAttributeType.S }
            },
            KeySchema = new List<KeySchemaElement>
            {
                new() { AttributeName = "id", KeyType = KeyType.HASH }
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }

    public override async Task DisposeAsync()
    {
        SQS.Dispose();
        DynamoDB.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write SqsLambdaConsumerTests.cs**

```csharp
// scenarios/08-SQS.Lambda.Consumer/SqsLambdaConsumerTests.cs
using Amazon.DynamoDBv2.Model;
using Shared;

namespace Scenarios.SQS.LambdaConsumer;

public class SqsLambdaConsumerTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task SendMessage_ShouldTriggerLambda_AndPersistToDynamoDB()
    {
        await fixture.SQS.SendMessageAsync(fixture.QueueUrl,
            """{"event": "order-placed", "orderId": "123"}""");

        await PollingHelper.WaitUntilAsync(async () =>
        {
            var scan = await fixture.DynamoDB.ScanAsync(
                new ScanRequest { TableName = Fixture.TableName });
            return scan.Items.Any(i =>
                i.ContainsKey("body") && i["body"].S.Contains("order-placed"));
        }, timeout: TimeSpan.FromSeconds(30));
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test scenarios/08-SQS.Lambda.Consumer/ -v normal
```

Expected: `1 passed`.

- [ ] **Step 5: Commit**

```bash
git add scenarios/08-SQS.Lambda.Consumer/
git commit -m "feat: add scenario 08 - SQS Lambda Consumer"
```

---

## Task 15: Scenario 09 — SNS SQS Fanout

**Files:** Create `scenarios/09-SNS.SQS.Fanout/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "09-SNS.SQS.Fanout" -o scenarios/09-SNS.SQS.Fanout --framework net9.0
dotnet sln add scenarios/09-SNS.SQS.Fanout/09-SNS.SQS.Fanout.csproj
cd scenarios/09-SNS.SQS.Fanout
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.SimpleNotificationService
dotnet add package AWSSDK.SQS
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/09-SNS.SQS.Fanout/Fixture.cs
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Shared;

namespace Scenarios.SNS.SQS.Fanout;

public class Fixture : LocalStackFixture
{
    public AmazonSimpleNotificationServiceClient SNS { get; private set; } = null!;
    public AmazonSQSClient SQS { get; private set; } = null!;
    public string TopicArn { get; private set; } = null!;
    public string Queue1Url { get; private set; } = null!;
    public string Queue2Url { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        SNS = AwsClientFactory.SNS();
        SQS = AwsClientFactory.SQS();

        TopicArn = (await SNS.CreateTopicAsync("fanout-topic")).TopicArn;
        Queue1Url = (await SQS.CreateQueueAsync("fanout-queue-1")).QueueUrl;
        Queue2Url = (await SQS.CreateQueueAsync("fanout-queue-2")).QueueUrl;

        foreach (var url in new[] { Queue1Url, Queue2Url })
        {
            var arn = (await SQS.GetQueueAttributesAsync(
                url, new List<string> { "QueueArn" })).Attributes["QueueArn"];
            await SNS.SubscribeAsync(new SubscribeRequest
            {
                TopicArn = TopicArn,
                Protocol = "sqs",
                Endpoint = arn
            });
        }
    }

    public override async Task DisposeAsync()
    {
        SNS.Dispose();
        SQS.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write SnsSqsFanoutTests.cs**

```csharp
// scenarios/09-SNS.SQS.Fanout/SnsSqsFanoutTests.cs
using Amazon.SQS.Model;
using Shared;

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
```

- [ ] **Step 4: Run tests**

```bash
dotnet test scenarios/09-SNS.SQS.Fanout/ -v normal
```

Expected: `1 passed`.

- [ ] **Step 5: Commit**

```bash
git add scenarios/09-SNS.SQS.Fanout/
git commit -m "feat: add scenario 09 - SNS SQS Fanout"
```

---

## Task 16: Scenario 10 — S3 SQS Notification

**Files:** Create `scenarios/10-S3.SQS.Notification/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "10-S3.SQS.Notification" -o scenarios/10-S3.SQS.Notification --framework net9.0
dotnet sln add scenarios/10-S3.SQS.Notification/10-S3.SQS.Notification.csproj
cd scenarios/10-S3.SQS.Notification
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.S3
dotnet add package AWSSDK.SQS
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/10-S3.SQS.Notification/Fixture.cs
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Shared;

namespace Scenarios.S3.SQS.Notification;

public class Fixture : LocalStackFixture
{
    public AmazonS3Client S3 { get; private set; } = null!;
    public AmazonSQSClient SQS { get; private set; } = null!;
    public string QueueUrl { get; private set; } = null!;
    public const string BucketName = "notify-bucket";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        S3 = AwsClientFactory.S3();
        SQS = AwsClientFactory.SQS();

        QueueUrl = (await SQS.CreateQueueAsync("s3-events-queue")).QueueUrl;
        var queueArn = (await SQS.GetQueueAttributesAsync(
            QueueUrl, new List<string> { "QueueArn" })).Attributes["QueueArn"];

        await S3.PutBucketAsync(BucketName);

        await S3.PutBucketNotificationAsync(new PutBucketNotificationRequest
        {
            BucketName = BucketName,
            QueueConfigurations = new List<QueueConfiguration>
            {
                new()
                {
                    Id = "notify-on-put",
                    Queue = queueArn,
                    Events = new List<EventType> { EventType.ObjectCreatedPut }
                }
            }
        });
    }

    public override async Task DisposeAsync()
    {
        S3.Dispose();
        SQS.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write S3SqsNotificationTests.cs**

```csharp
// scenarios/10-S3.SQS.Notification/S3SqsNotificationTests.cs
using Amazon.S3.Model;
using Amazon.SQS.Model;
using Shared;

namespace Scenarios.S3.SQS.Notification;

public class S3SqsNotificationTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task PutObject_ShouldSendNotificationToSQS()
    {
        await fixture.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Fixture.BucketName,
            Key = "upload.csv",
            ContentBody = "col1,col2"
        });

        await PollingHelper.WaitUntilAsync(async () =>
        {
            var resp = await fixture.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = fixture.QueueUrl,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 2
            });
            return resp.Messages.Any(m => m.Body.Contains("upload.csv"));
        }, timeout: TimeSpan.FromSeconds(20));
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test scenarios/10-S3.SQS.Notification/ -v normal
```

Expected: `1 passed`.

- [ ] **Step 5: Commit**

```bash
git add scenarios/10-S3.SQS.Notification/
git commit -m "feat: add scenario 10 - S3 SQS Notification"
```

---

## Task 17: Scenario 11 — DynamoDB Lambda

**Files:** Create `scenarios/11-DynamoDB.Lambda/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "11-DynamoDB.Lambda" -o scenarios/11-DynamoDB.Lambda --framework net9.0
dotnet sln add scenarios/11-DynamoDB.Lambda/11-DynamoDB.Lambda.csproj
cd scenarios/11-DynamoDB.Lambda
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.DynamoDBv2
dotnet add package AWSSDK.Lambda
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/11-DynamoDB.Lambda/Fixture.cs
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Shared;

namespace Scenarios.DynamoDB.Lambda;

public class Fixture : LocalStackFixture
{
    public AmazonDynamoDBClient DynamoDB { get; private set; } = null!;
    public AmazonLambdaClient Lambda { get; private set; } = null!;
    public const string SourceTable = "events";
    public const string ResultTable = "processed-events";
    public const string FunctionName = "dynamodb-writer";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        DynamoDB = AwsClientFactory.DynamoDB();
        Lambda = AwsClientFactory.Lambda();

        await new LambdaDeployer(Lambda).DeployAsync(
            FunctionName, "dynamodb_writer",
            new Dictionary<string, string> { ["DYNAMODB_TABLE"] = ResultTable });

        // Source table with DynamoDB Streams enabled
        await DynamoDB.CreateTableAsync(new CreateTableRequest
        {
            TableName = SourceTable,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new() { AttributeName = "id", AttributeType = ScalarAttributeType.S }
            },
            KeySchema = new List<KeySchemaElement>
            {
                new() { AttributeName = "id", KeyType = KeyType.HASH }
            },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            StreamSpecification = new StreamSpecification
            {
                StreamEnabled = true,
                StreamViewType = StreamViewType.NEW_IMAGE
            }
        });

        // Result table
        await DynamoDB.CreateTableAsync(new CreateTableRequest
        {
            TableName = ResultTable,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new() { AttributeName = "id", AttributeType = ScalarAttributeType.S }
            },
            KeySchema = new List<KeySchemaElement>
            {
                new() { AttributeName = "id", KeyType = KeyType.HASH }
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }

    public override async Task DisposeAsync()
    {
        DynamoDB.Dispose();
        Lambda.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write DynamoDbLambdaTests.cs**

```csharp
// scenarios/11-DynamoDB.Lambda/DynamoDbLambdaTests.cs
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Model;
using System.Text;
using System.Text.Json;
using Shared;

namespace Scenarios.DynamoDB.Lambda;

public class DynamoDbLambdaTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task InvokeLambda_ShouldWriteToDynamoDB()
    {
        var payload = JsonSerializer.Serialize(new { id = "evt-001", type = "click" });

        await fixture.Lambda.InvokeAsync(new InvokeRequest
        {
            FunctionName = Fixture.FunctionName,
            Payload = payload
        });

        await PollingHelper.WaitUntilAsync(async () =>
        {
            var resp = await fixture.DynamoDB.GetItemAsync(
                Fixture.ResultTable,
                new Dictionary<string, AttributeValue> { ["id"] = new() { S = "evt-001" } });
            return resp.Item.ContainsKey("id");
        });

        var item = await fixture.DynamoDB.GetItemAsync(
            Fixture.ResultTable,
            new Dictionary<string, AttributeValue> { ["id"] = new() { S = "evt-001" } });

        Assert.Equal("evt-001", item.Item["id"].S);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test scenarios/11-DynamoDB.Lambda/ -v normal
```

Expected: `1 passed`.

- [ ] **Step 5: Commit**

```bash
git add scenarios/11-DynamoDB.Lambda/
git commit -m "feat: add scenario 11 - DynamoDB Lambda"
```

---

## Task 18: Scenario 12 — Pipeline S3 → SQS → Lambda → DynamoDB

**Files:** Create `scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "12-Pipeline.S3.SQS.Lambda.DynamoDB" -o "scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB" --framework net9.0
dotnet sln add "scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/12-Pipeline.S3.SQS.Lambda.DynamoDB.csproj"
cd "scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB"
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.S3
dotnet add package AWSSDK.SQS
dotnet add package AWSSDK.DynamoDBv2
dotnet add package AWSSDK.Lambda
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/Fixture.cs
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Shared;

namespace Scenarios.Pipeline.S3SqsLambdaDynamoDB;

public class Fixture : LocalStackFixture
{
    public AmazonS3Client S3 { get; private set; } = null!;
    public AmazonSQSClient SQS { get; private set; } = null!;
    public AmazonDynamoDBClient DynamoDB { get; private set; } = null!;
    public const string BucketName = "pipeline-uploads";
    public const string QueueName = "pipeline-queue";
    public const string TableName = "pipeline-results";
    public const string FunctionName = "sqs-consumer-pipeline";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        S3 = AwsClientFactory.S3();
        SQS = AwsClientFactory.SQS();
        DynamoDB = AwsClientFactory.DynamoDB();
        var lambda = AwsClientFactory.Lambda();

        await new LambdaDeployer(lambda).DeployAsync(
            FunctionName, "sqs_consumer",
            new Dictionary<string, string> { ["DYNAMODB_TABLE"] = TableName });

        var queueUrl = (await SQS.CreateQueueAsync(QueueName)).QueueUrl;
        var queueArn = (await SQS.GetQueueAttributesAsync(
            queueUrl, new List<string> { "QueueArn" })).Attributes["QueueArn"];

        // S3 → SQS notification
        await S3.PutBucketAsync(BucketName);
        await S3.PutBucketNotificationAsync(new PutBucketNotificationRequest
        {
            BucketName = BucketName,
            QueueConfigurations = new List<QueueConfiguration>
            {
                new()
                {
                    Id = "pipeline-trigger",
                    Queue = queueArn,
                    Events = new List<EventType> { EventType.ObjectCreatedPut }
                }
            }
        });

        // SQS → Lambda event source mapping
        await lambda.CreateEventSourceMappingAsync(new CreateEventSourceMappingRequest
        {
            FunctionName = FunctionName,
            EventSourceArn = queueArn,
            BatchSize = 1,
            Enabled = true
        });

        await DynamoDB.CreateTableAsync(new CreateTableRequest
        {
            TableName = TableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new() { AttributeName = "id", AttributeType = ScalarAttributeType.S }
            },
            KeySchema = new List<KeySchemaElement>
            {
                new() { AttributeName = "id", KeyType = KeyType.HASH }
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }

    public override async Task DisposeAsync()
    {
        S3.Dispose();
        SQS.Dispose();
        DynamoDB.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write FileProcessingPipelineTests.cs**

```csharp
// scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/FileProcessingPipelineTests.cs
using Amazon.DynamoDBv2.Model;
using Amazon.S3.Model;
using Shared;

namespace Scenarios.Pipeline.S3SqsLambdaDynamoDB;

public class FileProcessingPipelineTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task UploadToS3_ShouldFlowThroughSQS_ToLambda_IntoDynamoDB()
    {
        await fixture.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Fixture.BucketName,
            Key = "invoice-001.pdf",
            ContentBody = "invoice data"
        });

        // Poll DynamoDB: the full pipeline (S3→SQS→Lambda→DynamoDB) is async
        await PollingHelper.WaitUntilAsync(async () =>
        {
            var scan = await fixture.DynamoDB.ScanAsync(
                new ScanRequest { TableName = Fixture.TableName });
            return scan.Items.Any(i =>
                i.ContainsKey("body") && i["body"].S.Contains("invoice-001.pdf"));
        }, timeout: TimeSpan.FromSeconds(45));
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test "scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/" -v normal
```

Expected: `1 passed`.

- [ ] **Step 5: Commit**

```bash
git add "scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/"
git commit -m "feat: add scenario 12 - full file processing pipeline"
```

---

## Task 19: Scenario 13 — Pipeline SNS → SQS → Lambda → S3

**Files:** Create `scenarios/13-Pipeline.SNS.SQS.Lambda.S3/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "13-Pipeline.SNS.SQS.Lambda.S3" -o "scenarios/13-Pipeline.SNS.SQS.Lambda.S3" --framework net9.0
dotnet sln add "scenarios/13-Pipeline.SNS.SQS.Lambda.S3/13-Pipeline.SNS.SQS.Lambda.S3.csproj"
cd "scenarios/13-Pipeline.SNS.SQS.Lambda.S3"
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.SimpleNotificationService
dotnet add package AWSSDK.SQS
dotnet add package AWSSDK.S3
dotnet add package AWSSDK.Lambda
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/13-Pipeline.SNS.SQS.Lambda.S3/Fixture.cs
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Shared;

namespace Scenarios.Pipeline.SnsSqsLambdaS3;

public class Fixture : LocalStackFixture
{
    public AmazonSimpleNotificationServiceClient SNS { get; private set; } = null!;
    public AmazonSQSClient SQS { get; private set; } = null!;
    public AmazonS3Client S3 { get; private set; } = null!;
    public string TopicArn { get; private set; } = null!;
    public const string BucketName = "fanout-results";
    public const string FunctionName = "fanout-processor";
    private const string QueueName = "fanout-queue";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        SNS = AwsClientFactory.SNS();
        SQS = AwsClientFactory.SQS();
        S3 = AwsClientFactory.S3();
        var lambda = AwsClientFactory.Lambda();

        await S3.PutBucketAsync(BucketName);

        await new LambdaDeployer(lambda).DeployAsync(
            FunctionName, "fanout_processor",
            new Dictionary<string, string> { ["S3_BUCKET"] = BucketName });

        TopicArn = (await SNS.CreateTopicAsync("fanout-topic-pipeline")).TopicArn;
        var queueUrl = (await SQS.CreateQueueAsync(QueueName)).QueueUrl;
        var queueArn = (await SQS.GetQueueAttributesAsync(
            queueUrl, new List<string> { "QueueArn" })).Attributes["QueueArn"];

        await SNS.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = TopicArn,
            Protocol = "sqs",
            Endpoint = queueArn
        });

        await lambda.CreateEventSourceMappingAsync(new CreateEventSourceMappingRequest
        {
            FunctionName = FunctionName,
            EventSourceArn = queueArn,
            BatchSize = 1,
            Enabled = true
        });
    }

    public override async Task DisposeAsync()
    {
        SNS.Dispose();
        SQS.Dispose();
        S3.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write EventFanoutPipelineTests.cs**

```csharp
// scenarios/13-Pipeline.SNS.SQS.Lambda.S3/EventFanoutPipelineTests.cs
using Amazon.S3.Model;
using Shared;

namespace Scenarios.Pipeline.SnsSqsLambdaS3;

public class EventFanoutPipelineTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task PublishToSNS_ShouldFlowToLambda_AndWriteResultToS3()
    {
        await fixture.SNS.PublishAsync(fixture.TopicArn,
            """{"type": "user-signup", "userId": "u-42"}""");

        await PollingHelper.WaitUntilAsync(async () =>
        {
            var resp = await fixture.S3.ListObjectsV2Async(
                new ListObjectsV2Request { BucketName = Fixture.BucketName, Prefix = "results/" });
            return resp.S3Objects.Any();
        }, timeout: TimeSpan.FromSeconds(45));

        var objects = await fixture.S3.ListObjectsV2Async(
            new ListObjectsV2Request { BucketName = Fixture.BucketName, Prefix = "results/" });
        Assert.NotEmpty(objects.S3Objects);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test "scenarios/13-Pipeline.SNS.SQS.Lambda.S3/" -v normal
```

Expected: `1 passed`.

- [ ] **Step 5: Commit**

```bash
git add "scenarios/13-Pipeline.SNS.SQS.Lambda.S3/"
git commit -m "feat: add scenario 13 - SNS SQS Lambda S3 fanout pipeline"
```

---

## Task 20: Scenario 14 — EventBridge Lambda

**Files:** Create `scenarios/14-EventBridge.Lambda/`

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "14-EventBridge.Lambda" -o scenarios/14-EventBridge.Lambda --framework net9.0
dotnet sln add scenarios/14-EventBridge.Lambda/14-EventBridge.Lambda.csproj
cd scenarios/14-EventBridge.Lambda
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.EventBridge
dotnet add package AWSSDK.DynamoDBv2
dotnet add package AWSSDK.Lambda
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/14-EventBridge.Lambda/Fixture.cs
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Shared;

namespace Scenarios.EventBridge.Lambda;

public class Fixture : LocalStackFixture
{
    public AmazonEventBridgeClient EventBridge { get; private set; } = null!;
    public AmazonDynamoDBClient DynamoDB { get; private set; } = null!;
    public const string TableName = "eb-events";
    public const string FunctionName = "eventbridge-handler";
    public const string BusName = "custom-bus";
    public const string RuleName = "order-events-rule";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        EventBridge = AwsClientFactory.EventBridge();
        DynamoDB = AwsClientFactory.DynamoDB();
        var lambda = AwsClientFactory.Lambda();

        await new LambdaDeployer(lambda).DeployAsync(
            FunctionName, "eventbridge_handler",
            new Dictionary<string, string> { ["DYNAMODB_TABLE"] = TableName });

        var funcConfig = await lambda.GetFunctionAsync(
            new GetFunctionRequest { FunctionName = FunctionName });
        var lambdaArn = funcConfig.Configuration.FunctionArn;

        // Create custom event bus
        await EventBridge.CreateEventBusAsync(new CreateEventBusRequest { Name = BusName });

        // Create rule matching source "myapp"
        await EventBridge.PutRuleAsync(new PutRuleRequest
        {
            Name = RuleName,
            EventBusName = BusName,
            EventPattern = """{"source": ["myapp"]}""",
            State = RuleState.ENABLED
        });

        // Add Lambda as target
        await EventBridge.PutTargetsAsync(new PutTargetsRequest
        {
            Rule = RuleName,
            EventBusName = BusName,
            Targets = new List<Target>
            {
                new() { Id = "lambda-target", Arn = lambdaArn }
            }
        });

        await DynamoDB.CreateTableAsync(new CreateTableRequest
        {
            TableName = TableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new() { AttributeName = "id", AttributeType = ScalarAttributeType.S }
            },
            KeySchema = new List<KeySchemaElement>
            {
                new() { AttributeName = "id", KeyType = KeyType.HASH }
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }

    public override async Task DisposeAsync()
    {
        EventBridge.Dispose();
        DynamoDB.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write EventBridgeLambdaTests.cs**

```csharp
// scenarios/14-EventBridge.Lambda/EventBridgeLambdaTests.cs
using Amazon.DynamoDBv2.Model;
using Amazon.EventBridge.Model;
using Shared;

namespace Scenarios.EventBridge.Lambda;

public class EventBridgeLambdaTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task PutEvent_ShouldTriggerLambda_AndPersistToDynamoDB()
    {
        await fixture.EventBridge.PutEventsAsync(new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry>
            {
                new()
                {
                    EventBusName = Fixture.BusName,
                    Source = "myapp",
                    DetailType = "OrderPlaced",
                    Detail = """{"orderId": "ord-99", "amount": 150}"""
                }
            }
        });

        await PollingHelper.WaitUntilAsync(async () =>
        {
            var scan = await fixture.DynamoDB.ScanAsync(
                new ScanRequest { TableName = Fixture.TableName });
            return scan.Items.Any(i =>
                i.ContainsKey("detail_type") && i["detail_type"].S == "OrderPlaced");
        }, timeout: TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task PutEvent_WithNonMatchingSource_ShouldNotTriggerLambda()
    {
        await fixture.EventBridge.PutEventsAsync(new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry>
            {
                new()
                {
                    EventBusName = Fixture.BusName,
                    Source = "other-app",   // does not match rule pattern
                    DetailType = "SomeEvent",
                    Detail = """{"ignored": true}"""
                }
            }
        });

        // Wait briefly to confirm nothing was written
        await Task.Delay(3000);
        var scan = await fixture.DynamoDB.ScanAsync(
            new ScanRequest { TableName = Fixture.TableName });
        Assert.DoesNotContain(scan.Items,
            i => i.ContainsKey("source") && i["source"].S == "other-app");
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test scenarios/14-EventBridge.Lambda/ -v normal
```

Expected: `2 passed`.

- [ ] **Step 5: Commit**

```bash
git add scenarios/14-EventBridge.Lambda/
git commit -m "feat: add scenario 14 - EventBridge Lambda"
```

---

## Task 21: Scenario 15 — Step Functions Orchestration

**Files:** Create `scenarios/15-StepFunctions.Orchestration/`

> **Note:** Step Functions may require LocalStack Pro in some configurations. If `CreateStateMachine` returns an error about the service not being available, skip this scenario and note it in the README.

- [ ] **Step 1: Create project**

```bash
dotnet new xunit -n "15-StepFunctions.Orchestration" -o scenarios/15-StepFunctions.Orchestration --framework net9.0
dotnet sln add scenarios/15-StepFunctions.Orchestration/15-StepFunctions.Orchestration.csproj
cd scenarios/15-StepFunctions.Orchestration
dotnet add reference ../../src/Shared/Shared.csproj
dotnet add package AWSSDK.StepFunctions
dotnet add package AWSSDK.DynamoDBv2
dotnet add package AWSSDK.Lambda
rm UnitTest1.cs
cd ../..
```

- [ ] **Step 2: Write Fixture.cs**

```csharp
// scenarios/15-StepFunctions.Orchestration/Fixture.cs
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Shared;

namespace Scenarios.StepFunctions.Orchestration;

public class Fixture : LocalStackFixture
{
    public AmazonStepFunctionsClient SF { get; private set; } = null!;
    public AmazonDynamoDBClient DynamoDB { get; private set; } = null!;
    public string StateMachineArn { get; private set; } = null!;
    public const string TableName = "sf-results";
    public const string FunctionName = "stepfunctions-task";
    private const string FakeRole = "arn:aws:iam::000000000000:role/local-role";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        SF = AwsClientFactory.StepFunctions();
        DynamoDB = AwsClientFactory.DynamoDB();
        var lambda = AwsClientFactory.Lambda();

        await new LambdaDeployer(lambda).DeployAsync(FunctionName, "stepfunctions_task");

        var funcConfig = await lambda.GetFunctionAsync(
            new GetFunctionRequest { FunctionName = FunctionName });
        var lambdaArn = funcConfig.Configuration.FunctionArn;

        // ASL definition: Task → Choice → (success path or fail path)
        var asl = $$"""
        {
          "Comment": "Example Step Functions workflow",
          "StartAt": "ProcessStep",
          "States": {
            "ProcessStep": {
              "Type": "Task",
              "Resource": "{{lambdaArn}}",
              "Parameters": { "step": "process", "id.$": "$.id" },
              "ResultPath": "$.result",
              "Next": "CheckResult"
            },
            "CheckResult": {
              "Type": "Choice",
              "Choices": [
                {
                  "Variable": "$.result.processed",
                  "BooleanEquals": true,
                  "Next": "SuccessState"
                }
              ],
              "Default": "FailState"
            },
            "SuccessState": {
              "Type": "Succeed"
            },
            "FailState": {
              "Type": "Fail",
              "Error": "ProcessingFailed"
            }
          }
        }
        """;

        var sm = await SF.CreateStateMachineAsync(new CreateStateMachineRequest
        {
            Name = "example-workflow",
            Definition = asl,
            RoleArn = FakeRole,
            Type = StateMachineType.STANDARD
        });
        StateMachineArn = sm.StateMachineArn;

        await DynamoDB.CreateTableAsync(new CreateTableRequest
        {
            TableName = TableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new() { AttributeName = "id", AttributeType = ScalarAttributeType.S }
            },
            KeySchema = new List<KeySchemaElement>
            {
                new() { AttributeName = "id", KeyType = KeyType.HASH }
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }

    public override async Task DisposeAsync()
    {
        SF.Dispose();
        DynamoDB.Dispose();
        await base.DisposeAsync();
    }
}
```

- [ ] **Step 3: Write StepFunctionsTests.cs**

```csharp
// scenarios/15-StepFunctions.Orchestration/StepFunctionsTests.cs
using Amazon.StepFunctions.Model;
using System.Text.Json;
using Shared;

namespace Scenarios.StepFunctions.Orchestration;

public class StepFunctionsTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task StartExecution_ShouldCompleteSuccessfully()
    {
        var input = JsonSerializer.Serialize(new { id = "exec-001" });

        var exec = await fixture.SF.StartExecutionAsync(new StartExecutionRequest
        {
            StateMachineArn = fixture.StateMachineArn,
            Name = "test-execution-001",
            Input = input
        });

        await PollingHelper.WaitUntilAsync(async () =>
        {
            var desc = await fixture.SF.DescribeExecutionAsync(
                new DescribeExecutionRequest { ExecutionArn = exec.ExecutionArn });
            return desc.Status == ExecutionStatus.SUCCEEDED ||
                   desc.Status == ExecutionStatus.FAILED;
        }, timeout: TimeSpan.FromSeconds(60));

        var final = await fixture.SF.DescribeExecutionAsync(
            new DescribeExecutionRequest { ExecutionArn = exec.ExecutionArn });

        Assert.Equal(ExecutionStatus.SUCCEEDED, final.Status);
    }

    [Fact]
    public async Task ListExecutions_ShouldIncludeStartedExecution()
    {
        var exec = await fixture.SF.StartExecutionAsync(new StartExecutionRequest
        {
            StateMachineArn = fixture.StateMachineArn,
            Name = "test-execution-list",
            Input = """{"id": "exec-list"}"""
        });

        var list = await fixture.SF.ListExecutionsAsync(new ListExecutionsRequest
        {
            StateMachineArn = fixture.StateMachineArn
        });

        Assert.Contains(list.Executions, e => e.ExecutionArn == exec.ExecutionArn);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test scenarios/15-StepFunctions.Orchestration/ -v normal
```

Expected: `2 passed`. If Step Functions is unavailable in LocalStack Community, skip with `[Fact(Skip = "Requires LocalStack Pro")]`.

- [ ] **Step 5: Commit**

```bash
git add scenarios/15-StepFunctions.Orchestration/
git commit -m "feat: add scenario 15 - Step Functions Orchestration"
```

---

## Final Verification

- [ ] **Run full suite**

```bash
dotnet test aspire-aws.sln -v normal
```

Expected: All tests across all 15 scenario projects pass. LocalStack starts and stops automatically per project.

- [ ] **Spot check: Level 1**

```bash
dotnet test scenarios/01-S3.Basic/ scenarios/02-SQS.Basic/ scenarios/03-DynamoDB.Basic/
```

Expected: All pass without needing Docker directly.

- [ ] **Spot check: Level 3 pipeline**

```bash
dotnet test "scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/"
```

Expected: Test passes (may take up to 45s for the full async pipeline to complete).

---

## Self-Review Notes

- All 15 scenarios covered by Tasks 6–21.
- `LambdaDeployer.DeployAsync` updated in Task 13 to accept `extraEnv` — Tasks 7–21 that call it use this overload.
- `PollingHelper` used in all Lambda-triggered tests (Tasks 13–21) — no fixed `Task.Delay`.
- `ForcePathStyle = true` set in `AwsClientFactory.S3()` — required for LocalStack.
- Step Functions risk documented inline with skip guidance.
- Python handlers all read `AWS_ENDPOINT_URL` from env — `LambdaDeployer` injects it.
