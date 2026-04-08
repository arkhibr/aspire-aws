using Amazon;
using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.Lambda;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.SecretsManager;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.SQS;
using Amazon.StepFunctions;

namespace Shared;

public static class AwsClientFactory
{
    private static BasicAWSCredentials Credentials => new("test", "test");

    public static AmazonS3Client S3(string endpoint = LocalStackFixture.Endpoint)
    {
        return new AmazonS3Client(Credentials, Configure(new AmazonS3Config
        {
            ForcePathStyle = true
        }, endpoint));
    }

    public static AmazonSQSClient SQS(string endpoint = LocalStackFixture.Endpoint)
    {
        return new AmazonSQSClient(Credentials, Configure(new AmazonSQSConfig(), endpoint));
    }

    public static AmazonSimpleNotificationServiceClient SNS(string endpoint = LocalStackFixture.Endpoint)
    {
        return new AmazonSimpleNotificationServiceClient(
            Credentials,
            Configure(new AmazonSimpleNotificationServiceConfig(), endpoint));
    }

    public static AmazonDynamoDBClient DynamoDB(string endpoint = LocalStackFixture.Endpoint)
    {
        return new AmazonDynamoDBClient(Credentials, Configure(new AmazonDynamoDBConfig(), endpoint));
    }

    public static AmazonLambdaClient Lambda(string endpoint = LocalStackFixture.Endpoint)
    {
        return new AmazonLambdaClient(Credentials, Configure(new AmazonLambdaConfig(), endpoint));
    }

    public static AmazonSimpleSystemsManagementClient SSM(string endpoint = LocalStackFixture.Endpoint)
    {
        return new AmazonSimpleSystemsManagementClient(
            Credentials,
            Configure(new AmazonSimpleSystemsManagementConfig(), endpoint));
    }

    public static AmazonSecretsManagerClient SecretsManager(string endpoint = LocalStackFixture.Endpoint)
    {
        return new AmazonSecretsManagerClient(
            Credentials,
            Configure(new AmazonSecretsManagerConfig(), endpoint));
    }

    public static AmazonEventBridgeClient EventBridge(string endpoint = LocalStackFixture.Endpoint)
    {
        return new AmazonEventBridgeClient(Credentials, Configure(new AmazonEventBridgeConfig(), endpoint));
    }

    public static AmazonStepFunctionsClient StepFunctions(string endpoint = LocalStackFixture.Endpoint)
    {
        return new AmazonStepFunctionsClient(Credentials, Configure(new AmazonStepFunctionsConfig(), endpoint));
    }

    private static TConfig Configure<TConfig>(TConfig config, string endpoint)
        where TConfig : ClientConfig
    {
        config.ServiceURL = endpoint;
        config.AuthenticationRegion = RegionEndpoint.USEast1.SystemName;
        config.UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        return config;
    }
}
