using FluentValidation;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Locations;

/// <param name="Id">From the route.</param>
public record UpdateBinCommand(int Id, string? Name, int Version);

public class UpdateBinValidator : AbstractValidator<UpdateBinCommand>
{
    public UpdateBinValidator()
    {
        RuleFor(c => c.Name).RequiredText("Name", Bin.NameMaxLength);
        RuleFor(c => c.Version).ValidVersion();
    }
}

/// <summary>Renames a bin (LOC-2.1); the code never changes. Returns null when not found.</summary>
public class UpdateBinHandler(ILocationStore store, ILocationQueries queries)
{
    public async Task<BinDto?> HandleAsync(UpdateBinCommand command, CancellationToken cancellationToken)
    {
        await new UpdateBinValidator().ValidateAndThrowAsync(command, cancellationToken);

        var bin = await store.FindBinAsync(command.Id, cancellationToken);
        if (bin is null)
        {
            return null;
        }

        bin.Rename(command.Name);
        await store.SaveAsync(bin, command.Version, cancellationToken);
        return await queries.GetBinAsync(bin.Id, cancellationToken);
    }
}
