# 07 - S3 Lambda Trigger

## Tecnologias deste cenario

- [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html): origem do upload.
- [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html): funcao executada automaticamente quando o evento acontece.
- [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html): destino onde o resultado e gravado.
- [LocalStack](https://docs.localstack.cloud/): ambiente local que emula esses servicos.

## Conceitos base deste cenario

- `Trigger`: evento que dispara outra acao sem chamada direta do teste.
- `Bucket notification`: configuracao do bucket S3 que envia um evento quando algo acontece.
- `Lambda permission`: permissao explicita para um servico, aqui o S3, invocar a Lambda.
- `Fixture`: classe que monta todo o ambiente antes de o teste subir o arquivo.

## O que este cenario ensina

Este roteiro mostra um fluxo muito comum:

`upload no [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html) -> evento no bucket -> [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html) -> escrita no [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html)`

## Conceitos em portugues simples

- `Trigger`: evento que dispara outra acao.
- [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html): funcao executada sob demanda.
- `Bucket notification`: configuracao do S3 que chama outro servico quando algo acontece.
- `DynamoDB`: usado aqui como destino do processamento.

## Como o cenario esta montado

O `Fixture` faz cinco coisas:

1. cria a tabela `processed-files`
2. publica a Lambda `s3-processor`
3. cria o bucket `uploads`
4. da permissao para o S3 invocar a Lambda
5. configura notificacao `ObjectCreated:Put`

Arquivo: [Fixture.cs](../../scenarios/07-S3.Lambda.Trigger/Fixture.cs)

```csharp
await lambda.AddPermissionAsync(new AddPermissionRequest
{
    Action = "lambda:InvokeFunction",
    FunctionName = FunctionName,
    Principal = "s3.amazonaws.com",
    StatementId = "allow-s3-invoke",
    SourceArn = $"arn:aws:s3:::{BucketName}"
});

await S3.PutBucketNotificationAsync(new PutBucketNotificationRequest
{
    BucketName = BucketName,
    LambdaFunctionConfigurations =
    [
        new LambdaFunctionConfiguration
        {
            Id = "trigger-on-upload",
            FunctionArn = function.Configuration.FunctionArn,
            Events = [EventType.ObjectCreatedPut]
        }
    ]
});
```

## O que a Lambda faz

Arquivo: [handler.py](../../src/lambdas/s3_processor/handler.py)

```python
for record in event.get("Records", []):
    bucket = record["s3"]["bucket"]["name"]
    key = record["s3"]["object"]["key"]
    table.put_item(Item={"key": key, "bucket": bucket, "status": "processed"})
```

Em portugues simples: a Lambda recebe o nome do bucket e o nome do arquivo e grava isso em uma tabela DynamoDB.

## O que o teste valida

Arquivo: [S3LambdaTriggerTests.cs](../../scenarios/07-S3.Lambda.Trigger/S3LambdaTriggerTests.cs)

O teste sobe um arquivo chamado `report.pdf`, espera o processamento assincrono e depois confirma:

- `bucket = uploads`
- `status = processed`

## Passo a passo para rodar

1. Abra o Docker.
2. Na raiz do repositorio, rode:

```bash
dotnet test scenarios/07-S3.Lambda.Trigger/
```

3. Aguarde a subida do [LocalStack](https://docs.localstack.cloud/) e da [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html).
4. Interprete o resultado:
   - em Linux/x64, a expectativa eh executar o fluxo
   - em `macOS arm64`, este teste pode aparecer como `SKIP`

## O que observar no resultado

Se o teste rodar de verdade e passar, voce validou um pipeline orientado a evento.
Se aparecer `SKIP` em `macOS arm64`, isso reflete uma limitacao conhecida do LocalStack com Lambda neste host.

## Arquivos principais

- [Fixture.cs](../../scenarios/07-S3.Lambda.Trigger/Fixture.cs)
- [S3LambdaTriggerTests.cs](../../scenarios/07-S3.Lambda.Trigger/S3LambdaTriggerTests.cs)
- [handler.py](../../src/lambdas/s3_processor/handler.py)
- [LambdaDeployer.cs](../../src/Shared/LambdaDeployer.cs)
