using FluentValidation;
using MiniWms.Domain.Common;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Locations;

public record CreateBinCommand(int ZoneId, string? Code, string? Name);

public class CreateBinValidator : AbstractValidator<CreateBinCommand>
{
    public CreateBinValidator()
    {
        RuleFor(c => c.ZoneId).GreaterThan(0).WithMessage("Zone is required.");
        RuleFor(c => c.Code).RequiredText("Code", Bin.CodeMaxLength);
        RuleFor(c => c.Name).RequiredText("Name", Bin.NameMaxLength);
    }
}

/// <summary>
/// Creates an Active bin under an Active zone (LOC-1.3). An unknown or Inactive zone is a 400 under
/// <c>zoneId</c>; a duplicate code is a 400 under <c>code</c> (LOC-1.4).
/// </summary>
public class CreateBinHandler(ILocationStore store, ILocationQueries queries)
{
    public async Task<BinDto> HandleAsync(CreateBinCommand command, CancellationToken cancellationToken)
    {
        await new CreateBinValidator().ValidateAndThrowAsync(command, cancellationToken);

        var zone = await store.FindZoneAsync(command.ZoneId, cancellationToken);
        if (zone is null || !zone.IsActive)
        {
            throw new ValidationFailedException("zoneId", "Zone must be an existing Active zone.");
        }

        var code = command.Code!.Trim();
        if (await store.BinCodeExistsAsync(code, cancellationToken))
        {
            throw LocationRules.DuplicateCode();
        }

        var bin = Bin.Create(zone.Id, code, command.Name);
        store.Add(bin);
        await store.SaveAsync(bin, null, cancellationToken);

        return await queries.GetBinAsync(bin.Id, cancellationToken)
            ?? throw new InvalidOperationException("The saved bin could not be read.");
    }
}
