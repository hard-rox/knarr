using System.Linq;

namespace Knarr.Service.Exceptions;

public sealed class AggregateCliCommandException(IReadOnlyList<CliCommandException> failures)
    : Exception(BuildMessage(failures))
{
    public IReadOnlyList<CliCommandException> Failures { get; } = failures;

    private static string BuildMessage(IReadOnlyList<CliCommandException> failures)
    {
        if (failures.Count == 1)
        {
            return failures[0].Message;
        }

        IEnumerable<string> lines = failures.Select(f => $"\u2022 {f.Message}");
        return $"{failures.Count} commands failed:\n{string.Join('\n', lines)}";
    }
}
