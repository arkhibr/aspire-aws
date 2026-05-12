using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Shared;

public class LocalStackFixture : IAsyncLifetime
{
    private const string LockFileName = "aspire-aws-localstack-4566.lock";

    private DistributedApplication? _app;
    private FileStream? _portLock;
    private bool _scenarioInitialized;

    public const string Endpoint = "http://localhost:4566";

    protected static bool ModoAws =>
        string.Equals(Environment.GetEnvironmentVariable("AWS_TARGET"), "aws",
            StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        if (ModoAws)
        {
            try
            {
                await InitializeScenarioAsync().ConfigureAwait(false);
                _scenarioInitialized = true;
            }
            catch
            {
                await ReleaseResourcesAsync().ConfigureAwait(false);
                throw;
            }
            return;
        }

        _portLock = await AcquirePortLockAsync().ConfigureAwait(false);

        try
        {
            var appHost = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.AppHost>()
                .ConfigureAwait(false);

            _app = await appHost.BuildAsync().ConfigureAwait(false);
            await _app.StartAsync().ConfigureAwait(false);
            await WaitForLocalStackAsync().ConfigureAwait(false);
            await ImprimirUrlDashboardAsync().ConfigureAwait(false);
            await InitializeScenarioAsync().ConfigureAwait(false);
            _scenarioInitialized = true;
        }
        catch
        {
            await ReleaseResourcesAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await ReleaseResourcesAsync().ConfigureAwait(false);
    }

    protected virtual Task InitializeScenarioAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task DisposeScenarioAsync()
    {
        return Task.CompletedTask;
    }

    private async Task ReleaseResourcesAsync()
    {
        Exception? scenarioException = null;
        Exception? appException = null;

        try
        {
            if (_scenarioInitialized)
            {
                await DisposeScenarioAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            scenarioException = ex;
        }

        if (_app is not null)
        {
            try
            {
                await _app.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                appException = ex;
            }
            finally
            {
                _app = null;
            }
        }

        _portLock?.Dispose();
        _portLock = null;
        _scenarioInitialized = false;

        if (scenarioException is not null && appException is not null)
        {
            throw new AggregateException(scenarioException, appException);
        }

        if (scenarioException is not null)
        {
            throw scenarioException;
        }

        if (appException is not null)
        {
            throw appException;
        }
    }

    private static async Task<FileStream> AcquirePortLockAsync()
    {
        var lockPath = Path.Combine(Path.GetTempPath(), LockFileName);
        FileStream? stream = null;

        await PollingHelper.WaitUntilAsync(async () =>
        {
            try
            {
                stream = new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);

                return true;
            }
            catch (IOException)
            {
                await Task.Delay(100).ConfigureAwait(false);
                return false;
            }
        }, timeout: TimeSpan.FromMinutes(5), interval: TimeSpan.FromMilliseconds(250),
        failureMessage: "Timed out waiting for exclusive access to LocalStack port 4566.")
        .ConfigureAwait(false);

        return stream!;
    }

    private Task ImprimirUrlDashboardAsync()
    {
        try
        {
            var config = _app!.Services.GetRequiredService<IConfiguration>();

            var token     = config["ASPIRE__DASHBOARD__FRONTEND__BROWSERTOKEN"]
                         ?? config["Dashboard:Frontend:BrowserToken"];
            var endpoint  = config["ASPIRE__DASHBOARD__FRONTEND__ENDPOINTURLS"]
                         ?? config["Dashboard:Frontend:EndpointUrls"]
                         ?? "http://localhost:18888";
            var url = string.IsNullOrEmpty(token)
                ? endpoint
                : $"{endpoint.TrimEnd('/')}/login?t={token}";

            Console.WriteLine();
            Console.WriteLine("┌──────────────────────────────────────────────────────────────────┐");
            Console.WriteLine($"│  ASPIRE DASHBOARD  →  {url,-44}│");
            Console.WriteLine("└──────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();
        }
        catch
        {
            // Dashboard não disponível neste ambiente — sem impacto nos testes
        }
        return Task.CompletedTask;
    }

    private static async Task WaitForLocalStackAsync()
    {
        using var http = new HttpClient();

        await PollingHelper.WaitUntilAsync(async () =>
        {
            try
            {
                using var response = await http.GetAsync($"{Endpoint}/_localstack/health")
                    .ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }, timeout: TimeSpan.FromMinutes(2), interval: TimeSpan.FromSeconds(1),
        failureMessage: "LocalStack did not become healthy within 120s.")
        .ConfigureAwait(false);
    }
}
