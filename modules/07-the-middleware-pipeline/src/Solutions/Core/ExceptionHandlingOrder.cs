using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Training.Module07.Core;

/// <summary>
/// A handler only covers what is registered behind it.
///
/// Exception middleware works by wrapping its call to next in a try/catch, so
/// anything registered before it is not inside that try block.
/// </summary>
public static class ExceptionHandlingOrder
{
    public static void ConfigureHandlerFirst(IApplicationBuilder app, IList<string> log)
    {
        app.Use(Handler(log));
        app.Use(Thrower(log));
    }

    public static void ConfigureHandlerLast(IApplicationBuilder app, IList<string> log)
    {
        app.Use(Thrower(log));
        app.Use(Handler(log));
    }

    private static Func<RequestDelegate, RequestDelegate> Handler(IList<string> log)
        => next => async context =>
        {
            try
            {
                await next(context);
            }
            catch (InvalidOperationException)
            {
                log.Add("caught");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        };

    private static Func<RequestDelegate, RequestDelegate> Thrower(IList<string> log)
        => next => context =>
        {
            log.Add("threw");
            throw new InvalidOperationException("checkout failed");
        };
}
