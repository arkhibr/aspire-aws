# 02 - SQS Basic

## Tecnologias deste cenario

- [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html): servico de filas da AWS.
- [LocalStack](https://docs.localstack.cloud/): emulador local dos servicos AWS.
- [AWS SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/welcome.html): biblioteca usada para criar filas, enviar mensagens e ler o resultado.

## Conceitos base deste cenario

- `Queue`: fila onde as mensagens esperam para ser consumidas.
- `Message`: cada item enviado para a fila.
- `DLQ`: fila de erro para mensagens que falharam repetidas vezes.
- `Fixture`: classe que prepara o cliente SQS antes dos testes.

## O que este cenario ensina

Este roteiro mostra o basico do [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html):

- criar fila
- enviar mensagem
- receber mensagem
- deletar mensagem
- configurar DLQ

## Conceitos em portugues simples

- [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html): servico de filas.
- `Queue`: fila onde mensagens ficam aguardando consumo.
- `Message`: item enviado para a fila.
- `ReceiptHandle`: identificador temporario usado para deletar a mensagem recebida.
- `DLQ`: Dead Letter Queue. Fila para mensagens que falharam varias vezes.

## Como o cenario esta montado

Arquivo: [Fixture.cs](../../scenarios/02-SQS.Basic/Fixture.cs)

```csharp
protected override Task InitializeScenarioAsync()
{
    SQS = AwsClientFactory.SQS();
    return Task.CompletedTask;
}
```

## O que os testes validam

Arquivo: [SqsBasicTests.cs](../../scenarios/02-SQS.Basic/SqsBasicTests.cs)

- cria uma fila simples
- envia e recebe uma mensagem
- recebe e apaga uma mensagem
- define politica de redirecionamento para DLQ

Fragmento central:

```csharp
await fixture.SQS.SendMessageAsync(queueUrl, "hello sqs");

var response = await fixture.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
{
    QueueUrl = queueUrl,
    MaxNumberOfMessages = 1,
    WaitTimeSeconds = 5
});
```

## Passo a passo para rodar

1. Abra o Docker.
2. Entre na raiz do projeto.
3. Rode:

```bash
dotnet test scenarios/02-SQS.Basic/
```

4. Aguarde o [LocalStack](https://docs.localstack.cloud/) iniciar.
5. Confira `4 passed`.

## O que observar no resultado

Quando este cenario passa, voce confirma que:

- o SQS do LocalStack esta ativo
- envio e leitura de mensagens funcionam
- a remocao da mensagem por `ReceiptHandle` funciona
- a fila aceita configuracao de DLQ

## Arquivos principais

- [Fixture.cs](../../scenarios/02-SQS.Basic/Fixture.cs)
- [SqsBasicTests.cs](../../scenarios/02-SQS.Basic/SqsBasicTests.cs)
