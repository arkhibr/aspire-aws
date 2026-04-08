# 13 - Pipeline SNS SQS Lambda S3

## Tecnologias deste cenario

- [Amazon SNS](https://docs.aws.amazon.com/sns/latest/dg/welcome.html): servico que recebe a publicacao inicial.
- [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html): fila intermediaria do pipeline.
- [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html): processador que transforma a mensagem em arquivo.
- [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html): destino final do resultado.
- [LocalStack](https://docs.localstack.cloud/): ambiente local que emula toda essa topologia.

## Conceitos base deste cenario

- `Pipeline`: cadeia automatica com varias etapas.
- `Event source mapping`: configuracao que liga a fila SQS a Lambda.
- `Prefix`: parte inicial do nome de um objeto S3, usada aqui para procurar arquivos em `results/`.
- `Fanout`: distribuicao de uma publicacao para outros componentes.

## O que este cenario ensina

Este pipeline troca o [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html) por [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html) no final:

`[Amazon SNS](https://docs.aws.amazon.com/sns/latest/dg/welcome.html) publica -> [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html) recebe -> [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html) processa -> [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html) guarda o resultado`

## Conceitos em portugues simples

- [Amazon SNS](https://docs.aws.amazon.com/sns/latest/dg/welcome.html): origem do evento.
- [Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/welcome.html): buffer entre publicacao e processamento.
- [AWS Lambda](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html): pega a mensagem e decide o que gravar.
- [Amazon S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html): destino final do resultado processado.

## Como o cenario esta montado

O `Fixture`:

1. cria o bucket `fanout-results`
2. publica a Lambda `fanout-processor`
3. cria o topico SNS
4. cria a fila SQS
5. autoriza o SNS a publicar nela
6. inscreve a fila no topico
7. cria o `event source mapping` da fila para a Lambda

Arquivo: [Fixture.cs](../../scenarios/13-Pipeline.SNS.SQS.Lambda.S3/Fixture.cs)

## O que a Lambda faz

Arquivo: [handler.py](../../src/lambdas/fanout_processor/handler.py)

```python
outer = json.loads(record["body"])
message = outer.get("Message", record["body"])
key = f"results/{record['messageId']}.json"
s3.put_object(Bucket=bucket, Key=key, Body=message)
```

Ela pega a mensagem recebida e grava um arquivo JSON dentro da pasta `results/` no bucket.

## O que o teste valida

Arquivo: [EventFanoutPipelineTests.cs](../../scenarios/13-Pipeline.SNS.SQS.Lambda.S3/EventFanoutPipelineTests.cs)

O teste publica:

```json
{"type":"user-signup","userId":"u-42"}
```

Depois ele espera aparecer algum objeto com prefixo `results/`.

## Passo a passo para rodar

1. Abra o Docker.
2. Rode:

```bash
dotnet test scenarios/13-Pipeline.SNS.SQS.Lambda.S3/
```

3. Aguarde a configuracao do topico, fila e Lambda.
4. Em `macOS arm64`, a expectativa atual eh `SKIP`.

## O que observar no resultado

Este cenario ensina que a saida de um processamento nao precisa ser banco.
Ela pode ser um arquivo no S3, o que eh comum em relatorios, auditoria e armazenamento de eventos.

## Arquivos principais

- [Fixture.cs](../../scenarios/13-Pipeline.SNS.SQS.Lambda.S3/Fixture.cs)
- [EventFanoutPipelineTests.cs](../../scenarios/13-Pipeline.SNS.SQS.Lambda.S3/EventFanoutPipelineTests.cs)
- [handler.py](../../src/lambdas/fanout_processor/handler.py)
