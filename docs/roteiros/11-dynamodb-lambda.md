# 11 - DynamoDB Lambda

## Tecnologias deste cenario

- [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html): funcao executada a partir de uma chamada direta do teste.
- [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html): banco usado para guardar o resultado.
- [LocalStack](https://docs.localstack.cloud/): ambiente local onde a Lambda e as tabelas sao criadas.

## Conceitos base deste cenario

- `Invoke`: chamada direta de uma Lambda.
- `Payload`: JSON enviado para a Lambda.
- `Result table`: tabela onde a Lambda grava sua resposta.
- `StreamEnabled`: configuracao que liga um fluxo de eventos na tabela DynamoDB. Neste cenario ele existe, mas o teste atual nao depende dele.

## O que este cenario ensina

Este roteiro mostra uma [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html) sendo invocada diretamente e gravando em [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html).

## Conceitos em portugues simples

- `Invoke`: chamada direta de uma Lambda pelo codigo.
- `Payload`: dados enviados para a Lambda.
- `Result table`: tabela de destino.

## Observacao importante

O `Fixture` tambem cria uma tabela de origem com `StreamEnabled = true`, mas o teste atual nao usa o stream.
O teste valida a parte mais simples: invocar a Lambda diretamente.

## Como o cenario esta montado

O `Fixture`:

1. cria o cliente DynamoDB
2. cria o cliente Lambda
3. publica a Lambda `dynamodb-writer`
4. cria as tabelas `events` e `processed-events`

Arquivo: [Fixture.cs](../../scenarios/11-DynamoDB.Lambda/Fixture.cs)

```csharp
await new LambdaDeployer(Lambda).DeployAsync(
    FunctionName,
    "dynamodb_writer",
    new Dictionary<string, string> { ["DYNAMODB_TABLE"] = ResultTable });
```

## O que a Lambda faz

Arquivo: [handler.py](../../src/lambdas/dynamodb_writer/handler.py)

```python
payload = event if isinstance(event, dict) else json.loads(event)
table.put_item(Item={"id": payload["id"], "data": json.dumps(payload)})
```

Ela recebe um JSON e salva esse JSON na tabela de resultados.

## O que o teste valida

Arquivo: [DynamoDbLambdaTests.cs](../../scenarios/11-DynamoDB.Lambda/DynamoDbLambdaTests.cs)

O teste manda este payload:

```json
{"id":"evt-001","type":"click"}
```

Depois espera o item aparecer em `processed-events`.

## Passo a passo para rodar

1. Abra o Docker.
2. Rode:

```bash
dotnet test scenarios/11-DynamoDB.Lambda/
```

3. Aguarde a publicacao da Lambda.
4. Interprete o resultado:
   - em alguns ambientes a Lambda roda normalmente
   - em `macOS arm64`, a expectativa atual eh `SKIP`

## O que observar no resultado

Este cenario eh bom para aprender a diferenca entre:

- evento vindo de outro servico
- chamada direta da Lambda pelo proprio teste

## Arquivos principais

- [Fixture.cs](../../scenarios/11-DynamoDB.Lambda/Fixture.cs)
- [DynamoDbLambdaTests.cs](../../scenarios/11-DynamoDB.Lambda/DynamoDbLambdaTests.cs)
- [handler.py](../../src/lambdas/dynamodb_writer/handler.py)
