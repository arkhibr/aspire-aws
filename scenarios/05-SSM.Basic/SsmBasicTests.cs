using Amazon.SimpleSystemsManagement.Model;

namespace Scenarios.SSM.Basic;

public class SsmBasicTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task PutAndGetParameter_ShouldRoundTrip()
    {
        await fixture.SSM.PutParameterAsync(new PutParameterRequest
        {
            Name = "/app/config/db-host",
            Value = "localhost",
            Type = "String"
        });

        var response = await fixture.SSM.GetParameterAsync(new GetParameterRequest
        {
            Name = "/app/config/db-host"
        });

        Assert.Equal("localhost", response.Parameter.Value);
    }

    [Fact]
    public async Task PutSecureStringParameter_ShouldBeRetrievable()
    {
        await fixture.SSM.PutParameterAsync(new PutParameterRequest
        {
            Name = "/app/secrets/api-key",
            Value = "super-secret",
            Type = "SecureString"
        });

        var response = await fixture.SSM.GetParameterAsync(new GetParameterRequest
        {
            Name = "/app/secrets/api-key",
            WithDecryption = true
        });

        Assert.Equal("super-secret", response.Parameter.Value);
    }

    [Fact]
    public async Task GetParametersByPath_ShouldReturnAllUnderPrefix()
    {
        await fixture.SSM.PutParameterAsync(new PutParameterRequest
        {
            Name = "/myapp/env/host",
            Value = "host-val",
            Type = "String"
        });
        await fixture.SSM.PutParameterAsync(new PutParameterRequest
        {
            Name = "/myapp/env/port",
            Value = "5432",
            Type = "String"
        });

        var response = await fixture.SSM.GetParametersByPathAsync(new GetParametersByPathRequest
        {
            Path = "/myapp/env",
            Recursive = true
        });

        Assert.Equal(2, response.Parameters.Count);
    }
}
