# 15 - StepFunctions Orchestration

## Tecnologias deste cenario

- [AWS Step Functions](https://docs.aws.amazon.com/step-functions/latest/dg/welcome.html): servico de orquestracao de workflows.
- [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html): tarefa chamada de dentro da state machine.
- [LocalStack](https://docs.localstack.cloud/): ambiente local usado para tentar reproduzir a state machine.

## Conceitos base deste cenario

- `State machine`: definicao do fluxo com etapas e regras.
- `Task state`: etapa que chama outro servico, como Lambda.
- `Choice state`: etapa que decide o proximo caminho com base em dados.
- `Execution`: uma rodada concreta de execucao da state machine.

## O que este cenario ensina

Este roteiro mostra orquestracao:

`[AWS Step Functions](https://docs.aws.amazon.com/step-functions/latest/dg/welcome.html) -> chama [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html) -> avalia resultado -> termina com sucesso ou falha`

## Conceitos em portugues simples

- [AWS Step Functions](https://docs.aws.amazon.com/step-functions/latest/dg/welcome.html): servico para montar fluxos de varias etapas.
- `State machine`: definicao do fluxo.
- `Task state`: etapa que chama outro servico, aqui uma Lambda.
- `Choice state`: etapa que decide o proximo caminho com base no resultado.
- `Execution`: uma rodada de execucao da maquina.

## Como o cenario esta montado

O `Fixture`:

1. cria a tabela `sf-results`
2. publica a Lambda `stepfunctions-task`
3. monta uma definicao JSON da state machine
4. cria a state machine `example-workflow`

Arquivo: [Fixture.cs](../../scenarios/15-StepFunctions.Orchestration/Fixture.cs)

Trecho importante da definicao:

```json
{
  "StartAt": "ProcessStep",
  "States": {
    "ProcessStep": {
      "Type": "Task"
    },
    "CheckResult": {
      "Type": "Choice"
    }
  }
}
```

## O que a Lambda faz

Arquivo: [handler.py](../../src/lambdas/stepfunctions_task/handler.py)

```python
def lambda_handler(event, context):
    result = dict(event)
    result["processed"] = True
    result["step"] = event.get("step", "unknown")
    return result
```

Ela simplesmente devolve o mesmo evento com `processed = True`.

## O que os testes tentariam validar

Arquivo: [StepFunctionsTests.cs](../../scenarios/15-StepFunctions.Orchestration/StepFunctionsTests.cs)

- iniciar uma execucao e esperar `SUCCEEDED`
- listar execucoes e encontrar a execucao criada

## Observacao importante

Hoje os dois testes estao marcados com `Skip`:

```csharp
private const string SkipReason =
    "Requires LocalStack Step Functions support, which is often unavailable in Community edition.";
```

Ou seja: este roteiro serve mais para estudo da estrutura do que para execucao pratica garantida no ambiente atual.

## Passo a passo para rodar

1. Abra o Docker.
2. Rode:

```bash
dotnet test scenarios/15-StepFunctions.Orchestration/
```

3. Aguarde a descoberta dos testes.
4. A expectativa atual eh ver `SKIP`, nao `pass`.

## O que observar no resultado

Mesmo sem executar de verdade, este cenario ajuda a entender como o projeto representa uma orquestracao mais rica do que um simples trigger.

## Arquivos principais

- [Fixture.cs](../../scenarios/15-StepFunctions.Orchestration/Fixture.cs)
- [StepFunctionsTests.cs](../../scenarios/15-StepFunctions.Orchestration/StepFunctionsTests.cs)
- [handler.py](../../src/lambdas/stepfunctions_task/handler.py)
