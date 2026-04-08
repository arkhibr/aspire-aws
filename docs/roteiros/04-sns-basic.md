# 04 - SNS Basic

## Tecnologias deste cenario

- [Amazon SNS](https://docs.aws.amazon.com/sns/latest/dg/welcome.html): servico de publicacao e distribuicao de mensagens.
- [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html): fila usada como destino da mensagem.
- [LocalStack](https://docs.localstack.cloud/): emulador local dos servicos AWS usados no teste.

## Conceitos base deste cenario

- `Topic`: canal do SNS para publicar mensagens.
- `Subscription`: ligacao entre o topico e um destino, como uma fila.
- `Queue policy`: permissao que autoriza um servico a escrever em uma fila SQS.
- `Fanout`: padrao onde uma publicacao pode ser entregue a varios destinos.

## O que este cenario ensina

Este roteiro mostra como um topico do [Amazon SNS](https://docs.aws.amazon.com/sns/latest/dg/welcome.html) entrega mensagens para uma fila do [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html).

## Conceitos em portugues simples

- [Amazon SNS](https://docs.aws.amazon.com/sns/latest/dg/welcome.html): servico de publicacao.
- `Topic`: canal onde voce publica eventos.
- `Subscription`: ligacao entre o topico e quem recebe a mensagem.
- `Fanout`: enviar a mesma mensagem para varios destinos.
- `Queue policy`: permissao que autoriza o SNS a escrever na fila SQS.

## Como o cenario esta montado

O `Fixture` cria dois clientes: um para [Amazon SNS](https://docs.aws.amazon.com/sns/latest/dg/welcome.html) e outro para [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html).

Arquivo: [Fixture.cs](../../scenarios/04-SNS.Basic/Fixture.cs)

```csharp
protected override Task InitializeScenarioAsync()
{
    SNS = AwsClientFactory.SNS();
    SQS = AwsClientFactory.SQS();
    return Task.CompletedTask;
}
```

O teste principal faz quatro passos:

1. cria um topico
2. cria uma fila
3. coloca uma politica na fila
4. inscreve a fila no topico

Trecho importante:

```csharp
await fixture.SNS.SubscribeAsync(new SubscribeRequest
{
    TopicArn = topicArn,
    Protocol = "sqs",
    Endpoint = queueArn
});

await fixture.SNS.PublishAsync(topicArn, "hello from SNS");
```

## O que os testes validam

Arquivo: [SnsBasicTests.cs](../../scenarios/04-SNS.Basic/SnsBasicTests.cs)

- `CreateTopic_ShouldReturnArn`
- `Publish_WithSqsSubscription_ShouldDeliverMessage`
- `ListTopics_ShouldIncludeCreatedTopic`

## Passo a passo para rodar

1. Abra o Docker.
2. Va para a raiz do repo.
3. Rode:

```bash
dotnet test scenarios/04-SNS.Basic/
```

4. Aguarde o [LocalStack](https://docs.localstack.cloud/).
5. Veja `3 passed`.

## O que observar no resultado

O ponto mais importante aqui nao eh so o `Publish`.
Sem a politica da fila, o SNS nao consegue entregar a mensagem ao SQS.
Este cenario ensina exatamente essa ligacao.

## Arquivos principais

- [Fixture.cs](../../scenarios/04-SNS.Basic/Fixture.cs)
- [SnsBasicTests.cs](../../scenarios/04-SNS.Basic/SnsBasicTests.cs)
