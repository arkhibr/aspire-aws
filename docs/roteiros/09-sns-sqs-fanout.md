# 09 - SNS SQS Fanout

## Tecnologias deste cenario

- [Amazon SNS](https://docs.aws.amazon.com/sns/latest/dg/welcome.html): origem da publicacao.
- [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html): filas que recebem a copia da mensagem.
- [LocalStack](https://docs.localstack.cloud/): ambiente local onde o topico e as filas sao criados.

## Conceitos base deste cenario

- `Fanout`: padrao onde um evento e distribuido para varios consumidores.
- `Topic`: canal do SNS onde a mensagem e publicada.
- `Subscription`: ligacao entre o topico e cada fila.
- `Queue policy`: permissao que autoriza o SNS a entregar mensagens na fila.

## O que este cenario ensina

Este roteiro mostra o padrao `fanout`:

`uma publicacao no [Amazon SNS](https://docs.aws.amazon.com/sns/latest/dg/welcome.html) -> duas filas [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html) recebem a mesma mensagem`

## Conceitos em portugues simples

- `Fanout`: replicar um evento para varios consumidores.
- `Topic`: origem da mensagem.
- `Queues`: destinos independentes da mesma mensagem.

## Como o cenario esta montado

O `Fixture` cria:

- um topico SNS
- duas filas SQS
- uma politica em cada fila
- uma inscricao para cada fila

Arquivo: [Fixture.cs](../../scenarios/09-SNS.SQS.Fanout/Fixture.cs)

```csharp
foreach (var queueUrl in new[] { Queue1Url, Queue2Url })
{
    var queueArn = (await SQS.GetQueueAttributesAsync(queueUrl, ["QueueArn"])).Attributes["QueueArn"];

    await SQS.SetQueueAttributesAsync(new SetQueueAttributesRequest
    {
        QueueUrl = queueUrl,
        Attributes = new Dictionary<string, string>
        {
            ["Policy"] = BuildQueuePolicy(queueArn, TopicArn)
        }
    });

    await SNS.SubscribeAsync(new SubscribeRequest
    {
        TopicArn = TopicArn,
        Protocol = "sqs",
        Endpoint = queueArn
    });
}
```

## O que o teste valida

Arquivo: [SnsSqsFanoutTests.cs](../../scenarios/09-SNS.SQS.Fanout/SnsSqsFanoutTests.cs)

O teste publica `broadcast event` e confere que as duas filas recebem a mesma mensagem.

## Passo a passo para rodar

1. Abra o Docker.
2. Rode:

```bash
dotnet test scenarios/09-SNS.SQS.Fanout/
```

3. Aguarde a execucao.
4. Confira `1 passed`.

## O que observar no resultado

Este cenario eh importante porque mostra como um unico evento pode abastecer varios consumidores ao mesmo tempo.
Isso eh comum em notificacoes, integracoes e pipelines de auditoria.

## Arquivos principais

- [Fixture.cs](../../scenarios/09-SNS.SQS.Fanout/Fixture.cs)
- [SnsSqsFanoutTests.cs](../../scenarios/09-SNS.SQS.Fanout/SnsSqsFanoutTests.cs)
