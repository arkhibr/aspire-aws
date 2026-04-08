# 14 - EventBridge Lambda

## Tecnologias deste cenario

- [Amazon EventBridge](https://docs.aws.amazon.com/eventbridge/latest/userguide/eb-what-is.html): barramento de eventos com regras de roteamento.
- [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html): alvo da regra.
- [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html): armazenamento do evento processado.
- [LocalStack](https://docs.localstack.cloud/): ambiente local onde bus, regra, Lambda e tabela sao criados.

## Conceitos base deste cenario

- `Event bus`: barramento por onde os eventos entram.
- `Rule`: filtro que escolhe quais eventos seguem adiante.
- `Target`: destino final da regra.
- `Source`: campo do evento usado aqui para decidir se ele combina com a regra.

## O que este cenario ensina

Este roteiro mostra um barramento de eventos:

`evento publicado no [Amazon EventBridge](https://docs.aws.amazon.com/eventbridge/latest/userguide/eb-what-is.html) -> regra filtra -> [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html) executa -> [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html) guarda`

## Conceitos em portugues simples

- [Amazon EventBridge](https://docs.aws.amazon.com/eventbridge/latest/userguide/eb-what-is.html): servico para rotear eventos por regra.
- `Event bus`: barramento onde os eventos entram.
- `Rule`: filtro que decide quais eventos seguem adiante.
- `Target`: destino final da regra, aqui uma Lambda.

## Como o cenario esta montado

O `Fixture`:

1. cria a tabela `eb-events`
2. publica a Lambda `eventbridge-handler`
3. cria o bus `custom-bus`
4. cria a regra `order-events-rule` com filtro `source = myapp`
5. da permissao para o EventBridge chamar a Lambda
6. aponta a regra para a Lambda

Arquivo: [Fixture.cs](../../scenarios/14-EventBridge.Lambda/Fixture.cs)

Trecho importante:

```csharp
var rule = await EventBridge.PutRuleAsync(new PutRuleRequest
{
    Name = RuleName,
    EventBusName = BusName,
    EventPattern = """{"source":["myapp"]}""",
    State = RuleState.ENABLED
});
```

## O que a Lambda faz

Arquivo: [handler.py](../../src/lambdas/eventbridge_handler/handler.py)

```python
table.put_item(
    Item={
        "id": context.aws_request_id,
        "source": event.get("source", "unknown"),
        "detail_type": event.get("detail-type", "unknown"),
        "detail": json.dumps(event.get("detail", {})),
    }
)
```

Ela grava o evento recebido em DynamoDB.

## O que os testes validam

Arquivo: [EventBridgeLambdaTests.cs](../../scenarios/14-EventBridge.Lambda/EventBridgeLambdaTests.cs)

- `PutEvent_ShouldTriggerLambda_AndPersistToDynamoDb`: evento com `Source = myapp` deve ser processado.
- `PutEvent_WithNonMatchingSource_ShouldNotTriggerLambda`: evento com outra origem nao deve passar pela regra.

## Passo a passo para rodar

1. Abra o Docker.
2. Rode:

```bash
dotnet test scenarios/14-EventBridge.Lambda/
```

3. Aguarde o bus, a regra e a Lambda serem configurados.
4. Em `macOS arm64`, a expectativa atual eh `SKIP`.

## O que observar no resultado

Este cenario eh importante para aprender que nem todo evento vai para todos os destinos.
A regra do EventBridge filtra pelo campo `source`.

## Arquivos principais

- [Fixture.cs](../../scenarios/14-EventBridge.Lambda/Fixture.cs)
- [EventBridgeLambdaTests.cs](../../scenarios/14-EventBridge.Lambda/EventBridgeLambdaTests.cs)
- [handler.py](../../src/lambdas/eventbridge_handler/handler.py)
