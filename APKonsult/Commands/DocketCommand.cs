using APKonsult.CommandChecks.Attributes;
using APKonsult.Context;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees.Metadata;
using System.ComponentModel;

namespace APKonsult.Commands;

/// <summary>
/// Read-only operations are open to the public, but operations that 
/// make changes to the docket are restricted to administrators.
/// </summary>
[Command("docket"),
    Description("Features or other items to be implemented in APKognito.")]
internal class DocketCommand(APKonsultContext _dbContext)
{
    /*
     * Write commands (require permissions)
     */

    [Command("add"),
        TextAlias("new"),
        Description("Adds a new item to the docket."),
        RequireAdminUser]
    public async ValueTask AddDocketAsync(CommandContext ctx)
    {
        throw new NotImplementedException();
    }

    [Command("remove"),
        TextAlias("delete", "rm"),
        Description("Removes an item from the docket."),
        RequireAdminUser]
    public async ValueTask RemoveDocketAsync(CommandContext ctx)
    {

    }

    /*
     * Read commands (no permissions)
     */

    [Command("list"),
        Description("Lists docket items.")]
    public async ValueTask ListDocketAsync(CommandContext ctx)
    {

    }
}
