using APKonsult.CommandChecks.Attributes;
using APKonsult.Context;
using APKonsult.Models.Main;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;

namespace APKonsult.CommandChecks;

public class RequireAdminUserCheck : IContextCheck<RequireAdminUserAttribute>
{
    private readonly APKonsultContext _dbContext;

    public RequireAdminUserCheck(APKonsultContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<string?> ExecuteCheckAsync(RequireAdminUserAttribute? _, CommandContext context)
    {
        UserDbEntity? user = await _dbContext.Users.FindAsync(context.User.Id);

        return user is null || (!user.IsBotAdmin && !RequireOwnerCheck.IsOwner(context))
            ? "You need to be a bot administrator!"
            : null;
    }
}