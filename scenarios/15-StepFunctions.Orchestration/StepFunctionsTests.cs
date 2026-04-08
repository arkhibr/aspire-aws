using Amazon.StepFunctions.Model;
using Shared;
using System.Text.Json;

namespace Scenarios.StepFunctions.Orchestration;

public class StepFunctionsTests(Fixture fixture) : IClassFixture<Fixture>
{
    private const string SkipReason = "Requires LocalStack Step Functions support, which is often unavailable in Community edition.";

    [Fact(Skip = SkipReason)]
    public async Task StartExecution_ShouldCompleteSuccessfully()
    {
        var input = JsonSerializer.Serialize(new { id = "exec-001" });

        var execution = await fixture.StepFunctions.StartExecutionAsync(new StartExecutionRequest
        {
            StateMachineArn = fixture.StateMachineArn,
            Name = "test-execution-001",
            Input = input
        });

        await PollingHelper.WaitUntilAsync(async () =>
        {
            var description = await fixture.StepFunctions.DescribeExecutionAsync(new DescribeExecutionRequest
            {
                ExecutionArn = execution.ExecutionArn
            });

            return string.Equals(description.Status, "SUCCEEDED", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(description.Status, "FAILED", StringComparison.OrdinalIgnoreCase);
        }, timeout: TimeSpan.FromSeconds(60));

        var final = await fixture.StepFunctions.DescribeExecutionAsync(new DescribeExecutionRequest
        {
            ExecutionArn = execution.ExecutionArn
        });

        Assert.Equal("SUCCEEDED", final.Status);
    }

    [Fact(Skip = SkipReason)]
    public async Task ListExecutions_ShouldIncludeStartedExecution()
    {
        var execution = await fixture.StepFunctions.StartExecutionAsync(new StartExecutionRequest
        {
            StateMachineArn = fixture.StateMachineArn,
            Name = "test-execution-list",
            Input = """{"id":"exec-list"}"""
        });

        var list = await fixture.StepFunctions.ListExecutionsAsync(new ListExecutionsRequest
        {
            StateMachineArn = fixture.StateMachineArn
        });

        Assert.Contains(list.Executions, item => item.ExecutionArn == execution.ExecutionArn);
    }
}
