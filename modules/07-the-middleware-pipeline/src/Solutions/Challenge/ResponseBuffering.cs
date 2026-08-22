using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Training.Module07.Challenge;

/// <summary>
/// Read the response on the way out without stealing it.
///
/// The response body is a write-only stream you cannot rewind, so the only way
/// to see it is to substitute one you can -- and then put everything back.
/// </summary>
public static class ResponseBuffering
{
    public static IApplicationBuilder UseResponseCapture(IApplicationBuilder app, IList<string> captured)
        => app.Use(async (context, next) =>
        {
            var original = context.Response.Body;
            using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await next(context);

                buffer.Position = 0;
                using (var reader = new StreamReader(buffer, leaveOpen: true))
                {
                    captured.Add(await reader.ReadToEndAsync());
                }

                // Without this the caller gets an empty response: everything
                // downstream wrote went into the buffer, not to the client.
                buffer.Position = 0;
                await buffer.CopyToAsync(original);
            }
            finally
            {
                // In a finally because a failure downstream must not leave the
                // pipeline writing into a buffer that is about to be disposed.
                context.Response.Body = original;
            }
        });

    public static void Configure(IApplicationBuilder app, IList<string> captured)
    {
        UseResponseCapture(app, captured);
        app.Run(context => context.Response.WriteAsync("hello"));
    }
}
