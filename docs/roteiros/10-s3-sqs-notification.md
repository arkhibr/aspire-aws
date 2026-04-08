# 10 - S3 SQS Notification

## Tecnologias deste cenario

- [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html): servico que gera o evento de upload.
- [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html): fila que recebe a notificacao.
- [LocalStack](https://docs.localstack.cloud/): ambiente local que emula o bucket e a fila.

## Conceitos base deste cenario

- `Bucket notification`: configuracao do S3 que envia eventos para outro servico.
- `Queue target`: fila escolhida como destino da notificacao.
- `Policy`: permissao que libera um servico a publicar em outro.
- `Polling`: repeticao de consultas ate a mensagem aparecer.

## O que este cenario ensina

Este roteiro troca a [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html) por uma fila:

`upload no [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html) -> notificacao do bucket -> mensagem no [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html)`

## Conceitos em portugues simples

- `Bucket notification`: configuracao do S3 para avisar outro servico sobre eventos.
- `Queue target`: a fila SQS que vai receber esse aviso.

## Como o cenario esta montado

O `Fixture`:

1. cria a fila `s3-events-queue`
2. adiciona permissao para o S3 publicar nela
3. cria o bucket `notify-bucket`
4. configura a notificacao `ObjectCreated:Put`

Arquivo: [Fixture.cs](../../scenarios/10-S3.SQS.Notification/Fixture.cs)

```csharp
await S3.PutBucketNotificationAsync(new PutBucketNotificationRequest
{
    BucketName = BucketName,
    QueueConfigurations =
    [
        new QueueConfiguration
        {
            Id = "notify-on-put",
            Queue = queueArn,
            Events = [EventType.ObjectCreatedPut]
        }
    ]
});
```

## O que o teste valida

Arquivo: [S3SqsNotificationTests.cs](../../scenarios/10-S3.SQS.Notification/S3SqsNotificationTests.cs)

O teste faz upload de `upload.csv` e espera aparecer uma mensagem no SQS contendo esse nome.

## Passo a passo para rodar

1. Abra o Docker.
2. Rode:

```bash
dotnet test scenarios/10-S3.SQS.Notification/
```

3. Aguarde a notificacao ser entregue.
4. Confira `1 passed`.

## O que observar no resultado

Este cenario eh um degrau importante antes da [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html).
Ele prova que o S3 ja esta conseguindo emitir eventos; depois disso fica mais facil encaixar consumidores.

## Arquivos principais

- [Fixture.cs](../../scenarios/10-S3.SQS.Notification/Fixture.cs)
- [S3SqsNotificationTests.cs](../../scenarios/10-S3.SQS.Notification/S3SqsNotificationTests.cs)
