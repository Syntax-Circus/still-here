using SyntaxCircus.Common;

namespace StillHere.Application.Features.Domains;

public sealed record DeleteManagedDomainRequest(int Id);

public interface IDeleteManagedDomainRequestHandler
{
    Task<Result> HandleAsync(DeleteManagedDomainRequest request, CancellationToken cancellationToken);
}

public sealed class DeleteManagedDomainRequestHandler(IManagedDomainRepository managedDomains)
    : IDeleteManagedDomainRequestHandler
{
    public async Task<Result> HandleAsync(DeleteManagedDomainRequest request, CancellationToken cancellationToken)
    {
        var existing = await managedDomains.FindByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure(new ResultError(
                "domain-not-found", "Domain not found.", ResultErrorKind.NotFound));
        }

        await managedDomains.DeleteAsync(request.Id, cancellationToken);

        return Result.Success();
    }
}
