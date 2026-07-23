using System.Linq;

namespace Knarr.Service.Exceptions;

/// <summary>
/// Thrown when a bulk operation that runs one CLI invocation per item (e.g. starting several
/// containers) has one or more individual failures. Carries every underlying
/// <see cref="CliCommandException"/> so the UI can surface each failing command together.
/// </summary>
public sealed class AggregateCliCommandException : Exception
{
    public AggregateCliCommandException(IReadOnlyList<CliCommandException> failures)
        : base(BuildMessage(failures))
    {
        Failures = failures;
    }

    /// <summary>The individual command failures that made up this bulk error.</summary>
    public IReadOnlyList<CliCommandException> Failures { get; }

    private static string BuildMessage(IReadOnlyList<CliCommandException> failures)
    {
        if (failures.Count == 1)
        {
            return failures[0].Message;
        }

        var lines = failures.Select(f => $"\u2022 {f.Message}");
        return $"{failures.Count} commands failed:\n{string.Join('\n', lines)}";
    }
}
