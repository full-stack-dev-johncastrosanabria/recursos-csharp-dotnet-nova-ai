using Microsoft.AspNetCore.Builder;

namespace Training.Module07.Challenge;

/// <summary>
/// Exercise: read the response on the way out without stealing it.
///
/// Reach for this when something genuinely needs the response body -- audit
/// logging, a checksum, a compatibility rewrite. Do not reach for it casually:
/// buffering defeats streaming, holds the whole body in memory, and is the
/// usual reason a large download becomes an out-of-memory error.
///
/// The response body is a write-only stream you cannot rewind, so the only way
/// to see it is to substitute one you can. Three things have to be right:
///
///   Put a buffer in place of Response.Body before calling next.
///   Read the buffer, then copy it into the ORIGINAL stream -- otherwise the
///   caller receives nothing at all, which is the classic first attempt.
///   Restore the original stream afterwards, whatever happened, or every
///   middleware outside this one is writing into a buffer nobody reads.
///
/// UseResponseCapture adds that middleware, appending each response body it
/// sees to the supplied list. Configure wires it in front of a terminal Run
/// that writes "hello".
/// </summary>
public static class ResponseBuffering
{
    public static IApplicationBuilder UseResponseCapture(IApplicationBuilder app, IList<string> captured)
        => throw new NotImplementedException();

    public static void Configure(IApplicationBuilder app, IList<string> captured)
        => throw new NotImplementedException();
}
