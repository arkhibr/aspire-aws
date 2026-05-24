using Amazon.ECS;
using Amazon.SQS;
using Shared;

namespace Scenarios.ECS.RunTask;

public class Fixture : LocalStackFixture
{
    public const string NomeCluster       = "pedidos-cluster";
    public const string FamiliaTask       = "pedido-processor";
    public const string NomeFilaPedidos   = "fila-pedidos";
    public const string NomeTabelaPedidos = "pedidos";

    public AmazonECSClient ECS { get; private set; } = null!;
    public AmazonSQSClient SQS { get; private set; } = null!;
    public string ClusterArn     { get; private set; } = null!;
    public string TaskDefArn     { get; private set; } = null!;
    public string UrlFilaPedidos { get; private set; } = null!;
    public string ConnectionString => LocalStackFixture.PostgresConnectionString;

    protected override Task InitializeScenarioAsync() => Task.CompletedTask;

    protected override Task DisposeScenarioAsync()
    {
        ECS?.Dispose();
        SQS?.Dispose();
        return Task.CompletedTask;
    }
}
