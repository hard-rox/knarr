using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Knarr.Service.Exceptions;

namespace Knarr.Service.AppleContainerCli;

internal sealed class AppleContainerSystemService(ILogger<AppleContainerSystemService> logger)
    : IContainerSystemService
{
    private const string Executable = "container";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ContainerSystemStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        string[] arguments = ["system", "status", "--format", "json"];
        var command = $"{Executable} {string.Join(' ', arguments)}";
        logger.LogDebug("Executing CLI command: {Command}", command);

        try
        {
            BufferedCommandResult result = await Cli.Wrap(Executable)
                .WithArguments(arguments)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellationToken)
                .ConfigureAwait(false);

            // An unregistered system still prints a well-formed payload while exiting non-zero, so
            // the output is authoritative whenever it parses; only fall back when it does not.
            ContainerSystemStatus status = ParseStatus(result.StandardOutput);
            if (status.State is ContainerSystemState.Unknown && !result.IsSuccess)
            {
                logger.LogWarning(
                    "CLI command failed: {Command} (exit {ExitCode}): {Error}",
                    command, result.ExitCode, result.StandardError);
            }

            return status;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "CLI command threw: {Command}", command);
            return ContainerSystemStatus.Unknown;
        }
    }

    // `--enable-kernel-install` is passed explicitly because the CLI otherwise prompts interactively
    // for the default kernel, which would hang a process we run without a console.
    public Task StartAsync(CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "system", "start", "--enable-kernel-install");

    public Task StopAsync(CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "system", "stop");

    // Payload is a single object, unlike the list commands' arrays; blank/malformed input yields ContainerSystemStatus.Unknown.
    internal static ContainerSystemStatus ParseStatus(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ContainerSystemStatus.Unknown;
        }

        AppleSystemStatusResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<AppleSystemStatusResponse>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return ContainerSystemStatus.Unknown;
        }

        if (response is null)
        {
            return ContainerSystemStatus.Unknown;
        }

        return new ContainerSystemStatus
        {
            State = MapState(response.Status),
            ApiServerVersion = response.ApiServerVersion ?? string.Empty,
            AppRoot = response.AppRoot ?? string.Empty,
            InstallRoot = response.InstallRoot ?? string.Empty,
        };
    }

    private static ContainerSystemState MapState(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "running" => ContainerSystemState.Running,
        "unregistered" => ContainerSystemState.Unregistered,
        "not running" or "stopped" => ContainerSystemState.NotRunning,
        _ => ContainerSystemState.Unknown,
    };

    private async Task RunAsync(CancellationToken cancellationToken, params string[] arguments)
    {
        var command = $"{Executable} {string.Join(' ', arguments)}";
        logger.LogDebug("Executing CLI command: {Command}", command);

        BufferedCommandResult result = await Cli.Wrap(Executable)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            logger.LogDebug("CLI command succeeded: {Command}", command);
            return;
        }

        logger.LogError("CLI command failed: {Command} (exit {ExitCode}): {Error}", command, result.ExitCode, result.StandardError);
        throw new CliCommandException(command, result.ExitCode, result.StandardError);
    }
}
