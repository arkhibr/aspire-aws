var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
{
    Args = args
});

var alvoAws = Environment.GetEnvironmentVariable("AWS_TARGET") ?? "localstack";

if (!string.Equals(alvoAws, "aws", StringComparison.OrdinalIgnoreCase))
{
    builder
        .AddContainer("localstack", "localstack/localstack", "3.8")
        .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
        .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
        .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
        .WithEnvironment("SERVICES", "s3,sqs,sns,dynamodb,lambda,ssm,secretsmanager,events,scheduler,stepfunctions,rds,ecs")
        .WithEnvironment("LAMBDA_REMOVE_CONTAINERS", "true")
        .WithEnvironment("LAMBDA_RUNTIME_ENVIRONMENT_TIMEOUT", "120")
        .WithEnvironment("DOCKER_HOST", "unix:///var/run/docker.sock")
        .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock")
        .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "gateway", isProxied: false);

    builder
        .AddContainer("postgres-rds", "postgres", "16.9")
        .WithEnvironment("POSTGRES_USER", "test")
        .WithEnvironment("POSTGRES_PASSWORD", "test")
        .WithEnvironment("POSTGRES_DB", "testdb")
        .WithHttpEndpoint(port: 5433, targetPort: 5432, name: "tcp", isProxied: false);

    builder
        .AddDockerfile("ecs-worker", "../tasks/pedido_processor")
        .WithEnvironment("AWS_ENDPOINT_URL", "http://host.docker.internal:4566")
        .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
        .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
        .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
        .WithEnvironment("DATABASE_URL", "postgresql://test:test@host.docker.internal:5433/testdb")
        .WithEnvironment("FILA_PEDIDOS_URL",
            "http://host.docker.internal:4566/000000000000/fila-pedidos");
}

builder.Build().Run();
