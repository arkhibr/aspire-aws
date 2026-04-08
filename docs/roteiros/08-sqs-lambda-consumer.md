# 08 - SQS Lambda Consumer

## Tecnologias deste cenario

- [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html): fila onde a mensagem entra.
- [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html): consumidora da fila.
- [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html): banco onde o resultado e salvo.
- [LocalStack](https://docs.localstack.cloud/): ambiente local que emula todos esses servicos.

## Conceitos base deste cenario

- `Event source mapping`: configuracao que liga uma fila SQS a uma Lambda.
- `Consumer`: componente que le a fila.
- `Assincrono`: o envio da mensagem e o processamento nao acontecem no mesmo instante.
- `Polling`: tecnica de consultar repetidamente ate o resultado aparecer.

## O que este cenario ensina

Aqui a ideia eh:

`mensagem na fila -> [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html) consome -> [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html) recebe o resultado`

## Conceitos em portugues simples

- `Event source mapping`: ligacao entre uma fila e uma Lambda.
- `Consumer`: quem le a fila.
- `Assincrono`: o teste envia a mensagem, mas o processamento acontece em outro momento.

## Como o cenario esta montado

O `Fixture`:

1. cria a tabela `consumed-messages`
2. publica a Lambda `sqs-consumer`
3. cria a fila `consumer-queue`
4. cria o `event source mapping`

Arquivo: [Fixture.cs](../../scenarios/08-SQS.Lambda.Consumer/Fixture.cs)

```csharp
var mapping = await lambda.CreateEventSourceMappingAsync(new CreateEventSourceMappingRequest
{
    FunctionName = FunctionName,
    EventSourceArn = QueueArn,
    BatchSize = 1,
    Enabled = true
});
```

## O que a Lambda faz

Arquivo: [handler.py](../../src/lambdas/sqs_consumer/handler.py)

```python
for record in event.get("Records", []):
    body = json.loads(record["body"])
    table.put_item(Item={"id": record["messageId"], "body": json.dumps(body)})
```

Ela le a mensagem do [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html) e grava o conteudo em [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html).

## O que o teste valida

Arquivo: [SqsLambdaConsumerTests.cs](../../scenarios/08-SQS.Lambda.Consumer/SqsLambdaConsumerTests.cs)

O teste envia:

```json
{"event":"order-placed","orderId":"123"}
```

Depois ele usa `PollingHelper.WaitUntilAsync` para esperar ate que o registro apareca na tabela.

## Passo a passo para rodar

1. Abra o Docker.
2. Rode:

```bash
dotnet test scenarios/08-SQS.Lambda.Consumer/
```

3. Aguarde o mapeamento da fila ser habilitado.
4. Interprete o resultado:
   - em alguns ambientes o fluxo executa
   - em `macOS arm64`, a expectativa atual eh `SKIP`

## O que observar no resultado

Este cenario mostra uma ideia importante em sistemas de eventos:
quem envia a mensagem nao conversa diretamente com quem processa.
A fila fica no meio, desacoplando as duas partes.

## Arquivos principais

- [Fixture.cs](../../scenarios/08-SQS.Lambda.Consumer/Fixture.cs)
- [SqsLambdaConsumerTests.cs](../../scenarios/08-SQS.Lambda.Consumer/SqsLambdaConsumerTests.cs)
- [handler.py](../../src/lambdas/sqs_consumer/handler.py)
- [PollingHelper.cs](../../src/Shared/PollingHelper.cs)
