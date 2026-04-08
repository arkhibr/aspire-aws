# 03 - DynamoDB Basic

## Tecnologias deste cenario

- [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html): banco NoSQL da AWS.
- [LocalStack](https://docs.localstack.cloud/): ambiente local que imita o DynamoDB da AWS.
- [AWS SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/welcome.html): biblioteca usada para criar tabela e manipular itens.

## Conceitos base deste cenario

- `Table`: estrutura principal do DynamoDB, parecida com uma tabela de banco.
- `Item`: registro salvo dentro da tabela.
- `HASH key`: chave primaria principal usada para encontrar um item.
- `Fixture`: classe que cria a tabela antes dos testes comecarem.

## O que este cenario ensina

Este roteiro apresenta o [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html) local:

- criar tabela
- inserir item
- buscar item por chave
- listar varios itens
- apagar item
- consultar por chave

## Conceitos em portugues simples

- [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html): banco NoSQL da AWS.
- `Table`: tabela.
- `Item`: registro.
- `HASH key`: chave primaria principal.
- `Scan`: le varios itens da tabela.
- `Query`: procura itens a partir da chave.

## Como o cenario esta montado

O `Fixture` cria a tabela `items` e espera o status `ACTIVE`.

Arquivo: [Fixture.cs](../../scenarios/03-DynamoDB.Basic/Fixture.cs)

```csharp
await DynamoDB.CreateTableAsync(new CreateTableRequest
{
    TableName = TableName,
    AttributeDefinitions =
    [
        new AttributeDefinition("id", ScalarAttributeType.S)
    ],
    KeySchema =
    [
        new KeySchemaElement("id", KeyType.HASH)
    ],
    BillingMode = BillingMode.PAY_PER_REQUEST
});
```

## O que os testes validam

Arquivo: [DynamoDbBasicTests.cs](../../scenarios/03-DynamoDB.Basic/DynamoDbBasicTests.cs)

- `PutAndGetItem_ShouldRoundTrip`: salva e busca pelo `id`.
- `Scan_ShouldReturnAllItems`: confirma leitura ampla da tabela.
- `DeleteItem_ShouldRemoveRecord`: garante remocao.
- `Query_ShouldReturnMatchingItems`: mostra consulta por chave.

Trecho importante:

```csharp
var response = await fixture.DynamoDB.GetItemAsync(
    Fixture.TableName,
    new Dictionary<string, AttributeValue> { ["id"] = new() { S = "item-1" } });
```

## Passo a passo para rodar

1. Abra o Docker.
2. Na raiz do repo, rode:

```bash
dotnet test scenarios/03-DynamoDB.Basic/
```

3. Aguarde a criacao da tabela.
4. Confira `4 passed`.

## O que observar no resultado

Se este cenario passar, o basico do banco local esta pronto:

- tabela sobe corretamente
- escrita e leitura funcionam
- a chave `id` esta sendo usada corretamente

## Arquivos principais

- [Fixture.cs](../../scenarios/03-DynamoDB.Basic/Fixture.cs)
- [DynamoDbBasicTests.cs](../../scenarios/03-DynamoDB.Basic/DynamoDbBasicTests.cs)
- [PollingHelper.cs](../../src/Shared/PollingHelper.cs)
