using Microsoft.AspNetCore.Http;

namespace Training.Module07.Core;

/// <summary>
/// Exercise: build the chain yourself, the way ApplicationBuilder.Build() does.
///
/// A middleware component is a function that, given the next delegate, returns
/// the delegate that runs in its place. A pipeline is those functions folded
/// into one RequestDelegate -- and the fold runs backwards, because a component
/// can only be given its "next" once that next already exists.
///
/// Compose returns a single delegate in which the components run in the order
/// they appear in the list, with the terminal delegate last. There is no
/// framework involved: this is the entire mechanism.
/// </summary>
public static class RequestPipeline
{
    public static RequestDelegate Compose(
        IReadOnlyList<Func<RequestDelegate, RequestDelegate>> components,
        RequestDelegate terminal)
        => throw new NotImplementedException();
}
