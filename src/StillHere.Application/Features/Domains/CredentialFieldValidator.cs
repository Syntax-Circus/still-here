using StillHere.Application.Features.DnsProviders;
using SyntaxCircus.Common;

namespace StillHere.Application.Features.Domains;

internal static class CredentialFieldValidator
{
    /// <summary>
    /// Re-projects <paramref name="submitted"/> down to exactly the keys <paramref name="provider"/>
    /// declares, failing validation if any required field is missing or blank. Unexpected extra
    /// keys are silently dropped rather than stored.
    /// </summary>
    public static Result<Dictionary<string, string>> ValidateAndProject(
        IDnsProvider provider,
        IReadOnlyDictionary<string, string> submitted)
    {
        var projected = new Dictionary<string, string>();
        var errors = new List<ResultError>();

        foreach (var field in provider.CredentialFields)
        {
            if (!submitted.TryGetValue(field.Key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new ResultError(
                    "credential-field-required",
                    $"{field.Label} is required.",
                    ResultErrorKind.Validation,
                    field.Key));
                continue;
            }

            projected[field.Key] = value;
        }

        return errors.Count > 0
            ? Result<Dictionary<string, string>>.Failure(errors[0], [.. errors.Skip(1)])
            : Result<Dictionary<string, string>>.Success(projected);
    }
}
