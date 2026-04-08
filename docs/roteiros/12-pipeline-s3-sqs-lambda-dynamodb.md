# 12 - Pipeline S3 SQS Lambda DynamoDB

## Tecnologias deste cenario

- [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html): servico onde o arquivo entra.
- [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html): fila que intermedeia o evento.
- [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html): processador do evento.
- [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html): banco de saida do pipeline.
- [LocalStack](https://docs.localstack.cloud/): ambiente local que emula o pipeline completo.

## Conceitos base deste cenario

- `Pipeline`: cadeia de etapas automaticas.
- `Desacoplamento`: ideia de separar quem gera o evento de quem processa o evento.
- `Event source mapping`: ligacao entre a fila SQS e a Lambda.
- `Polling`: repeticao de consultas ate o efeito final aparecer.

## O que este cenario ensina

Este eh o primeiro pipeline completo do projeto:

`upload no [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html) -> evento vira mensagem no [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html) -> [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html) consome -> [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html) recebe resultado`

## Conceitos em portugues simples

- `Pipeline`: cadeia de etapas que acontece automaticamente.
- [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html): origem do evento.
- [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html): fila intermediaria.
- [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html): processador.
- [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html): armazenamento do resultado.

## Como o cenario esta montado

O `Fixture` faz a orquestracao inteira:

1. cria a tabela `pipeline-results`
2. publica a Lambda `sqs-consumer-pipeline`
3. cria a fila `pipeline-queue`
4. libera o S3 para publicar na fila
5. cria o bucket `pipeline-uploads`
6. liga o bucket a fila
7. liga a fila a Lambda

Arquivo: [Fixture.cs](../../scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/Fixture.cs)

Trecho importante:

```csharp
await S3.PutBucketNotificationAsync(new PutBucketNotificationRequest
{
    BucketName = BucketName,
    QueueConfigurations =
    [
        new QueueConfiguration
        {
            Id = "pipeline-trigger",
            Queue = queueArn,
            Events = [EventType.ObjectCreatedPut]
        }
    ]
});

var mapping = await lambda.CreateEventSourceMappingAsync(new CreateEventSourceMappingRequest
{
    FunctionName = FunctionName,
    EventSourceArn = queueArn,
    BatchSize = 1,
    Enabled = true
});
```

## O que o teste valida

Arquivo: [FileProcessingPipelineTests.cs](../../scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/FileProcessingPipelineTests.cs)

O teste sobe `invoice-001.pdf` no bucket e espera um item aparecer na tabela com o nome do arquivo dentro do campo `body`.

## Passo a passo para rodar

1. Abra o Docker.
2. Rode:

```bash
dotnet test scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/
```

3. Aguarde a criacao do bucket, fila, tabela e Lambda.
4. Interprete o resultado:
   - em `macOS arm64`, este cenario pode aparecer como `SKIP`

## O que observar no resultado

Este roteiro eh otimo para quem quer entender arquitetura orientada a eventos.
O sistema fica desacoplado:

- o S3 so avisa
- o SQS segura o evento
- a Lambda processa quando puder
- o DynamoDB guarda o resultado

## Arquivos principais

- [Fixture.cs](../../scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/Fixture.cs)
- [FileProcessingPipelineTests.cs](../../scenarios/12-Pipeline.S3.SQS.Lambda.DynamoDB/FileProcessingPipelineTests.cs)
- [handler.py](../../src/lambdas/sqs_consumer/handler.py)
