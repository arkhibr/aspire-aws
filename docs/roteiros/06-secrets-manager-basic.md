# 06 - Secrets Manager Basic

## Tecnologias deste cenario

- [AWS Secrets Manager](https://docs.aws.amazon.com/secretsmanager/latest/userguide/intro.html): servico para guardar segredos como senha, token e chave de API.
- [LocalStack](https://docs.localstack.cloud/): emulador local que permite testar segredos sem usar uma conta AWS real.
- [AWS SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/welcome.html): biblioteca usada para criar, atualizar e apagar segredos.

## Conceitos base deste cenario

- `Secret`: informacao sensivel guardada de forma centralizada.
- `SecretString`: conteudo textual do segredo.
- `Version`: cada atualizacao de um segredo gera uma nova versao.
- `Fixture`: classe que cria o cliente do Secrets Manager antes do teste.

## O que este cenario ensina

Este roteiro mostra o uso basico do [AWS Secrets Manager](https://docs.aws.amazon.com/secretsmanager/latest/userguide/intro.html):

- criar segredo
- ler segredo
- atualizar segredo
- apagar segredo

## Conceitos em portugues simples

- [AWS Secrets Manager](https://docs.aws.amazon.com/secretsmanager/latest/userguide/intro.html): servico para guardar segredos como senha, token e chave de API.
- `SecretString`: valor textual do segredo.
- `Version`: cada atualizacao cria uma nova versao interna do segredo.

## Como o cenario esta montado

Arquivo: [Fixture.cs](../../scenarios/06-SecretsManager.Basic/Fixture.cs)

```csharp
protected override Task InitializeScenarioAsync()
{
    SecretsManager = AwsClientFactory.SecretsManager();
    return Task.CompletedTask;
}
```

## O que os testes validam

Arquivo: [SecretsManagerBasicTests.cs](../../scenarios/06-SecretsManager.Basic/SecretsManagerBasicTests.cs)

- `CreateAndGetSecret_ShouldRoundTrip`
- `UpdateSecret_ShouldOverwriteValue`
- `DeleteSecret_ShouldMakeItInaccessible`

Trecho central:

```csharp
await fixture.SecretsManager.CreateSecretAsync(new CreateSecretRequest
{
    Name = "myapp/db-password",
    SecretString = "p@ssw0rd"
});
```

## Passo a passo para rodar

1. Abra o Docker.
2. Na raiz do repositorio, rode:

```bash
dotnet test scenarios/06-SecretsManager.Basic/
```

3. Aguarde o [LocalStack](https://docs.localstack.cloud/) subir.
4. Confira `3 passed`.

## O que observar no resultado

Este cenario eh parecido com o de SSM, mas a intencao eh diferente.
Aqui o foco eh guardar segredo de verdade, nao configuracao comum.

## Arquivos principais

- [Fixture.cs](../../scenarios/06-SecretsManager.Basic/Fixture.cs)
- [SecretsManagerBasicTests.cs](../../scenarios/06-SecretsManager.Basic/SecretsManagerBasicTests.cs)
