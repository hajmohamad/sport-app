using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace sport_app_backend.Services;

public class IpAddressFilter(IConfiguration config) : IActionFilter
{
    private readonly string[] _allowedIps = config.GetSection("AllowedIps").Get<string[]>() ?? new string[0];

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!_allowedIps.Contains(remoteIp))
        {
            context.Result = new ContentResult
            {
                StatusCode = (int)HttpStatusCode.Forbidden,
                Content = "Access denied: Your IP is not allowed"
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}