# 01 - S3 Basic

## Tecnologias deste cenario

- [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html): servico de armazenamento de arquivos da AWS.
- [LocalStack](https://docs.localstack.cloud/): emulador local que faz o papel da AWS durante os testes.
- [AWS SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/welcome.html): biblioteca usada pelo codigo C# para falar com o S3 local.

## Conceitos base deste cenario

- `Fixture`: classe que prepara o ambiente do teste. Aqui ele so cria o cliente do S3.
- `Bucket`: container logico onde os arquivos do S3 ficam guardados.
- `Object`: cada arquivo salvo dentro de um bucket.
- `Fabrica compartilhada`: apelido para a classe [AwsClientFactory.cs](../../src/Shared/AwsClientFactory.cs), que centraliza a criacao dos clientes AWS para todos os cenarios.

## O que este cenario ensina

Este roteiro mostra o basico do [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html):

- criar bucket
- gravar arquivo
- ler arquivo
- listar arquivos
- apagar arquivo
- gerar URL temporaria

## Conceitos em portugues simples

- [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html): servico de armazenamento de arquivos da AWS.
- `Bucket`: "pasta raiz" onde os arquivos ficam.
- `Object`: cada arquivo salvo no bucket.
- `Pre-signed URL`: link temporario para baixar um arquivo sem autenticar.

## Como o cenario esta montado

O `Fixture` so cria um cliente do [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html) apontando para o [LocalStack](https://docs.localstack.cloud/):

Arquivo: [Fixture.cs](../../scenarios/01-S3.Basic/Fixture.cs)

```csharp
protected override Task InitializeScenarioAsync()
{
    S3 = AwsClientFactory.S3();
    return Task.CompletedTask;
}
```

Repare que o cliente sempre vem da `fabrica compartilhada`, ou seja, da classe [AwsClientFactory.cs](../../src/Shared/AwsClientFactory.cs). A ideia dessa fabrica e criar todos os clientes AWS em um so lugar, sempre com o endpoint correto do LocalStack:

Arquivo: [AwsClientFactory.cs](../../src/Shared/AwsClientFactory.cs)

```csharp
public static AmazonS3Client S3(string endpoint = LocalStackFixture.Endpoint)
{
    return new AmazonS3Client(Credentials, Configure(new AmazonS3Config
    {
        ForcePathStyle = true
    }, endpoint));
}
```

## O que os testes validam

Arquivo: [S3BasicTests.cs](../../scenarios/01-S3.Basic/S3BasicTests.cs)

- `CreateBucket_ShouldSucceed`: cria um bucket e confirma na listagem.
- `PutAndGetObject_ShouldRoundTrip`: grava e le o mesmo arquivo.
- `ListObjects_ShouldReturnUploadedKeys`: garante que dois arquivos aparecem na listagem.
- `GetPresignedUrl_ShouldReturnAccessibleUrl`: gera URL temporaria e faz `GET` HTTP.
- `DeleteObject_ShouldRemoveKey`: apaga o objeto e verifica lista vazia.

Fragmento central:

```csharp
await fixture.S3.PutObjectAsync(new PutObjectRequest
{
    BucketName = "test-bucket-rw",
    Key = "hello.txt",
    ContentBody = "hello world"
});

using var response = await fixture.S3.GetObjectAsync("test-bucket-rw", "hello.txt");
```

## Passo a passo para rodar

1. Abra o Docker.
2. Va para a raiz do repositorio.
3. Rode:

```bash
dotnet test scenarios/01-S3.Basic/
```

4. Espere o [AppHost do Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview#the-apphost) subir o [LocalStack](https://docs.localstack.cloud/).
5. Veja os `5 passed`.

## O que observar no resultado

Se este cenario passar, voce ja sabe que:

- o AppHost subiu o LocalStack
- o cliente S3 conseguiu falar com o endpoint local
- leitura e escrita de objetos estao funcionando

## Arquivos principais

- [Fixture.cs](../../scenarios/01-S3.Basic/Fixture.cs)
- [S3BasicTests.cs](../../scenarios/01-S3.Basic/S3BasicTests.cs)
- [AwsClientFactory.cs](../../src/Shared/AwsClientFactory.cs)
