# aspire-aws

Local AWS testing environment using .NET Aspire + LocalStack + Python Lambdas.

## Structure

- `src/AppHost` — Aspire AppHost; manages LocalStack container lifecycle
- `src/Shared` — shared xUnit fixtures and AWS helpers
- `src/lambdas/<name>/handler.py` — Python Lambda source code
- `scenarios/<NN>-<Name>/` — one xUnit project per scenario

## Non-negotiable conventions

**One project per scenario.** Never consolidate scenarios into a single test project. Each scenario must be independently runnable via `dotnet test scenarios/<name>/`.

**Lambda handlers are Python, tests are C#.** Do not mix. Tests deploy and invoke Lambdas via AWS SDK; they do not import or reference Python code directly.

**No fixed `Task.Delay` in tests.** Use a polling helper with timeout for async assertions (e.g., wait for Lambda side effects in SQS/DynamoDB). Fixed sleeps are flaky.

**AWS clients always via `AwsClientFactory`.** Never construct `AmazonXxxClient` directly in test code.

**`LocalStackFixture` stays generic.** Scenario-specific resource setup (create bucket, deploy Lambda, create table) belongs in the scenario's own `Fixture.cs`, not in `LocalStackFixture`.

**Lambda handlers read endpoint from env.** Always use `os.environ.get("AWS_ENDPOINT_URL")` in Python — LocalStack injects it automatically.

## Running tests

```bash
dotnet test scenarios/01-S3.Basic/       # single scenario
dotnet test aspire-aws.sln               # full suite
```

Docker must be running. LocalStack starts and stops automatically via Aspire.

## Known risks

- `StepFunctions` scenarios may require LocalStack Pro — skip if unavailable in Community edition
- On CI without Docker-in-Docker, set `LAMBDA_EXECUTOR=local` in the LocalStack container environment
