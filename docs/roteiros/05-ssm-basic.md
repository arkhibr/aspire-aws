# 05 - SSM Basic

## Tecnologias deste cenario

- [AWS Systems Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html): servico para guardar parametros de configuracao.
- [LocalStack](https://docs.localstack.cloud/): emulador local usado para testar o Parameter Store sem acessar AWS real.
- [AWS SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/welcome.html): biblioteca usada para gravar e ler parametros.

## Conceitos base deste cenario

- `Parametro`: valor salvo no Parameter Store.
- `String`: parametro textual comum.
- `SecureString`: parametro marcado como sensivel.
- `Path`: prefixo como `/app/config/...` usado para organizar parametros.

## O que este cenario ensina

Este roteiro apresenta o [AWS Systems Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html):

- salvar configuracoes simples
- salvar valores sensiveis
- buscar por nome
- buscar por caminho

## Conceitos em portugues simples

- [AWS Systems Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html): servico para guardar parametros de configuracao.
- `String`: valor textual comum.
- `SecureString`: valor tratado como segredo.
- `Path`: prefixo que organiza parametros em uma arvore, como `/app/config/...`.

## Como o cenario esta montado

Arquivo: [Fixture.cs](../../scenarios/05-SSM.Basic/Fixture.cs)

```csharp
protected override Task InitializeScenarioAsync()
{
    SSM = AwsClientFactory.SSM();
    return Task.CompletedTask;
}
```

## O que os testes validam

Arquivo: [SsmBasicTests.cs](../../scenarios/05-SSM.Basic/SsmBasicTests.cs)

- gravar e ler um parametro comum
- gravar e ler um `SecureString`
- listar parametros abaixo de um mesmo caminho

Trecho importante:

```csharp
await fixture.SSM.PutParameterAsync(new PutParameterRequest
{
    Name = "/app/secrets/api-key",
    Value = "super-secret",
    Type = "SecureString"
});
```

## Passo a passo para rodar

1. Abra o Docker.
2. Na raiz do repositorio, rode:

```bash
dotnet test scenarios/05-SSM.Basic/
```

3. Aguarde a execucao.
4. Confira `3 passed`.

## O que observar no resultado

Este cenario ajuda a responder uma duvida comum de iniciantes:
"Onde guardar configuracoes pequenas sem criar uma tabela ou um arquivo?"

A resposta aqui eh: Parameter Store.

## Arquivos principais

- [Fixture.cs](../../scenarios/05-SSM.Basic/Fixture.cs)
- [SsmBasicTests.cs](../../scenarios/05-SSM.Basic/SsmBasicTests.cs)
