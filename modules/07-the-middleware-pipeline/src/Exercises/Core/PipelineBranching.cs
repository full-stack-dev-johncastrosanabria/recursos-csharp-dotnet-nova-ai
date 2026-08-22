using Microsoft.AspNetCore.Builder;

namespace Training.Module07.Core;

/// <summary>
/// Exercise: three ways to send some requests down a different path, two of
/// which never come back.
///
/// Reach for a branch when a subset of requests needs a genuinely different
/// pipeline -- an admin area, a webhook endpoint that must skip your normal
/// authentication, a legacy prefix. Reach for UseWhen instead when the subset
/// needs one EXTRA step and then the normal treatment.
///
/// ConfigureMap branches on the path segment "/api" into a branch that logs
/// "branch" and writes "api", then registers a main Run that logs "main" and
/// writes "main".
///
/// ConfigureMapWhen branches on the presence of the "X-Beta" request header
/// into the same branch body, with the same main Run.
///
/// ConfigureUseWhen branches on the same header, but the branch only adds a
/// component that logs "branch" and calls next -- so the request rejoins the
/// main pipeline and the main Run still handles it.
/// </summary>
public static class PipelineBranching
{
    public const string BranchHeader = "X-Beta";

    public static void ConfigureMap(IApplicationBuilder app, IList<string> log)
        => throw new NotImplementedException();

    public static void ConfigureMapWhen(IApplicationBuilder app, IList<string> log)
        => throw new NotImplementedException();

    public static void ConfigureUseWhen(IApplicationBuilder app, IList<string> log)
        => throw new NotImplementedException();
}
