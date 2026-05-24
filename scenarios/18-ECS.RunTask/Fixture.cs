using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Npgsql;
using Shared;
using Task = System.Threading.Tasks.Task;

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

    // ECS control plane (CreateCluster, RegisterTaskDefinition) é Pro feature.
    // Quando não disponível, testes de plano de controle são pulados mas integração SQS/PG funciona.
    public bool EcsApiDisponivel { get; private set; }

    protected override async Task InitializeScenarioAsync()
    {
        ECS = AwsClientFactory.ECS();
        SQS = AwsClientFactory.SQS();

        // 1. Aguarda PostgreSQL real
        await PollingHelper.WaitUntilAsync(async () =>
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }, timeout: TimeSpan.FromSeconds(60), interval: TimeSpan.FromSeconds(1),
        failureMessage: "PostgreSQL não ficou disponível em 60s.");

        // 2. Aguarda o worker criar a tabela 'pedidos' (prova de que está rodando)
        await WaitForWorkerAsync();

        // 3. Cria fila SQS que o worker vai consumir
        UrlFilaPedidos = (await SQS.CreateQueueAsync(NomeFilaPedidos)).QueueUrl;

        // 4. Tenta criar cluster ECS — pode falhar no LocalStack Community (Pro feature)
        try
        {
            var cluster = await ECS.CreateClusterAsync(new CreateClusterRequest
            {
                ClusterName = NomeCluster
            });
            ClusterArn = cluster.Cluster.ClusterArn;

            // 5. Registra task definition com env vars do worker
            var taskDef = await ECS.RegisterTaskDefinitionAsync(new RegisterTaskDefinitionRequest
            {
                Family = FamiliaTask,
                ContainerDefinitions =
                [
                    new ContainerDefinition
                    {
                        Name      = "worker",
                        Image     = "ecs-worker:latest",
                        Essential = true,
                        Environment =
                        [
                            new Amazon.ECS.Model.KeyValuePair { Name = "FILA_PEDIDOS_URL",  Value = UrlFilaPedidos },
                            new Amazon.ECS.Model.KeyValuePair { Name = "DATABASE_URL",       Value = ConnectionString },
                            new Amazon.ECS.Model.KeyValuePair { Name = "AWS_ENDPOINT_URL",   Value = Endpoint }
                        ]
                    }
                ]
            });
            TaskDefArn = taskDef.TaskDefinition.TaskDefinitionArn;
            EcsApiDisponivel = true;
        }
        catch (AmazonECSException ex) when (ex.Message.Contains("not yet implemented") || ex.Message.Contains("pro feature"))
        {
            // LocalStack Community não suporta ECS control plane — integração SQS/PG ainda funciona
            EcsApiDisponivel = false;
        }
    }

    private async Task WaitForWorkerAsync()
    {
        // O worker cria a tabela 'pedidos' ao inicializar.
        // Quando a tabela existir, o worker está pronto para receber mensagens.
        await PollingHelper.WaitUntilAsync(async () =>
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(
                    "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'pedidos')",
                    conn);
                return (bool)(await cmd.ExecuteScalarAsync())!;
            }
            catch
            {
                return false;
            }
        }, timeout: TimeSpan.FromSeconds(120), interval: TimeSpan.FromSeconds(1),
        failureMessage: "Worker ECS não inicializou — tabela 'pedidos' não criada em 120s.");
    }

    protected override async Task DisposeScenarioAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"DELETE FROM {NomeTabelaPedidos}", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch { /* ignora se tabela não existir */ }

        try
        {
            if (UrlFilaPedidos is not null)
                await SQS.DeleteQueueAsync(new DeleteQueueRequest { QueueUrl = UrlFilaPedidos });
        }
        catch { }

        if (EcsApiDisponivel)
        {
            try
            {
                await ECS.DeleteClusterAsync(new DeleteClusterRequest { Cluster = NomeCluster });
            }
            catch { }
        }

        ECS?.Dispose();
        SQS?.Dispose();
    }
}
