using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;
using sport_app_backend.Interface;

namespace sport_app_backend.Handler;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, ISmsService smsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

    
        logger.LogError(
            exception,
            "خطای مدیریت نشده رخ داد. TraceId: {TraceId}, Path: {Path}",
            traceId,
            httpContext.Request.Path
        );


        try
        {
            await smsService.SendErrorSms();
        }
        catch (Exception smsEx)
        {
            logger.LogError(smsEx, "خطا در هنگام ارسال پیامک اضطراری.");
        }


        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            Title = "خطای داخلی سرور",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "یک مشکل غیرمنتظره در سرور رخ داده است. لطفاً بعداً تلاش کنید.",
            TraceId = traceId
        }, cancellationToken);

        return true;
    }
}
    
