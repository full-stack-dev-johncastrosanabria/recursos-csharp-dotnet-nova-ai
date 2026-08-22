using Microsoft.AspNetCore.Builder;

namespace Training.Module07.Core;

/// <summary>
/// Exercise: three pipelines that make the difference between continuing and
/// stopping visible.
///
/// Each component logs "in:{name}" before calling next and "out:{name}" after
/// it returns. Whether the second half ever runs is the whole subject.
///
/// ConfigureWithTerminal registers "one" and "two" with Use, then a Run that
/// logs "terminal" and writes "handled" to the response.
///
/// ConfigureWithGuard registers "one" with Use, then a guard that logs "guard",
/// sets status 403 and does not call next, then the same terminal Run.
///
/// ConfigureAfterTerminal registers the terminal Run first and then "late" with
/// Use, which is the mistake: Run takes no next, so nothing behind it exists.
/// </summary>
public static class ShortCircuiting
{
    public static void ConfigureWithTerminal(IApplicationBuilder app, IList<string> log)
        => throw new NotImplementedException();

    public static void ConfigureWithGuard(IApplicationBuilder app, IList<string> log)
        => throw new NotImplementedException();

    public static void ConfigureAfterTerminal(IApplicationBuilder app, IList<string> log)
        => throw new NotImplementedException();
}
