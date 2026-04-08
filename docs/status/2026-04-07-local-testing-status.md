# Status - 2026-04-07 - Local Testing

## Contexto

Implementacao do ambiente local de testes AWS descrito em [2026-04-07-aspire-aws-local-testing.md](../superpowers/plans/2026-04-07-aspire-aws-local-testing.md), usando .NET Aspire + LocalStack + cenarios xUnit + Lambdas Python.

## Estado atual

- `dotnet build aspire-aws.sln --no-restore -m:1 -v minimal`: ok
- `dotnet test aspire-aws.sln --no-build -v minimal`: ok
- Resultado agregado da suite neste host: `24` aprovados, `9` ignorados, `0` falhas

## Ajustes persistidos no repositorio

- [src/Shared/Shared.csproj](../../src/Shared/Shared.csproj) define `IsTestProject=false` para impedir que `src/Shared` seja descoberto como projeto de teste.
- [Directory.Build.props](../../Directory.Build.props) define:
  - `BuildInParallel=false`
  - `TestTfmsInParallel=false`
- [test.runsettings](../../test.runsettings) define `MaxCpuCount=1`.
- Esses ajustes estabilizam `dotnet test aspire-aws.sln` e evitam contencao pela porta fixa `4566` do LocalStack.

## Limitacoes conhecidas neste ambiente

- Em `macOS arm64`, o caminho de invoke de Lambda do LocalStack `3.8` nao eh confiavel para os cenarios assincronos com trigger.
- Por isso, os cenarios abaixo sao ignorados neste host:
  - `07-S3.Lambda.Trigger`
  - `08-SQS.Lambda.Consumer`
  - `11-DynamoDB.Lambda`
  - `12-Pipeline.S3.SQS.Lambda.DynamoDB`
  - `13-Pipeline.SNS.SQS.Lambda.S3`
  - `14-EventBridge.Lambda`
- Os testes de Step Functions (`15`) seguem ignorados conforme o plano.

## Diagnostico resumido

- O bootstrap das Lambdas em Apple Silicon exigiu publicar com arquitetura compativel com o host.
- Mesmo com o runtime ficando `ready`, o LocalStack continuou falhando no invoke das Lambdas assincronas neste ambiente.
- O problema efetivo nao ficou no codigo dos cenarios, e sim no caminho de execucao do LocalStack para Lambda em `macOS arm64`.

## Proximos passos recomendados

- Validar os cenarios com Lambda assincrona em Linux/x64 ou CI.
- Se necessario, documentar essa limitacao tambem em um README do repositorio.
