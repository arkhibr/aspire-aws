using System.IO.Compression;
using System.Runtime.InteropServices;
using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace Shared;

public class LambdaDeployer(IAmazonLambda client)
{
    private const string FakeRole = "arn:aws:iam::000000000000:role/local-role";

    public async Task DeployAsync(
        string functionName,
        string lambdaFolderName,
        Dictionary<string, string>? extraEnv = null)
    {
        var sourcePath = ResolveLambdaPath(lambdaFolderName);
        var zipBytes = CreateZip(sourcePath);

        var envVars = new Dictionary<string, string>();

        if (extraEnv is not null)
        {
            foreach (var pair in extraEnv)
            {
                envVars[pair.Key] = pair.Value;
            }
        }

        await client.CreateFunctionAsync(new CreateFunctionRequest
        {
            FunctionName = functionName,
            Runtime = Runtime.Python312,
            Architectures =
            [
                GetLambdaArchitecture()
            ],
            Handler = "handler.lambda_handler",
            Role = FakeRole,
            Timeout = 30,
            Code = new FunctionCode
            {
                ZipFile = new MemoryStream(zipBytes)
            },
            Environment = new Amazon.Lambda.Model.Environment
            {
                Variables = envVars
            }
        }).ConfigureAwait(false);

        await WaitUntilActiveAsync(functionName).ConfigureAwait(false);
    }

    private static Amazon.Lambda.Architecture GetLambdaArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.Arm64 => Amazon.Lambda.Architecture.Arm64,
            _ => Amazon.Lambda.Architecture.X86_64
        };
    }

    private static string ResolveLambdaPath(string folderName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null && !File.Exists(Path.Combine(current.FullName, "aspire-aws.sln")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new DirectoryNotFoundException("Could not locate the solution root.");
        }

        var lambdaPath = Path.Combine(current.FullName, "src", "lambdas", folderName);
        if (!Directory.Exists(lambdaPath))
        {
            throw new DirectoryNotFoundException($"Lambda folder not found: {lambdaPath}");
        }

        return lambdaPath;
    }

    private static byte[] CreateZip(string sourcePath)
    {
        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourcePath, file);
                if (relativePath.Contains("__pycache__", StringComparison.Ordinal))
                {
                    continue;
                }

                archive.CreateEntryFromFile(file, relativePath.Replace('\\', '/'));
            }
        }

        return stream.ToArray();
    }

    private async Task WaitUntilActiveAsync(string functionName)
    {
        await PollingHelper.WaitUntilAsync(async () =>
        {
            try
            {
                var response = await client.GetFunctionAsync(new GetFunctionRequest
                {
                    FunctionName = functionName
                }).ConfigureAwait(false);

                return response.Configuration.State == State.Active ||
                       string.Equals(response.Configuration.LastUpdateStatus, "Successful", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }, timeout: TimeSpan.FromSeconds(30), interval: TimeSpan.FromSeconds(1),
        failureMessage: $"Lambda '{functionName}' did not become active within 30s.")
        .ConfigureAwait(false);
    }
}
