using Microsoft.AspNetCore.Builder;

namespace Training.Module07.Core;

/// <summary>
/// Exercise: a handler only covers what is registered behind it.
///
/// Exception middleware works by wrapping its call to next in a try/catch. That
/// is the entire mechanism, and it explains the rule completely: anything
/// registered before the handler is not inside its try block, so the handler
/// cannot see it fail. This is why the exception handler goes first, above
/// even HTTPS redirection and static files.
///
/// The handler catches InvalidOperationException, logs "caught", and sets
/// status 500. The thrower logs "threw" and throws
/// InvalidOperationException("checkout failed") without calling next.
///
/// ConfigureHandlerFirst registers the handler and then the thrower.
/// ConfigureHandlerLast registers the thrower and then the handler.
/// </summary>
public static class ExceptionHandlingOrder
{
    public static void ConfigureHandlerFirst(IApplicationBuilder app, IList<string> log)
        => throw new NotImplementedException();

    public static void ConfigureHandlerLast(IApplicationBuilder app, IList<string> log)
        => throw new NotImplementedException();
}
