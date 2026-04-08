using Amazon.SecretsManager.Model;

namespace Scenarios.SecretsManager.Basic;

public class SecretsManagerBasicTests(Fixture fixture) : IClassFixture<Fixture>
{
    [Fact]
    public async Task CreateAndGetSecret_ShouldRoundTrip()
    {
        await fixture.SecretsManager.CreateSecretAsync(new CreateSecretRequest
        {
            Name = "myapp/db-password",
            SecretString = "p@ssw0rd"
        });

        var response = await fixture.SecretsManager.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = "myapp/db-password"
        });

        Assert.Equal("p@ssw0rd", response.SecretString);
    }

    [Fact]
    public async Task UpdateSecret_ShouldOverwriteValue()
    {
        await fixture.SecretsManager.CreateSecretAsync(new CreateSecretRequest
        {
            Name = "myapp/api-key",
            SecretString = "old-key"
        });

        await fixture.SecretsManager.PutSecretValueAsync(new PutSecretValueRequest
        {
            SecretId = "myapp/api-key",
            SecretString = "new-key"
        });

        var response = await fixture.SecretsManager.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = "myapp/api-key"
        });

        Assert.Equal("new-key", response.SecretString);
    }

    [Fact]
    public async Task DeleteSecret_ShouldMakeItInaccessible()
    {
        await fixture.SecretsManager.CreateSecretAsync(new CreateSecretRequest
        {
            Name = "myapp/to-delete",
            SecretString = "value"
        });

        await fixture.SecretsManager.DeleteSecretAsync(new DeleteSecretRequest
        {
            SecretId = "myapp/to-delete",
            ForceDeleteWithoutRecovery = true
        });

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            fixture.SecretsManager.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = "myapp/to-delete"
            }));
    }
}
