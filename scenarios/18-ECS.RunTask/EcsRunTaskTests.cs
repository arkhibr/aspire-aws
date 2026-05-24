using Xunit.Abstractions;

namespace Scenarios.ECS.RunTask;

public class EcsRunTaskTests(Fixture fixture, ITestOutputHelper output) : IClassFixture<Fixture>
{
    [Fact]
    public void Placeholder_ShouldPass() => Assert.True(true);
}
