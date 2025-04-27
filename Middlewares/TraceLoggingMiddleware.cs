
using System.Diagnostics;
using Serilog.Context;

namespace WolfBankGateway.Middlewares;

public class TraceLoggingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<TraceLoggingMiddleware> _logger;

  public TraceLoggingMiddleware(RequestDelegate next, ILogger<TraceLoggingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task Invoke(HttpContext context)
  {
    var activity = Activity.Current;
    if (activity != null)
    {
      using (LogContext.PushProperty("TraceId", activity.TraceId.ToString()))
      using (LogContext.PushProperty("SpanId", activity.SpanId.ToString()))
      {
        await _next(context);
      }
    }
    else
    {
      await _next(context);
    }
  }
}
