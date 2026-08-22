using Microsoft.AspNetCore.Http;

namespace Training.Module07.Core;

/// <summary>
/// Build the chain yourself, the way ApplicationBuilder.Build() does.
///
/// A middleware component is a function that, given the next delegate, returns
/// the delegate that runs in its place. A pipeline is those functions folded
/// into one RequestDelegate -- and the fold runs backwards, because a component
/// can only be given its "next" once that next already exists.
/// </summary>
public static class RequestPipeline
{
    public static RequestDelegate Compose(
        IReadOnlyList<Func<RequestDelegate, RequestDelegate>> components,
        RequestDelegate terminal)
    {
        var next = terminal;

        // Backwards: the last component registered is the one closest to the
        // terminal, so it is the first that can be given a next to wrap.
        for (var index = components.Count - 1; index >= 0; index--)
        {
            next = components[index](next);
        }

        return next;
    }
}
