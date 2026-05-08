using System.Diagnostics;

namespace sport_app_backend.Handler;

public class ResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<ResponseLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await next(context);
        }
        finally
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            if (context.Response.StatusCode >= 400)
            {
                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
                var user = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.Identity!.Name
                    : "Anonymous";

                logger.LogError(
                    "HTTP Error Response | TraceId: {TraceId} | StatusCode: {StatusCode} | Method: {Method} | Path: {Path} | User: {User} | Body: {Body}",
                    traceId,
                    context.Response.StatusCode,
                    context.Request.Method,
                    context.Request.Path,
                    user,
                    responseText
                );
            }

            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }
}